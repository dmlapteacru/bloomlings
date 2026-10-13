using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    public static partial class UiKit
    {
        private static GardenButton IllustratedPrimary(string name,Transform parent,bool decorate,bool hero)
        {
            GardenButton view=NewButton(name,parent,GardenLook.Green,true);
            view.GreyWhenDisabled=false;view.FadeWhenDisabled=true;
            RectTransform face=UiFactory.Stretch(UiFactory.CreateRect("Face",view.Body));
            Image art=UiFactory.CreateImage("Illustration",face,NavigationArt.Get(hero ? PrimaryActionDesign.Play : decorate ? PrimaryActionDesign.Decorated : PrimaryActionDesign.Plain),Color.white);
            art.type=Image.Type.Sliced;art.preserveAspect=false;UiFactory.Stretch(art.rectTransform);
            RectTransform content=UiFactory.Stretch(UiFactory.CreateRect("Content",face));
            BoxLayout layout=BoxLayout.On(face);
            BoxLayout.On(view.Body).Then(b=>{art.pixelsPerUnitMultiplier=(hero ? 636f : decorate ? 430f : 384f)/Mathf.Max(.01f,b.Height);view.SetGeometry(0,0,b.Height*.025f,b.Height*.5f);});
            view.BuildPictureFace(face,art,content,layout,_=>art.color=Color.white);
            return view;
        }
    }
}
