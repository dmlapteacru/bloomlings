# Profile: game composition concepts

Owner feedback: the current Profile and editor drift toward an application UI. These are body redesign proposals, not implemented screens.

## Direction

- Replace the broad rectangular player card with a compact framed portrait stand, a name ribbon, and a short wooden level tag.
- Present achievements as one medal display with shallow sockets and ledges. Existing emblems already carry their own frames; avoid another identical rectangular card around every emblem.
- Present Avatar selection as one inventory tray containing recessed portrait slots. Retain three columns, the actual catalog, prices, selection state, standard green purchase buttons, and a fixed Save footer.
- Keep the existing garden background, top navigation and connected cream editor tabs. Reuse existing heroes. Preview geometry must remain shared across all four editor tabs.

## Implementation considerations after approval

Use bitmap art for the distinctive portrait stand and display/tray rim, preserving their illustrated silhouette. Slice simple tray surfaces only; do not stretch a whole decorated illustration into an arbitrary rectangle. Render all names, prices, progress, checks and buttons separately with the shared responsive layout in native Unity and the playtest renderer. Use the actual existing portrait and emblem assets in production; generated concepts are not replacements for the character catalog.

Inventory density and clear touch targets remain requirements. Avatar and Badge retain equal three-column slot sizes. Body scroll remains independent from navigation, preview and footer. No new mechanics, rewards or products are proposed.

## Generation

Generated with the built-in OpenAI ImageGen tool using current main Profile, current Avatar editor and the existing Bloom portrait as references. Exact prompts are in `prompts.json`; generated output paths are recorded in `outputs.json`. The images are design proposals: final pixel placement, exact art reuse and responsiveness require implementation and rendered review after choosing the direction.
