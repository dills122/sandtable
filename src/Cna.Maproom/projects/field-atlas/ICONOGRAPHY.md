# Field Atlas iconography

Use named exports from `@sandtable/field-atlas`. Each alias re-exports one pinned
`@lucide/angular` standalone icon, so importing one does not require a runtime
icon lookup or a bundled icon set. Keep icons at 20px in map controls and 18px
beside text, with Lucide's default outline stroke and `currentColor`. Avoid
weapon glyphs and decorative icons in operational controls.

| Action             | Export / Lucide glyph                    | Control copy              |
| ------------------ | ---------------------------------------- | ------------------------- |
| Zoom in            | `AtlasZoomInIcon` / `ZoomIn`             | Zoom in                   |
| Zoom out           | `AtlasZoomOutIcon` / `ZoomOut`           | Zoom out                  |
| Fit full map       | `AtlasFitMapIcon` / `Scan`               | Fit map                   |
| Center selection   | `AtlasLocateIcon` / `LocateFixed`        | Locate selected formation |
| Open roster panel  | `AtlasOpenPanelIcon` / `PanelLeftOpen`   | Open roster               |
| Close roster panel | `AtlasClosePanelIcon` / `PanelLeftClose` | Close roster              |
| Expand map         | `AtlasExpandIcon` / `Expand`             | Expand map                |
| Contract map       | `AtlasContractIcon` / `Shrink`           | Contract map              |
| Search formations  | `AtlasSearchIcon` / `Search`             | Search formations         |
| Sort formations    | `AtlasSortIcon` / `ArrowDownUp`          | Sort formations           |
| Clear search       | `AtlasClearIcon` / `X`                   | Clear search              |
| Review order       | `AtlasReviewIcon` / `ClipboardCheck`     | Review order              |
| Issue order        | `AtlasIssueIcon` / `Send`                | Issue order               |
| Refresh view       | `AtlasRefreshIcon` / `RefreshCw`         | Refresh view              |
| Cancel draft       | `AtlasCancelIcon` / `Ban`                | Cancel draft              |

Use `AtlasIconButton` only for familiar, reversible map and panel actions with
short, context-specific names. Its required `label` supplies accessible name
and native hover title; its projected icon is hidden from assistive technology.
The button supplies a 44px minimum target and visible keyboard focus. Set
`pressed` only for a persistent toggle; omit it for ordinary actions. Use the
opposite open/close or expand/contract glyph and copy when state changes.
Do not show both members of a pair for the same target at once.

Use text buttons for reviewing, issuing, refreshing, cancelling, or any action
with a material consequence or unclear target. Place an 18px icon before text
when it helps scanning, and mark that SVG `aria-hidden="true"`. Disabled actions
need nearby explanation; a disabled icon and hover title alone do not explain
why an action is unavailable. Use a local translated string for button copy,
accessible name, and title rather than separate labels that can drift.

- Lucide Angular usage: https://lucide.dev/guide/angular
- Lucide accessibility: https://lucide.dev/how-to/accessibility
- Lucide license: https://github.com/lucide-icons/lucide/blob/main/LICENSE

The pinned Angular package's ISC and inherited Feather MIT notices are copied
without changes to `licenses/lucide.txt`, which is included in the built Field
Atlas package.
