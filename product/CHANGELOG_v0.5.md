# Bloomlings Pre-production v0.5 — Revision Notes

## Added

### `15_TECHNICAL_ARCHITECTURE.md`

High-level technical direction is now documented.

## Technical decisions

1. Unity selected as client engine.
2. C# selected as gameplay/runtime language.
3. Product remains strictly 2D.
4. Core gameplay simulation should be deterministic and independent from visual animation timing.
5. Levels are data definitions, not Unity scenes.
6. 5000+ launch levels are represented as versioned content data.
7. Generator and Solver are first-class offline/content-pipeline systems.
8. Main gameplay is offline-first.
9. Backend is limited primarily to meta systems such as cloud save, leaderboard, remote config and validated economy operations.
10. Future content can be delivered separately from the binary through versioned content packs/CDN-style delivery.
11. Mobile stack must support ads, IAP, analytics, crash reporting and remote config.
12. CI/CD must validate game logic and level content, not only compile the app.
13. Exact backend vendor, analytics vendor, DI framework, UI implementation, class design and project structure remain intentionally undecided.

## Documentation package

v0.5 contains only the latest active documentation plus:
- current Locked Concept;
- current Changelog;
- new Technical Architecture document.

## Revision 2026-09-29 — picture-first levels

Source: clarifications recorded in `specs/001-core-game-mvp/spec.md` (session 2026-09-29).

### Decisions

1. Every level is a **picture-first mosaic**:
   - built from a reviewed base picture made of color roles (max ~14×16 cells);
   - the role → variant mapping fixes the visible top layer of every cell, so the board reads as the subject;
   - hidden layers, keys and specials may deviate while the subject stays recognizable.
2. Cleared cells reveal the **finished version of the same picture**:
   - it is rendered automatically by default;
   - bespoke illustrations are optional;
   - on Win the picture goes to the Collection.
3. The generator is **picture-first + solution-first**:
   - the dependency graph is derived from the picture, which replaces the earlier "graph first, then spatialize" step;
   - pods and the Source Tray are designed around at least one planned solution;
   - the solver validates every level.
4. **Picture reuse** is allowed through remapping, mirroring, background and Source design, within these limits:
   - Levels 1–100 each use a different base picture;
   - the same base picture never appears within 50 consecutive levels;
   - estimated library size is roughly 1000–1500 base pictures for 5000 levels.
5. **No lives**: failed or abandoned attempts are free and restarts are unlimited. This confirms `10_ECONOMY_AND_MONETIZATION.md` §3.

### Updated documents

- `05_LEVEL_STRUCTURE.md` — §1, §4, §5 (Base picture), §6, §11, §18.
- `06_LEVEL_GENERATOR.md` — §1, §3–§10, §19–§22, new §23 (Picture library).
- `12_ART_AND_CONTENT_PIPELINE.md` — §7, §10 (Picture library), §12–§14.
- `14_MVP_SCOPE.md` — §4, §8, §9.
- `15_TECHNICAL_ARCHITECTURE.md` — §4, §6, §7, §8, §17.

Each updated document carries a `Revision 2026-09-29` note under its status line.
