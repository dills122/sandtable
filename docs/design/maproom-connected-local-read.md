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

Creation retries reuse the same request ID while the page stays open; the host
retains that reply only for the operator session lifetime (15 minutes). A lost
seat response or browser reload can still require a fresh campaign because the
grant is one-use and this lane does not yet provide full reconnect recovery.

The generated MAP-04 client supplies typed requests and responses. The lane checks
campaign, seat, generation and contract version before rendering. It uses only the
audience-safe `/view`; `503` stops with an unsupported-phase message. Map and
inspector selections use outward formation IDs and public location coordinates.
No independent legal-action fetch, action submission, opposing formation identity,
or client-derived War Diary entry is present. MAP-09 command ingress, MAP-10 full
connected workspace integration, and MAP-11 real orders remain separate gates.
