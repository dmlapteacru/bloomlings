using System;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Client.UI.Screens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bloomlings.Client.UI
{
    public static class GameNavigationView
    {
        public static Button Button(string name,Transform parent,string asset,Action tap)
        {
            Image image=UiFactory.CreateImage(name,parent,NavigationArt.Get(asset),Color.white,raycast:true);image.preserveAspect=true;
            image.gameObject.AddComponent<PressMotion>();
            Button button=image.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.targetGraphic=image;
            button.onClick.AddListener(()=>{GameFeedback.Current?.Play(SoundCue.Click);tap();});return button;
        }
        public static Image Surface(string name,Transform parent,string asset)
        {
            Image image=UiFactory.CreateImage(name,parent,NavigationArt.Get(asset),Color.white);
            image.type=Image.Type.Sliced;
            var crop=GameNavigationDesign.Crop(asset,asset==PrimaryActionDesign.Reward ? 2030 : 2172,asset==PrimaryActionDesign.Reward ? 775 : 724);
            BoxLayout.On(image.rectTransform).Then(b=>image.pixelsPerUnitMultiplier=crop.Height/Mathf.Max(.01f,b.Height));return image;
        }
        public static TextMeshProUGUI Label(string name,Transform parent,string value,TypeStyle style,TextLook look,float share=.72f)
        {
            var label=ProfileGameView.Label(name,parent,value,style,look);
            BoxLayout.On(label.rectTransform).Then(b=>{float size=Mathf.Min(UiKit.Units(style.Size),b.Height*share);label.fontSizeMax=size;label.fontSizeMin=Mathf.Min(size,UiKit.Units(style.Min));label.fontSize=size;});return label;
        }
    }
}
