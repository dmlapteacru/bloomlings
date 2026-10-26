# Cell pictures from an image

Reads a sheet of cell pictures (such as the owner's example of 2026-10-26 for levels 4–10) back into picture modules,
so they can be checked with `pictures.mjs verify` and recorded like any drawn batch.

```sh
python3 tools/rounds/from-image/extract.py <sheet.png> /tmp/cells.json      # panel boxes and masks inside the script
python3 tools/rounds/from-image/quant.py /tmp/cells.json content/rounds/pictures/src/reference-0004-0010.mjs
node tools/rounds/pictures.mjs verify content/rounds/pictures/src/reference-0004-0010.mjs --out /tmp/ref.png
```

Needs Python 3 with numpy and Pillow. The panel boxes, masks and the colour map are set per sheet in the scripts.
