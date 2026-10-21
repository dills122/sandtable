# Normal campaign Combat source v1

This contract supplies formation Basic Morale and component offensive Close Assault
ratings to a normal Content 6 campaign. Content 6 already owns component defensive
Close Assault ratings and current TOE. The supplement is part of the authoritative
content catalog, not a client decision or a legacy private Combat replay input.

Canonical UTF-8 JSON has this field order:

```text
contractVersion:1, contentPackId:id, contentHash:sha256,
formationMorale:[{formationId:id,basicMorale:int,sourceId:id,locator:source-atom}],
componentRatings:[{componentId:id,offensiveCloseAssaultRating:int,sourceId:id,locator:source-atom}]
```

Both arrays are sorted by ID and cover **every** formation and combat component
in the exact Content 6 pack once. Basic Morale is -3 through 3; offensive rating
is nonnegative. Every `sourceId` must occur in the pack's source index. The
supplement's SHA-256 identity includes the exact Content 6 hash and all facts and
provenance. Any absent, extra, duplicate, out-of-range, noncanonical, or unindexed
fact fails closed. A pack without a supplement cannot issue a positive native
Close Assault result.

The first catalog resource binds the normal adjacent-infantry Rules Lab pack. Its
synthetic source locators are declared scenario inputs; no rating or morale is
inferred from a hidden World view, an owner submission, or the separate Content 7
historical fixture. A positive assault event must retain this source hash and
verify it on cold replay before a Core random draw or settlement is admitted.

This contract does not itself issue an assault event, alter existing campaign
creation bytes, or make Combat public.
