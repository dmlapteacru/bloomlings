using Bloomlings.Client.UI.Design;
using TMPro;
using UnityEngine;
using C=Bloomlings.Client.UI.Design.DesignTokens.Colors;
namespace Bloomlings.Client.UI
{
    public sealed class ToastView:MonoBehaviour
    {
        private TextMeshProUGUI _text=null!;private float _until;
        public static ToastView Create(Transform parent)
        {
            var panel=UiKit.CardFrame("Message",parent,false);UiFactory.Place(panel.rectTransform,.08f,.74f,.92f,.8f);
            var view=panel.gameObject.AddComponent<ToastView>();
            view._text=UiKit.Label("Text",panel.transform,string.Empty,DesignTokens.Type.Body,UiTheme.Of(C.InkBrown),look:TextLook.Plain(C.InkBrown));UiFactory.Place(view._text.rectTransform,.04f,.15f,.96f,.85f);
            panel.gameObject.SetActive(false);return view;
        }
        public void Show(string message){_text.text=message;_until=Time.unscaledTime+2.6f;gameObject.SetActive(true);transform.SetAsLastSibling();}
        private void Update(){if(Time.unscaledTime>=_until)gameObject.SetActive(false);}
    }
}
