"""Regression checks for source-atlas panel identity; no game artwork is generated here."""
import importlib.util
import unittest
from pathlib import Path
from PIL import Image, ImageDraw

spec=importlib.util.spec_from_file_location('art_atlas',Path(__file__).with_name('import-art-atlas.py'))
atlas=importlib.util.module_from_spec(spec)
spec.loader.exec_module(atlas)

class AtlasPanelTests(unittest.TestCase):
    @staticmethod
    def sheet():
        image=Image.new('RGB',(280,280),(250,247,230))
        draw=ImageDraw.Draw(image)
        for row,shift in enumerate((-5,5,-5,5,0)):
            for col in range(4):
                x=12+col*66+shift;y=8+row*54
                draw.rectangle((x,y,x+55,y+45),fill=(140,240,250))
        return image

    def test_row_offsets_keep_twenty_distinct_panels(self):
        image=self.sheet()
        self.assertLess(len(atlas.gutters(image,0)),5)
        columns,rows=atlas.panel_boundaries(image,4,5)
        self.assertEqual(len(rows),6)
        self.assertEqual([len(row)-1 for row in columns],[4]*5)
        self.assertNotEqual(columns[0],columns[1])

    def test_cropped_outer_margin_does_not_remove_first_panel(self):
        image=self.sheet().crop((0,8,280,280))
        columns,rows=atlas.panel_boundaries(image,4,5)
        self.assertEqual(len(columns),5)
        self.assertEqual(rows[0],(-1,-1))

    def test_missing_internal_row_is_rejected(self):
        image=self.sheet().crop((0,0,280,216))
        with self.assertRaises(ValueError):
            atlas.panel_boundaries(image,4,5)

if __name__=='__main__':
    unittest.main()
