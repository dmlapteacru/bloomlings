using System;
namespace Bloomlings.Client.UI.Design
{
    /// <summary>Calm game windows: one ivory surface, honey wood, Lilita headings, one primary action.</summary>
    public static class PopupDesign
    {
        public const string SoundIcon="navigation/settings-sound",HapticsIcon="navigation/settings-haptics";
        public static bool SettingsIcon(string name)=>name==SoundIcon || name==HapticsIcon;
        public static (int Left,int Top,int Width,int Height) SettingsCrop(string name,int w,int h)
        { Box b=name==SoundIcon?new Box(88,153,860,760):new Box(951,113,1731,772);return ((int)(b.Left*w/1774),(int)(b.Top*h/887),(int)(b.Width*w/1774),(int)(b.Height*h/887)); }
        public const float SettingsContent=12+2*126+12+20+DesignTokens.Size.TouchMin+24+DesignTokens.Size.SecondaryHeight+30;
        public const string Header="navigation/popup-title-v1";
        public static (int Left,int Top,int Width,int Height) HeaderCrop(int w,int h)=>((int)(14f*w/2172),(int)(136f*h/724),(int)(2144f*w/2172),(int)(431f*h/724));
        public const string Frame = "navigation/popup-frame-v1";
        public const float BorderX = .09f, BorderY = .13f;
        public static TypeStyle Title => PrimaryActionDesign.Type(DesignTokens.Type.Title) with { Size = 62 };
        public static TypeStyle Label => PrimaryActionDesign.Type(DesignTokens.Type.ButtonSecondary);
        public const float PauseContent = 24 + DesignTokens.Size.CardPrimaryHeight + 30 + 210 + 24;
        public static Box PauseResume(Box body, float unit) => ScreenLayout.CardButton(body, body.Top + 24 * unit, true, unit);
        public static Box PauseTray(Box body, float unit) { Box resume = PauseResume(body, unit); return new Box(body.Left, resume.Bottom + 30 * unit, body.Right, resume.Bottom + 240 * unit); }
        public static Box CellIcon(Box cell) => Box.FromCenter(cell.CenterX,cell.Top + cell.Height * .29f,cell.Height * .53f,cell.Height * .53f);
        public static Box CellLabel(Box cell) => new Box(cell.Left + cell.Width * .04f,cell.Top + cell.Height * .54f,cell.Right - cell.Width * .04f,cell.Bottom - cell.Height * .18f);
        public static Box RecoveryIcon(Box cell) => Box.FromCenter(cell.CenterX,cell.Top+cell.Height*.24f,cell.Height*.36f,cell.Height*.36f);
        public static Box RecoveryLabel(Box cell) => new Box(cell.Left+cell.Width*.04f,cell.Top+cell.Height*.43f,cell.Right-cell.Width*.04f,cell.Top+cell.Height*.65f);
        public static Box RecoveryTray(JamCardRegions r) => r.Choices.Count == 0 ? new Box(r.Well.Left,r.Well.Bottom,r.Well.Right,r.Well.Bottom) : new Box(r.Well.Left,r.Choices[0].Top-r.W*.025f,r.Well.Right,r.Choices[r.Choices.Count-1].Bottom+r.W*.025f);
        public static (int Left,int Top,int Width,int Height) Crop(int width,int height) => ((int)(45f*width/1489),(int)(68f*height/1056),(int)(1400f*width/1489),(int)(922f*height/1056));
    }
}
