# Pilot v9

Separate 30-level experiment based on v8E frozen at c6b0bfd4; old catalogs are immutable controls.
Only L4/L10/L12/L13/L16/L25 are replaced. Production catalog and APK are untouched.

- `levels.json`: self-contained definitions, catalog hash, base hash and changed ids.
- `certificates.json`: patient live winning routes with their visible-horizon policy options.
- `quality.json`: bot diagnostics, independent seed checks on replacements and four explicit timing policies per level.

See [design and evidence](../../product/audits/pilot-v9-plan-2026-10-24.md), [playable build](../../product/previews/gameplay-pilot-v9/README.md).

Rebuild the checked-in HTML from these definitions:

```sh
node tools/gameplay-pilot-v9/rhythm-check.mjs
node tools/gameplay-pilot-v9/verify.mjs
python3 tools/gameplay-pilot-v9/build.py
```

The generator is for content experiments, not the installed launch catalog. To regenerate a replacement use `node tools/gameplay-pilot-v9/generate.mjs level ID [startAttempt]`, then `assemble` after all six finalists pass. Search can be expensive. Its work/logs are ignored. Do not regenerate unchanged controls. Bot win rates are policy measurements, not human probabilities. The early-error diagnostic is optimistic when its search budget runs out.
