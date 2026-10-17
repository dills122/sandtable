#!/usr/bin/env python3
"""Read-only, provisional P01 assessment over authentic R03 ancestry."""
import hashlib
import importlib.util
import json
from collections import deque
from pathlib import Path

ROOT = Path(__file__).resolve().parent
REPOSITORY = ROOT.parent.parent
SCHEMA = json.loads((ROOT / "combat-actual-moved-repeat-position-v1.schema.json").read_bytes())
spec = importlib.util.spec_from_file_location(
    "p01_frozen_r03", ROOT / "verify-combat-actual-moved-repeat-breakdown-completion-v1.py")
r03 = importlib.util.module_from_spec(spec)
spec.loader.exec_module(r03)


def require(value):
    if not value:
        raise ValueError("CMB-P01-001")


def canonical(value):
    return json.dumps(value, ensure_ascii=True, separators=(",", ":")).encode("ascii")


def digest(value):
    return "sha256:" + hashlib.sha256(value).hexdigest()


def selected_content():
    path = SCHEMA["contentPath"]
    require(r03.PINS.get(path) == SCHEMA["contentSha256"])
    data = (REPOSITORY / path).read_bytes()
    require(hashlib.sha256(data).hexdigest() == SCHEMA["contentSha256"])
    return json.loads(data)


def exact_selected_profile(state, content):
    """Return trusted singleton pair or None; never assert complete action support."""
    world = state["world"]
    elements = world["elements"]
    representations = world["representations"]
    static = content["elements"]
    if not (len(elements) == len(representations) == len(static) == 2
            and len(world["brokenVehicleLots"]) == len(world["custodyLots"]) == len(world["guards"]) == 0
            and len(world["replacementEntitlements"]) == len(world["futureObligations"]) == 0):
        return None
    by_id = {item["elementId"]: item for item in elements}
    static_by_id = {item["elementId"]: item for item in static}
    if len(by_id) != 2 or set(by_id) != set(static_by_id):
        return None
    for item in elements:
        static_item = static_by_id[item["elementId"]]
        if not (item["reserveStatus"] == "none"
                and item["currentParentFormationId"] == item["sourceParentFormationId"] == static_item["parentFormationId"]
                and len(item["components"]) == len(static_item["components"]) == 1
                and static_item["components"][0]["componentClassId"] == "land.combat-component.infantry"
                and item["components"][0]["componentId"] == static_item["components"][0]["componentId"]
                and item["readiness"]["waterStatus"] == item["readiness"]["storesStatus"] == "distributed-for-stage"
                and item["readiness"]["pinned"] is False
                # At or below the assault surrender threshold, this provisional assessor has
                # no typed automatic-surrender path. Reject even if another fact looks empty.
                and item["operationalState"]["cohesionLevel"] > -17
                and item["operationalState"]["capabilityPointsExpended"]["denominator"] == 1):
            return None
    seen = set()
    for representation in representations:
        ids = representation["boundElementIds"]
        if not (representation["bindingKind"] == "independent-element" and len(ids) == 1
                and ids[0] in by_id and ids[0] not in seen
                and representation["currentLocationId"] == by_id[ids[0]]["currentLocationId"]):
            return None
        seen.add(ids[0])
    if seen != set(by_id):
        return None
    acting = state["activeCycle"]["actingSide"]
    actors = [x for x in static if x["sideId"] == acting]
    defenders = [x for x in static if x["sideId"] != acting]
    if len(actors) != 1 or len(defenders) != 1:
        return None
    return by_id[actors[0]["elementId"]], by_id[defenders[0]["elementId"]]


def physical_shape(state, content):
    pair = exact_selected_profile(state, content)
    if pair is None:
        return "unresolved", None
    if any(edge["features"] for edge in content["edges"]):
        return "unresolved", None
    actor, defender = pair
    edges = {frozenset((edge["firstLocationId"], edge["secondLocationId"]))
             for edge in content["edges"] if edge["features"] == []}
    adjacent = frozenset((actor["currentLocationId"], defender["currentLocationId"])) in edges
    # An earlier paid settlement is history. No current compulsory assault exists for this exact
    # two one-stacking-point infantry profile. Never turn other profiles into an empty offer.
    if not adjacent:
        return "provisional-empty", None
    if actor["ammunition"]["points"] == 0 and state["profile"] == "paid":
        return "provisional-empty", None
    fresh = (state["profile"] == "armed" and not state["targetUses"]
             and not state["priorCycleTargetUses"] and not state["attackHistory"]
             and not world_has_settlement(state["world"])
             and actor["ammunition"]["points"] == defender["ammunition"]["points"] == 10
             and actor["components"][0]["currentToe"] == defender["components"][0]["currentToe"] == 10
             and actor["operationalState"]["cohesionLevel"] == defender["operationalState"]["cohesionLevel"] == 0
             and actor["operationalState"]["capabilityPointsExpended"]["numerator"] == 4
             and defender["operationalState"]["capabilityPointsExpended"]["numerator"] == 0)
    if not fresh:
        return "unresolved", None
    locations = {entry["locationId"]: entry for entry in content["locations"]}
    if not all(locations[unit["currentLocationId"]]["terrainId"] == "land.terrain.clear"
               for unit in pair):
        return "unresolved", None
    if not placement_shape(state, content, pair):
        return "unresolved", None
    return "provisional-positive", pair


def world_has_settlement(world):
    return bool(world["relationships"] or world["settlements"] or world["custodyLots"])


