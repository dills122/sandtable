# Author Explanation —019A first-cycle opening

## Intent and plan
Apply existing completion2 event atomically after D1/D2, closing first-cycle Reserve terminal replay,
readback and retries. No new command/event or canonical change.019A supplies genuine Movement-entry
handoff but not Movement adjudication, Task008H general restore or HOST-PUB-001 durable publication.
All16 traces40cuts152 artifacts required by original Reserve contract are now runtime test targets.

## Flow and components
Opening.Replay accepts full trusted request/Created/preamble/Weather/stage plus0–2 Reserve records.
Bounded record dispatch permits empty/designation/completion/designation+completion only. D1 validates
precompletion; D2 ReadEvent reconstructs complete actual predecessor and completion. Models retain
that genuine D1 state and optional typed completion, atomically derive eventprefix and append one
commandreceipt. No exposed API admits caller-supplied state/evidence as authority. Codec writes exact
ReserveState1, reusing guarded D1 World/members and now-internal unchanged D2 WriteCycle.

Apply overloads for designation/completion replay first, validate shape and trusted owner before
consumed occurrence lookup. Exactinput returns originalevent/currentstate. Changedinput or cross-kind
samepriorversion rejects. Freshinput aftercompletion rejects. Otherwise existing D1/D2 generator
produces event and fullReplay reconstructs appended result. ReadState compares entire bytes against
replay. No retained mutable byte arrays; result bytes do not own state.

## Choices and invariants
Wrapper avoids pretending terminal version11/12 is valid D1 precompletion; bounded Worldwriter keeps
its original equality guard. Optional completion changes position/version/prefix/ledger/cycle fields
only. Empty final11/10receipts; I final12/11receipts. Cycle openingPrefix is precompletion prefix,
openedversion equals resultversion, ordinal1, resolved firstside and nullcatalog ActiveSide. Weather,
RNG, World, members/history/order/holder preserved. Output canonical order and receipt/cycle framing
unchanged. Inputcounts/bytes/depth bounded. Trustedmetadata still requires later authenticated ingress.

## Verification and dev review
Tests cover allfrozen artifacts/cuts, independentprefix/receipt/cycle identity, retries at everylatercut,
wrongactors before retry, changed and cross-family occurrence reuse, freshafterterminal, missing/foreign/
reordered/repeated/postcompletion history, raw/scalar/coherently re-signedcycle+cache, bufferownership,
D1terminal rejection and creationSnapshot rejection. Devread sources/tests found no remaining issue;
added outputbuffer mutation and explicit D1terminal rejection beforefreeze. Exact execution evidence
in019a-evidence.md. Canonical Python baseline remains broader independent contract evidence.

## Costs and limits
Repeated bounded predecessor replay and small parallel private-state codec are deliberate finiteprofile
costs. Internal constructors can be called by trusted assembly code; future Movement must consume
completehistory rather than fabricated wrapper. No generalcapacity/performance claim. No publicaction,
Movement/Breakdown/Reaction handling, latercycle, Snapshot12 generalrestore or actualpersistence.
Review exactatomicfield/ledger update, retrycurrentstate, cross-kind conflicts and preservedresources.
