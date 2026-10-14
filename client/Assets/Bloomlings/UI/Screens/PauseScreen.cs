using System;
using Bloomlings.Client.UI;
using Bloomlings.Client.UI.Design;
using UnityEngine;
using UnityEngine.UI;
using Bloomlings.Client.UI.Localization;

namespace Bloomlings.Client.UI.Screens
{
    /// <summary>Resume and one connected Restart / Settings / Home tray; leaving and restarting cost nothing.</summary>
    public sealed class PauseScreen : MonoBehaviour
    {
        private GameObject _root = null!;

        public bool IsOpen => _root.activeSelf;

        /// <param name="onSettings">Opens Settings over the pause card; null hides the button.</param>
        public static PauseScreen Create(Transform parent, Action onResume, Action onRestart, Action onLeave, Action? onSettings = null)
        {
            CardView card=UiKit.Card("PauseScreen",parent,Loc.T("pause.title"),PopupDesign.PauseContent,onResume);
            var screen=card.Root.AddComponent<PauseScreen>();screen._root=card.Root;
            Box body=card.Regions.Body;float u=DesignTokens.ScaleFor(UiKit.ScreenBox().Width,UiKit.ScreenBox().Height);
            Button resume=UiKit.PrimaryButton("Resume",card.Body,Loc.T("pause.resume"),onResume);
            UiKit.PlaceBox((RectTransform)resume.transform,PopupDesign.PauseResume(body,u),body);
            Box trayBox=PopupDesign.PauseTray(body,u);RectTransform tray=UiKit.NavigationBar("Actions",card.Body,onSettings != null ? 3 : 2);
            UiKit.PlaceBox(tray,trayBox,body);
            Box[] cells=ScreenLayout.Row(trayBox,onSettings != null ? 3 : 2,0,float.MaxValue,false);
            UiKit.PopupCell(tray,cells[0],trayBox,Loc.T("common.restart"),onRestart,"ui.restart");
            if(onSettings != null) UiKit.PopupCell(tray,cells[1],trayBox,Loc.T("pause.settings"),onSettings,"ui.settings");
            UiKit.PopupCell(tray,cells[cells.Length-1],trayBox,Loc.T("pause.home"),onLeave,picture:"nav-home");
            card.Root.SetActive(false);
            return screen;
        }

        public void Show() => _root.SetActive(true);

        public void Hide() => _root.SetActive(false);
    }
}
