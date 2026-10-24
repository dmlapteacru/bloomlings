using System;
using System.Collections.Generic;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Cached/offline honor board; preview fixtures can supply real-shaped rows without inventing live ranks.</summary>
    public static class LeaderboardScreen
    {
        public static void Draw(IPainter p,DesignApp app)
        {
            var look=HomeScreen.Look(app);
            DesignApp.DrawBackdrop(p,BackdropScene.Home,app.Meta.CurrentLevel,OwnerPictures.Leaderboard);
            if(!BottomNav.IsOpen(NavPlace.Leaderboard,look)){Locked(p,app,look);return;}
            var r=LeaderboardHonorDesign.Page(p.Width,p.Height,p.Insets);
            LeaderboardPage? page=app.PreviewLeaderboard;
            p.Mark("ui.leaderboard.illustrations");p.Sprite(MetaScreenArt.Podium,r.Podium);
            for(int rank=1;rank<=3;rank++)
            {
                LeaderboardEntry? entry=null;if(page!=null)foreach(var candidate in page.Entries)if(candidate.Rank==rank){entry=candidate;break;}
                if(entry!=null)Portrait(p,r.Portrait(rank),entry,app);
                string displayName=entry==null?"—":DisplayName(entry);
                Box name=LeaderboardHonorDesign.NamePlate(r.Name(rank),p.MeasureText(displayName,LeaderboardHonorDesign.SmallName));
                Skin(p,MetaScreenArt.Plank,name);Name(p,name.Inset(name.Width*.08f,0),displayName,LeaderboardHonorDesign.SmallName);
                Skin(p,MetaScreenArt.Tag,r.Level(rank));ProfileGamePainter.Label(p,r.Level(rank).Inset(r.Level(rank).Width*.08f,0),entry==null?"—":PlaytestText.F("common.level",entry.Level),WardrobeShelfDesign.Status);
                ProfileGamePainter.Label(p,r.Medal(rank),rank.ToString(),LeaderboardHonorDesign.Number);
            }
            var rows=new List<LeaderboardEntry>();if(page!=null)foreach(var entry in page.Entries)if(entry.Rank>3&&!entry.IsPlayer)rows.Add(entry);
            rows.Sort((a,b)=>a.Rank.CompareTo(b.Rank));
            app.LeaderboardScrollOffset=Math.Max(0,Math.Min(r.Maximum(rows.Count),app.LeaderboardScrollOffset));
            p.PushClip(r.Viewport);
            for(int i=0;i<rows.Count;i++)
            {
                Box row=r.Row(i).Offset(0,-app.LeaderboardScrollOffset);
                if(row.Bottom>r.Rows.Top&&row.Top<r.Rows.Bottom)Row(p,row,rows[i],app);
            }
            p.PopClip();
            p.VerticalScroll(r.Viewport,dy=>app.LeaderboardScrollOffset=Math.Max(0,Math.Min(r.Maximum(rows.Count),app.LeaderboardScrollOffset-dy)));
            var own=page?.Player??new LeaderboardEntry(0,PlaytestText.T("leaderboard.you"),app.Meta.Progression.HighestCompletedLevel,true);
            Row(p,r.Player,own,app);
            ProfileGamePainter.Label(p,new Box(r.Status.Left,r.Status.Top,r.Status.CenterX,r.Status.Bottom),PlaytestText.T("leaderboard.offline"),WardrobeShelfDesign.Status);
            Kit.SecondaryButton(p, RulesButton(p), PlaytestText.T("leaderboard.rules"), () => app.OpenOverlay(Overlay.CompetitionRules));
            Kit.RoundButton(p,r.Refresh.CenterX,r.Refresh.CenterY,r.Refresh.Width,"ui.restart",()=>app.HomeToast(PlaytestText.T("leaderboard.offline_empty")));
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Leaderboard),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("leaderboard.title"),app.CloseLeaderboard,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
            if(app.HomeToastText!=null)Kit.Toast(p,r.Player,app.HomeToastText);
        }
        public static Box RulesButton(IPainter p)
        {
            var r = LeaderboardHonorDesign.Page(p.Width,p.Height,p.Insets);
            return new Box(r.Status.CenterX, r.Status.CenterY-p.U(DesignTokens.Size.TouchMin)*.5f,
                r.Refresh.Left-p.U(12), r.Status.CenterY+p.U(DesignTokens.Size.TouchMin)*.5f);
        }
        public static void Rules(IPainter p, DesignApp app, float since)
        {
            p.Mark("ui.leaderboard.rules");
            CardRegions r = Kit.Card(p, 1160, PlaytestText.T("leaderboard.rules_title"), app.CardClose, Kit.Pop(since));
            float y = r.Body.Top + p.U(10);
            foreach (string key in new[] { "leaderboard.rules_progress", "leaderboard.rules_week", "leaderboard.rules_score", "leaderboard.rules_fair", "leaderboard.rules_pending" })
                AccountScreen.Paragraph(p,r.Body,ref y,key,3);
            Kit.PrimaryButton(p,ScreenLayout.CardButton(r.Body,y,true,p.Scale),PlaytestText.T("common.close"),app.CardClose);
            Kit.EndCard(p);
        }
        private static void Skin(IPainter p,string asset,Box b)
        {
            p.Mark("ui.leaderboard.illustrations");var border=MetaScreenArt.Borders(asset);
            Kit.SlicedSprite(p,asset,b,border.X,border.Y,border.Y);
        }
        private static void Row(IPainter p,Box row,LeaderboardEntry entry,DesignApp app)
        {
            Skin(p,MetaScreenArt.Plank,row);
            if(entry.IsPlayer)p.FillRound(row.Inset(row.Height*.12f),row.Height*.12f,ShopCatalogDesign.ChosenRim.Face.WithAlpha(.28f));
            var parts=LeaderboardHonorDesign.Parts(row);
            ProfileGamePainter.Label(p,parts.Rank,entry.Rank>0?NumberText.Group(entry.Rank):"—",LeaderboardHonorDesign.Number);
            Portrait(p,parts.Avatar,entry,app);
            Name(p,parts.Name,DisplayName(entry),LeaderboardHonorDesign.NameType);
            Skin(p,MetaScreenArt.Tag,parts.Level);
            ProfileGamePainter.Label(p,parts.Level.Inset(parts.Level.Width*.10f,0),NumberText.Group(entry.Level),LeaderboardHonorDesign.Number);
        }
        private static string DisplayName(LeaderboardEntry entry)=>entry.IsPlayer?PlaytestText.T("leaderboard.you"):string.IsNullOrWhiteSpace(entry.Appearance?.Name??entry.Name)?PlaytestText.T("leaderboard.anonymous"):entry.Appearance?.Name??entry.Name;
        private static void Name(IPainter p,Box box,string full,TypeStyle type)
        {
            float size=Math.Min(p.U(type.Size),box.Height*.78f),scale=size/p.U(type.Size);
            string fitted=LeaderboardHonorDesign.FitName(full,box.Width,text=>p.MeasureText(text,type,scale));
            ProfileGamePainter.Label(p,box,fitted,type);
        }
        private static void Portrait(IPainter p,Box box,LeaderboardEntry entry,DesignApp app)
        {
            ProfileLook style=entry.IsPlayer?app.Meta.Wardrobe.Profile:LeaderboardHonorDesign.Look(entry.Appearance,app.Meta.Wardrobe.Catalog);
            AvatarItem? avatar=entry.IsPlayer?app.Meta.Profile.Avatar:AvatarCatalog.Get(entry.Appearance?.Avatar);
            if(avatar!=null)Kit.Avatar(p,box,avatar,style.Frame,style.Badge,null,sceneFrame:true);
            else {p.Mark("ui.person");p.Shape("ui.person",box,ShopBodyDesign.Card.Line);}
        }
        private static void Locked(IPainter p,DesignApp app,HomeLook look)
        {
            var r=ScreenLayout.LockedPage(p.Width,p.Height,p.Insets);Kit.CardFrame(p,r.Panel);
            Kit.LockedNotice(p,r.Notice,NavPlace.Leaderboard,app.UnlockLevel(NavPlace.Leaderboard));
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Leaderboard),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("leaderboard.title"),app.CloseLeaderboard,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
        }
    }
}
