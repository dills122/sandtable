#!/usr/bin/env python3
"""Scenario B sweep: N seeds x controller policies of Cna.ExerciseRunner in parallel processes.

Usage (repo root):
  python3 tools/sweep/sweep.py scenarios/sweeps/battalion-matrix-200.json [--build] [--out DIR]
      [--parallelism N] [--repeats N] [--runner PATH.dll] [--limit N]

Generates one manifest per (policy, seed) from the sweep input's base manifest, runs the Runner once per
manifest (one process each; the Runner manifest contract is unchanged), then writes under --out
(default artifacts/sweeps/<sweepId>):
  aggregate.json  identity-only report (no timings): reproducible, byte-identical across repeats
  timing.json     wall-clock, runs/s per repeat (NOT identity)
  runs/<repeat>/<run-id>/   raw Runner artifact roots (bundle under succeeded/ or failed/)
Determinism: every file of each bundle except diagnostics.jsonl is hashed; repeat k must equal repeat 0.
Exit 0 when the sweep completed and repeats agree; 1 on determinism mismatch; 2 on harness error.
Note: exercise-harness-v1.md records that parallel/distributed Runner work needs separate authorization;
this tool runs local processes only.
"""
import argparse, collections, concurrent.futures, hashlib, json, os, shutil, statistics, subprocess, sys, time

REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DEFAULT_DLL = "artifacts/bin/Cna.ExerciseRunner/release/Cna.ExerciseRunner.dll"


def canon(obj):
    return json.dumps(obj, sort_keys=True, indent=2, ensure_ascii=False) + "\n"


def bundle_dir(root):
    for kind in ("succeeded", "failed"):
        base = os.path.join(root, kind)
        if os.path.isdir(base):
            kids = sorted(os.listdir(base))
            if len(kids) == 1:
                return kind, os.path.join(base, kids[0])
    return None, None


def hash_bundle(path):
    digest = hashlib.sha256()
    for name in sorted(os.listdir(path)):
        if name == "diagnostics.jsonl":
            continue
        digest.update(name.encode() + b"\0")
        with open(os.path.join(path, name), "rb") as handle:
            digest.update(hashlib.sha256(handle.read()).digest())
    return digest.hexdigest()


def read_jsonl(path):
    with open(path, encoding="utf-8") as handle:
        return [json.loads(line) for line in handle if line.strip()]


def run_one(job):
    dll, manifest_rel, root = job["dll"], job["manifest"], job["root"]
    shutil.rmtree(root, ignore_errors=True)
    os.makedirs(root)
    proc = subprocess.run(
        ["dotnet", dll, "exercise", "run", "--manifest", manifest_rel, "--artifact-root", root],
        cwd=REPO, capture_output=True, text=True)
    kind, bundle = bundle_dir(root)
    record = {"runId": job["runId"], "policy": job["policy"], "seed": job["seed"],
              "exitCode": proc.returncode, "bundleKind": kind}
    if bundle:
        record["bundleHash"] = hash_bundle(bundle)
        result = json.load(open(os.path.join(bundle, "run-result.json"), encoding="utf-8"))
        record["status"] = result.get("status")
        record["failureCategory"] = (result.get("failure") or {}).get("category")
        steps = read_jsonl(os.path.join(bundle, "step-evidence.jsonl"))
        record["steps"] = len(steps)
        record["positions"] = [s["positionId"] for s in steps]
        final = json.load(open(os.path.join(bundle, "final-snapshot.json"), encoding="utf-8"))
        record["finalPosition"] = final.get("positionId")
    else:
        record["status"] = "no-bundle"
        record["failureCategory"] = "no-bundle"
        record["stderr"] = proc.stderr.strip()[-300:]
    return record


