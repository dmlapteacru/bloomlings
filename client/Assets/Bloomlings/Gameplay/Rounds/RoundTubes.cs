using Bloomlings.Client.Art;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.Gameplay.Rounds
{
    /// <summary>
    /// Draws a link tube (spec 006 FR-043, the kit's <see cref="LinkTube"/> geometry; the playtest's
    /// <c>Kit.LinkTubeStepped</c>) as one turned image in a parent's top-down canvas units. Its length is rounded up to whole
    /// steps of 8 px about its middle, so a tube between sliding pods reuses its cached picture (its ends tuck under the pods,
    /// which hide the step). Hosts keep the tube images before the pods and places in the sibling order, so a tube never lies
    /// over a pod.
    /// </summary>
    public static class RoundTubes
    {
        public static Image Create(string name, Transform parent)
        {
            Image image = UiFactory.CreateImage(name, parent, null, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = false;
            return image;
        }

        /// <summary>Shows <paramref name="tube"/> (in the parent's top-down canvas units) on <paramref name="image"/>.</summary>
        public static void Show(Image image, LinkTubeShape tube)
        {
            float ppu = Mathf.Max(0.0001f, UiKit.PixelsPerUnit);
            float lengthPx = tube.Length * ppu;
            if (lengthPx < 1f)
            {
                image.gameObject.SetActive(false);
                return;
            }

            int steppedPx = Mathf.Max(8, Mathf.CeilToInt(lengthPx / 8f) * 8);
            int thicknessPx = Mathf.Max(4, Mathf.RoundToInt(tube.Thickness * ppu));
            (float mx, float my) = tube.Middle;
            BoxLayout.Place(image.rectTransform, Box.FromCenter(mx, my, steppedPx / ppu, thicknessPx / ppu));
            image.rectTransform.localEulerAngles = new Vector3(0f, 0f, -tube.Degrees);
            image.sprite = RoundSprites.LinkTube(tube, steppedPx, thicknessPx);
            image.type = Image.Type.Simple;
            image.gameObject.SetActive(true);
        }
    }
}
