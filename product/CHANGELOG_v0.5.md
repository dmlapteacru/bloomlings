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
