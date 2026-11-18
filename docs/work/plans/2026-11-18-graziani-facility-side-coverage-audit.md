# Graziani facility-side coverage audit

**Date:** November 18, 2026. Reconciliation of the existing [air-facility batch](2026-11-18-graziani-air-facility-edges-batch.md), landing-strip batches [A](2026-11-18-graziani-landing-strip-edges-a.md) and [B](2026-11-18-graziani-landing-strip-edges-b.md), their same-scan reviews, and the targeted rechecks. This is a coverage and consistency audit of those notes, not a new map reading or independent source corroboration.

## Coverage

| Batch | Distinct targets | Directed target-to-neighbor incidences |
| --- | ---: | ---: |
| Air facilities | 10 | 60 |
| Landing strips A | 10 | 60 |
| Landing strips B | 10 | 60 |
| **Total** | **30** | **180** |

Each target has six listed neighbors; targets are distinct across batches. Canonicalizing each target–neighbor incidence as an unordered pair yields **178 distinct pairs**. Two pairs occur twice because both endpoints are targets:

| Shared pair | Incidence readings |
| --- | --- |
| `E2132`–`E2133` | Both air rows record blue cross-ticked railroad; [air review](2026-11-18-graziani-air-facility-edges-batch-review.md) supports it. |
| `D3416`–`D3516` | Both batch B rows describe brown terrain/grid with overprint and no complete side classification; neither asserts a crossing. |

These paired readings do not contradict one another. Distinct pairs include sides whose other endpoint is not a target; this count is not a count of confirmed features, clear sides, or traversable connections.

## Positive observations and unresolved sides

Air review counts 12 positive row claims on 11 unique pairs, with 48 other incidences unresolved. Positive linework does not establish a complete inventory even on those 11 pairs. Batch A uses `u` for no obvious distinct crossing or side band on exposed grid at crop scale, and `?` for overlapping or near-vertex uncertainty; neither is a negative classification. Its reviewed positives include `B4933`–`B4934` road, while several Gazala junction assignments remain provisional. Batch B records visible features on some sides and unresolved grid, overprint, or vertex situations on others; its descriptions are not exhaustive negative classifications. An absent pinned pair is unrepresented, not proven feature-free. The batches use different observation conventions, so their positive and unknown counts should not be combined as though they were equivalent classifications.

[Batch B challenge](2026-11-18-graziani-landing-strip-edges-b-review.md) and [critical recheck](2026-11-18-graziani-landing-b-critical-recheck.md) identify source-versus-pin questions: road plus railroad at `D3231`–`D3232`, apparent solid road versus pinned unfinished road at `B5917`–`B5918`, and a visible track near `B5229`–`B5130` whose crossing of that side remains unestablished. Interior settlement and coastal questions in those notes require their own classification rules and are not side-count results.

## Rail correction and remaining limits

The earlier [rail review](2026-11-17-graziani-d3516-d3517-rail-review.md) and [targeted recheck](2026-11-18-graziani-d3516-rail-conflict.md) support a blue railroad crossing on **`D3516`–`D3517`**, contradicting batch B's original omission and its review's no-distinct-crossing statement for that pair. The recheck supports a candidate missing pinned railroad only; road near the vertex and other features remain unresolved. This is a conflict across readings of one pair, not a conflict between the two `D3416`–`D3516` incidence rows. The batch B source table now places this correction on `D3516`–`D3517`; the separate `D3416`–`D3517` row has been restored to its original unresolved reading.

All map readings use the same scan, not independent editions. Facility attribution, source location, terrain and pin comparisons do not establish setup, control, capacity, complete routes, legal movement, or reachability. No runtime or map data changed; no gameplay tests run.