def placement_shape(state, content, pair):
    """Check fixed graph routes only; result, custody quantities and rules remain separate."""
    actor, defender = pair
    locations = {item["locationId"]: item for item in content["locations"]}
    if len(locations) != 6 or any(item["terrainId"] != "land.terrain.clear"
                                  for item in locations.values()):
        return False
    graph = {key: set() for key in locations}
    for edge in content["edges"]:
        left, right = edge["firstLocationId"], edge["secondLocationId"]
        if edge["features"] or left not in graph or right not in graph or left == right:
            return False
        graph[left].add(right)
        graph[right].add(left)
    if len(content["edges"]) != 5:
        return False

    def distance(start, end, blocked=()):
        if start not in graph or end not in graph:
            return None
        blocked = set(blocked) - {start}
        todo = deque([(start, 0)])
        seen = {start}
        while todo:
            node, count = todo.popleft()
            if node == end:
                return count
            for other in graph[node] - seen - blocked:
                seen.add(other)
                todo.append((other, count + 1))
        return None

    actor_location = actor["currentLocationId"]
    defender_location = defender["currentLocationId"]
    if distance(actor_location, defender_location) != 1:
        return False
    actor_side = state["activeCycle"]["actingSide"]
    defender_side = "commonwealth" if actor_side == "axis" else "axis"
    anchors = {item["sideId"]: item["locationId"]
               for item in content["scenarios"][0]["retreatSupplyAnchors"]
               if item["kind"] == "friendly-supply-direction"}
    if set(anchors) != {"axis", "commonwealth"}:
        return False
    prior_supply_distance = distance(defender_location, anchors[defender_side])
    if prior_supply_distance is None:
        return False
    occupied = {actor_location, defender_location}
    retreat = [node for node in graph[defender_location] - occupied
               if distance(node, anchors[defender_side]) == prior_supply_distance - 1
               and distance(node, actor_location) == 2]
    if len(retreat) != 1:
        return False
    destination = retreat[0]
    # Both capture roles and both defender dispositions. The selected clear graph gives
    # at most two guard hexes and one reunion hex; exact settlement remains unadmitted.
    for defender_moves in (False, True):
        final = {actor["elementId"]: actor_location,
                 defender["elementId"]: destination if defender_moves else defender_location}
        for victim, captor in ((actor, defender), (defender, actor)):
            origin = victim["currentLocationId"]
            victim_final = final[victim["elementId"]]
            captor_final = final[captor["elementId"]]
            guard_hexes = distance(origin, captor_final, (victim_final,))
            reunion_hexes = distance(origin, victim_final)
            if guard_hexes is None or guard_hexes > 3 or reunion_hexes is None or reunion_hexes > 4:
                return False
    return True


def private_candidate_preimage(state, pair):
    actor, defender = pair
    side = state["activeCycle"]["actingSide"]
    representations = {item["boundElementIds"][0]: item["representationId"]
                       for item in state["world"]["representations"]}
    values = dict(contractVersion=1, creationBinding=state["world"]["creationBinding"],
                  cycleId=state["cycleId"], positionId=state["sequencePosition"]["positionId"],
                  stateVersion=state["stateVersion"], prefix=state["prefix"],
                  basis="voluntary-adjacent", targetLocationId=defender["currentLocationId"],
                  attackerOriginalSide=side, attackerElementId=actor["elementId"],
                  attackerComponentId=actor["components"][0]["componentId"],
                  attackerRepresentationId=representations[actor["elementId"]],
                  attackerLocationId=actor["currentLocationId"],
                  defenderOriginalSide="commonwealth" if side == "axis" else "axis",
                  defenderElementId=defender["elementId"],
                  defenderComponentId=defender["components"][0]["componentId"],
                  defenderRepresentationId=representations[defender["elementId"]],
                  defenderLocationId=defender["currentLocationId"])
    fields = SCHEMA["privateCandidatePreimageFields"]
    require(len(fields) == len(set(fields)) and set(fields) == set(values))
    return canonical({field: values[field] for field in fields})


def private_candidate(state, pair):
    data = SCHEMA["candidateIdentityDomain"].encode("ascii") + b"\0" + private_candidate_preimage(state, pair)
    return "p01." + hashlib.sha256(data).hexdigest()


def assess(source_bytes, parent_ledgers, r03_ledger):
    """Authenticate exact R03 suffix and return no admitted action."""
    source, _, state = r03.replay(source_bytes, parent_ledgers, r03_ledger)
    require(len(parent_ledgers) == 11 and len(r03_ledger) == 1
            and len(source["breakdownEventCanonicalUtf8"]) == 1
            and state["closed"] and state["phase"] == "breakdown"
            and state["activeCycle"]["ordinal"] == 2
            and state["originalMovementEnd"]["ordinal"] == 1
            and state["movementEnd"]["ordinal"] == 2
            and state["sequencePosition"]["positionId"].endswith(".combat.position-determination")
            and state["breakdownCompletionReceiptId"] == state["breakdownReceipts"][0]["receiptId"])
    content = selected_content()
    shape, pair = physical_shape(state, content)
    result = dict(contractVersion=1, status="unsupported", physicalShape=shape,
                  placementShapeVerified=pair is not None,
                  admitted=False, sourceHash=digest(r03.raw(source, "Source")),
                  worldHash=digest(canonical(state["world"])),
                  randomHash=digest(canonical(state["randomState"])),
                  activeCycleId=state["cycleId"], positionId=state["sequencePosition"]["positionId"],
                  privateCandidateId=private_candidate(state, pair) if pair is not None else None,
                  publicActions=[])
    require(list(result) == SCHEMA["assessmentFields"])
    return result
