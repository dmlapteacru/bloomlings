# Showcase replay pictures

These 23 unchanged canonical pictures belong to the checked-in legacy mechanic showcases. They were retained from
commit `7b4a8129` before the launch installer pruned the runtime library to the 2500 new pictures plus Daily.
`playtest/check` loads these beside the installed library to replay the existing showcase win/jam certificates.
They are test fixtures, outside all game-asset globs; they do not reintroduce old pictures into the launch or APK.
The original authoring grids and provenance remain in `content/pictures/src`.

The replay harness also reads L1 from its retained `content/curated/level-0001.json` source; the replacement runtime catalog intentionally starts at L11.
