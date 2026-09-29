# Contracts: Bloomlings Launch Game

| Contract | Kind | Consumers |
|---|---|---|
| [level-definition.schema.json](level-definition.schema.json) | JSON Schema (level-definition.v1) | Pipeline, client loader, solver |
| [base-picture.schema.json](base-picture.schema.json) | JSON Schema (base-picture.v1) | Picture import, generator, finished-look renderer |
| [content-manifest.schema.json](content-manifest.schema.json) | JSON Schema (content-manifest.v1) | Publish step, client content service |
| [player-save.schema.json](player-save.schema.json) | JSON Schema (player-save.v1) | Client save system, Cloud Save |
| [simulation-api.md](simulation-api.md) | C# API + event contract | Unity presentation, solver, generator, tests |
| [pipeline-cli.md](pipeline-cli.md) | CLI contract | Content team, CI |
| [backend-services.md](backend-services.md) | Service interfaces, Cloud Code, Remote Config keys | Client services layer |
| [analytics-events.md](analytics-events.md) | Event catalog | Client, analytics |

Versioning: each schema is `v1`. A breaking change creates `v2` together with a loader migration. A level definition
never changes in place (FR-076).
