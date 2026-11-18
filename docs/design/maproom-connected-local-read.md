# MAP-08B local browser read lane

This is a development-only, read-only bridge from Angular Maproom to the temporary
Orleans host's **initial** admitted Rules9 fixture cut. It does not submit orders,
advance a phase, persist a campaign, or promise a later `/view`. The existing `/`
fixture study remains synthetic; the main Maproom route offers local campaign read mode
at `/?mode=campaign`. `/connected` remains a development deep link to that view.

Start a separate host on loopback, then the connected Angular dev server. The
Angular port 4300 leaves the usual 4200 fixture preview alone:

```sh
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5087 \
  dotnet run --no-launch-profile --project src/Cna.OrleansHost/Cna.OrleansHost.csproj
cd src/Cna.Maproom
npm ci
npm run start:connected
```

Open `http://127.0.0.1:4300/`, choose **Local campaign read mode** (or open
`http://127.0.0.1:4300/?mode=campaign` directly), check fixtures, create and enter one
seat, then inspect the returned map, formations, ledger and diary. The dev proxy
only forwards `/api/v1` to `127.0.0.1:5087` and rewrites Host and Origin to that
same loopback origin. The browser sends same-origin requests to port 4300;
operator and seat cookies remain HttpOnly. CSRF and one-use grant values remain
in Angular memory and never enter a URL. Never expose either port on a public
interface or use this proxy in a deployed build.

Creation retries reuse the same request ID while the original page and operator
session remain active (at most 15 minutes). The host holds exactly one campaign
per process and only that operator/request/fixture tuple can retrieve the retained
creation reply. A different request or expired operator session gets `409 conflict`
rather than a second campaign. One-use grants are not renewed. A page reload with
the same valid seat cookie can read the existing seat through the scoped session
endpoint; the browser does not redeem another grant. If the cookie or host process
is lost, that seat cannot be reconstructed. Restart the **development** host to
discard its temporary campaign and begin again. There is no campaign listing or
durable save in this lane.

Browser cookies are shared by host across local ports. If another local tab
rotates the session generation for the same seat, Refresh view rechecks the
host-owned seat before accepting its new generation. Choosing the opposing seat
with an existing seat cookie is rejected with the current seat named; it never
opens the opposing view.

The generated MAP-04 client supplies typed requests and responses. The lane checks
campaign, seat, generation and contract version before rendering. Refresh errors,
including revoked/expired seat responses and identity mismatch, remove the old
view and selection immediately. It uses only the audience-safe `/view`; `503`
stops with an unsupported-phase message. Map and
inspector selections use outward formation IDs and public location coordinates.
No independent legal-action fetch, action submission, opposing formation identity,
or client-derived War Diary entry is present. MAP-10 now renders this initial cut in
the operational workspace. MAP-09 has private request binding only; coherent
successor reads/diary, public submit/receipt and MAP-11 real orders remain gates.

## Hot-seat handoff within one browser tab (current behavior)

This is a bounded privacy note on what happens today, in the shipped code, when
one browser tab moves from one seat's view to the other's — not the dedicated
"pass the device" handoff screen MAP-13 plans (gated behind MAP-11/12 and still
undelivered). Maproom writes nothing to `localStorage` or `sessionStorage`
anywhere (confirmed by a repo-wide grep over `maproom-alpha/src`; zero hits), so
there is no persisted-storage key for a handoff to leak and none to clear. All
per-seat state lives only in two in-memory places: `ConnectedWorkspace`'s own
signals (`view`, `selectedId`, camera/fullscreen state) and `CampaignActionFlow`'s
signals (`state`, `currentView`), both plain Angular `signal()`s with no
persistence. `CampaignActionDraft` (`campaign-action-draft.ts`) is a separate,
well-tested model that is **not** wired into any component today — the live
action state machine is `CampaignActionFlow`, constructed by `ConnectedWorkspace`
with no adapter and `admittedCapability` hardcoded `false`. That means `canSubmit`
is permanently false and the submit control is permanently disabled in the
shipped app: there is no reachable "submitted, awaiting receipt" draft today, only
an own-formation selection (`selectedId`) and its (always-disabled) legal-action
labels — this is the closest real analogue to "seat A drafts an action."

There is also no in-app control to switch seats while a view is showing; the
seat-choice buttons only render when `view()` is null, so a same-tab handoff is
only reachable after something has already cleared the current view (a refresh
error, a revoked/expired seat, or — in the fully real flow — the production host
flat-out refusing the other seat to an already-seated browser, per the paragraph
above). Once the view is cleared and a different seat is actually entered, two
things independently discard the previous seat's state: `CampaignActionFlow.
observeView()` detects the campaign/seat mismatch, sets its internal
`capabilityRevoked` flag and resets `state` to `{ phase: 'disabled' }`; and
`ConnectedWorkspace.setView()` always re-derives `selectedId` via
`selectedFormationId(view, previousId, initial)`, which for a fresh seat entry
(`initial = true`) ignores the previous `selectedId` entirely and seeds the new
seat's own first formation instead. Neither path carries the old seat's label
text, formation IDs, or action labels forward.

A new Playwright case, `seat switch after a drafted-but-unsubmitted selection
leaves no axis trace in the commonwealth DOM` in
`src/Cna.Maproom/e2e/connected-read.spec.ts`, exercises this: it drives seat axis
to select a formation and surface its legal-action label (the real "draft" state
described above), forces the same kind of seat/generation mismatch the existing
"mismatched refresh generation removes prior audience view" case uses to clear
the view, then completes a real `resumeSeat()` handoff into commonwealth and
asserts the rendered DOM contains none of axis's formation label, location
label, or action label. As run against this code, the test **passes**: the two
mechanisms above already clear everything the test can observe. This closes one
narrow slice of the gap `docs/design/maproom-play-experience-plan.md` (§5) and
`docs/design/local-game-server-maproom-plan.md` (MAP-13) still flag as open —
specifically, the DOM-content half of "invalidate drafts/DOM/storage/worker-state"
for the state this app actually has today. It does not cover browser history,
dev-tool/heap inspection, in-flight request races, or the future MAP-13 curtain
screen, which remain the open parts of that roadmap item.
