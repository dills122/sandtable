# @sandtable/field-atlas

Internal Angular design-system library for Maproom. Its public API exports `AtlasChoiceSelect`,
`AtlasPanel`, `AtlasStatus`, `AtlasUnitCounter`, `AtlasIconButton`, `AtlasStatusTone`, `UnitKind`,
and 15 named Lucide icon aliases documented in [ICONOGRAPHY.md](ICONOGRAPHY.md).

- `src/styles/_tokens.scss`: canonical implemented semantic colors, typography, spacing and motion.
- `src/styles/_material.scss`: Angular Material public theme APIs only.
- `src/styles/styles.scss`: local fonts, baseline typography, focus and text actions.
- `fa-choice-select`: token-backed Angular Material choice control for compact choices. Pass a
  visible label, options, value, and handle `valueChange`. Its trigger and open menu use Field Atlas
  tokens; pointer or keyboard activation of its label focuses and opens the control. Keyboard,
  screen-reader, touch, and small-viewport behavior use Material's select overlay.
  See **Foundations / Choice select** in Storybook.
- `src/styles/utilities.css`: Tailwind utilities and semantic bridge, without preflight.
- `stories`: foundations, upstream Material usage, custom component states and design guidance.
- `licenses`: bundled font notices and Lucide ISC/Feather MIT notices; font assets come from pinned
  Fontsource packages.

From the workspace root, run `npm run storybook` or `npm run build:lib`.
See the [workspace guide](../../README.md) for setup and checks.

Presentation components take inputs and emit user intent. No app state, network service,
simulation behavior or authoritative game contract belongs here. Use Material directly unless
shared behavior justifies a wrapper. Feature-specific patterns stay in their app and appear in
Storybook without reversing the dependency direction.