def summarize(records):
    steps = [r["steps"] for r in records if "steps" in r]
    visits = collections.Counter()
    for r in records:
        visits.update(r.get("positions", []))
    return {
        "runs": len(records),
        "exitCodes": dict(sorted(collections.Counter(str(r["exitCode"]) for r in records).items())),
        "status": dict(sorted(collections.Counter(r["status"] for r in records).items())),
        "failureCategories": dict(sorted(collections.Counter(
            r["failureCategory"] or "none" for r in records).items())),
        "terminalReached": sum(1 for r in records if r["status"] == "succeeded"),
        "stepCount": ({"min": min(steps), "max": max(steps), "mean": round(statistics.fmean(steps), 3)}
                      if steps else None),
        "finalPositions": dict(sorted(collections.Counter(
            r.get("finalPosition") or "none" for r in records).items())),
        "distinctBundles": len({r["bundleHash"] for r in records if "bundleHash" in r}),
        "positionVisits": dict(sorted(visits.items())),
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("sweep")
    ap.add_argument("--build", action="store_true", help="dotnet build the Runner (Release) first")
    ap.add_argument("--out")
    ap.add_argument("--parallelism", type=int)
    ap.add_argument("--repeats", type=int)
    ap.add_argument("--runner", default=DEFAULT_DLL)
    ap.add_argument("--limit", type=int, help="only the first N runs (smoke test)")
    args = ap.parse_args()

    spec = json.load(open(os.path.join(REPO, args.sweep) if not os.path.isabs(args.sweep) else args.sweep,
                          encoding="utf-8"))
    sweep_id = spec["sweepId"]
    parallelism = args.parallelism or spec.get("parallelism", os.cpu_count() or 2)
    repeats = args.repeats or spec.get("repeats", 2)
    out = os.path.abspath(args.out or os.path.join(REPO, "artifacts", "sweeps", sweep_id))

    if args.build:
        build = subprocess.run(["dotnet", "build", "src/Cna.ExerciseRunner", "-c", "Release", "--nologo", "-v", "q"],
                               cwd=REPO)
        if build.returncode:
            return 2
    dll = os.path.join(REPO, args.runner) if not os.path.isabs(args.runner) else args.runner
    if not os.path.exists(dll):
        print(f"Runner not found: {dll} (use --build)", file=sys.stderr)
        return 2

    base = json.load(open(os.path.join(REPO, spec["baseManifest"]), encoding="utf-8"))
    # The Runner resolves manifests repo-relative; .planning/ is gitignored scratch.
    manifest_dir_rel = os.path.join(".planning", "sweeps", sweep_id)
    manifest_dir = os.path.join(REPO, manifest_dir_rel)
    shutil.rmtree(manifest_dir, ignore_errors=True)
    os.makedirs(manifest_dir)

    runs = []
    first = spec["seeds"]["start"]
    for policy in spec["policies"]:
        for seed in range(first, first + spec["seeds"]["count"]):
            manifest = dict(base)
            manifest.update(spec.get("overrides", {}))
            manifest["rootSeed"] = seed
            manifest["controllers"] = {role: policy for role in base["controllers"]}
            run_id = f"{policy}.seed-{seed:06d}"
            path = os.path.join(manifest_dir, run_id + ".json")
            with open(path, "w", encoding="utf-8") as handle:
                json.dump(manifest, handle, separators=(",", ":"))
            runs.append({"runId": run_id, "policy": policy, "seed": seed,
                         "manifest": os.path.join(manifest_dir_rel, run_id + ".json")})
    if args.limit:
        runs = runs[:args.limit]

    per_repeat, timing = [], []
    for repeat in range(repeats):
        jobs = [dict(r, dll=dll, root=os.path.join(out, "runs", f"repeat-{repeat}", r["runId"])) for r in runs]
        started = time.monotonic()
        with concurrent.futures.ThreadPoolExecutor(max_workers=parallelism) as pool:
            records = sorted(pool.map(run_one, jobs), key=lambda r: r["runId"])
        wall = time.monotonic() - started
        per_repeat.append(records)
        timing.append({"repeat": repeat, "wallSeconds": round(wall, 2), "runs": len(records),
                       "runsPerSecond": round(len(records) / wall, 2)})
        print(f"repeat {repeat}: {len(records)} runs in {wall:.1f}s ({len(records) / wall:.2f} runs/s)",
              file=sys.stderr)

    mismatches = [
        {"runId": a["runId"], "repeat": k, "first": a.get("bundleHash"), "other": b.get("bundleHash")}
        for k in range(1, repeats) for a, b in zip(per_repeat[0], per_repeat[k])
        if a.get("bundleHash") != b.get("bundleHash") or a.get("bundleHash") is None]
    by_policy = {p: summarize([r for r in per_repeat[0] if r["policy"] == p]) for p in spec["policies"]}
    for r in per_repeat[0]:
        r.pop("positions", None)
    report = {
        "schemeId": "sandtable.sweep-aggregate.v1",
        "sweepId": sweep_id,
        "input": {k: spec[k] for k in ("baseManifest", "overrides", "policies", "seeds")},
        "byPolicy": by_policy,
        "determinism": {"repeats": repeats, "runsCompared": len(runs), "mismatches": mismatches,
                        "byteIdentical": not mismatches},
        "runs": [{k: r.get(k) for k in ("runId", "exitCode", "bundleKind", "status", "failureCategory",
                                        "steps", "finalPosition", "bundleHash")} for r in per_repeat[0]],
    }
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, "aggregate.json"), "w", encoding="utf-8") as handle:
        handle.write(canon(report))
    with open(os.path.join(out, "timing.json"), "w", encoding="utf-8") as handle:
        handle.write(canon({"parallelism": parallelism, "cpuCount": os.cpu_count(), "repeats": timing}))
    print(f"wrote {os.path.join(out, 'aggregate.json')}", file=sys.stderr)
    return 0 if not mismatches else 1


if __name__ == "__main__":
    sys.exit(main())
