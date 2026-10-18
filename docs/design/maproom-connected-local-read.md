# MAP-08B local browser read lane

This is a development-only, read-only bridge from Angular Maproom to the temporary
Orleans host's **initial** admitted Rules9 fixture cut. It does not submit orders,
advance a phase, persist a campaign, or promise a later `/view`. The existing `/`
workspace remains synthetic; `/connected` is the explicit local host lane.

Start a separate host on loopback, then the connected Angular dev server. The
Angular port 4300 leaves the usual 4200 fixture preview alone:

```sh
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5087 \
  dotnet run --no-launch-profile --project src/Cna.OrleansHost/Cna.OrleansHost.csproj
cd src/Cna.Maproom
npm ci
npm run start:connected
```

Open `http://127.0.0.1:4300/connected`, check fixtures, create and enter one
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

The generated MAP-04 client supplies typed requests and responses. The lane checks
campaign, seat, generation and contract version before rendering. Refresh errors,
including revoked/expired seat responses and identity mismatch, remove the old
view and selection immediately. It uses only the audience-safe `/view`; `503`
stops with an unsupported-phase message. Map and
inspector selections use outward formation IDs and public location coordinates.
No independent legal-action fetch, action submission, opposing formation identity,
or client-derived War Diary entry is present. MAP-09 command ingress, MAP-10 full
connected workspace integration, and MAP-11 real orders remain separate gates.
