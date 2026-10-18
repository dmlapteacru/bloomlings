using System;
using Bloomlings.Client.Meta.Wardrobe;
using Bloomlings.Client.Services.Feedback;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;

namespace Bloomlings.Playtest.Design
{
    /// <summary>The same compact preview, category memory and culled shelf catalog as the Unity client.</summary>
    public static class WardrobeScreen
    {
        public static void Draw(IPainter p, DesignApp app)
        {
            var look=HomeScreen.Look(app);
            DesignApp.DrawBackdrop(p,BackdropScene.Home,app.Meta.CurrentLevel,OwnerPictures.Wardrobe);
            if(!BottomNav.IsOpen(NavPlace.Wardrobe,look)){Locked(p,app,look);return;}
            var browse=app.WardrobeBrowse; var wardrobe=app.Meta.Wardrobe;
            WardrobeShelfRegions r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
            var items=WardrobeShelfCatalog.Build(wardrobe,browse.Kind,browse.Family);
            browse.SetOffset(browse.Offset,r.Maximum(items.Count));
            Outfit outfit=browse.ShownOutfit(wardrobe);
            p.Mark("ui.wardrobe.illustrations");p.Sprite(MetaScreenArt.Base,r.Stage);
            Visuals.Hero(p,HomeStage.Figure(r.Hero.CenterX,r.Hero.Bottom,r.Hero.Height),browse.Family,outfit);
            Skin(p,MetaScreenArt.Plank,r.Name.Inset(-r.Name.Width*.16f,-r.Name.Height*.12f));
            ProfileGamePainter.Label(p,r.Name,PlaytestText.T("family."+WardrobeService.FamilyKey(browse.Family))+(browse.PreviewItem!=null?" · "+PlaytestText.T("wardrobe.preview"):""),WardrobeShelfDesign.FamilyName);
            p.PushClip(r.Viewport);
            var window=r.Visible(items.Count,browse.Offset);
            for(int i=window.First;i<=window.Last;i++)
            {
                WardrobeShelfItem card=items[i]; CosmeticItem? item=card.Item;
                Box cell=r.Cell(i).Offset(0,-browse.Offset);
                if(cell.Bottom<=r.Viewport.Top||cell.Top>=r.Viewport.Bottom)continue;
                bool selected=browse.Selected(wardrobe,item),hasAction=card.State==WardrobeItemState.ForSale||card.State==WardrobeItemState.Reward||card.State==WardrobeItemState.LevelLocked;
                p.Picture("wardrobe.card/"+selected,cell,(w,h)=>WardrobeShelfDesign.CardSurface(w,h,selected));
                string? asset=WardrobeShelfDesign.ItemPicture(browse.Family,browse.Kind,item);
                Box picture=WardrobeShelfDesign.Picture(cell,hasAction);
                if(asset!=null)p.Sprite(asset,FitItem(asset,picture,item));
                else p.Shape(item!=null?ShapeLibrary.CosmeticId(item.Shape):"ui.close",item!=null?picture:Box.FromCenter(picture.CenterX,picture.CenterY,cell.Width*.24f,cell.Width*.24f),item!=null?Visuals.Tint(item):ShopBodyDesign.InsetBorder.Line);
                ProfileGamePainter.Label(p,WardrobeShelfDesign.Caption(cell,hasAction),item==null?PlaytestText.T("wardrobe.default"):MetaCards.ItemName(item),WardrobeShelfDesign.ItemName);
                if(selected&&browse.PreviewItem==null)ProfileGamePainter.Check(p,WardrobeShelfDesign.Check(cell));
                Box action=WardrobeShelfDesign.Action(cell);
                // Register the cell before its purchase button: the nested button takes precedence.
                if(cell.Within(r.Area))p.Hit(cell,()=>{browse.TryOn(wardrobe,item);app.Sound.Play(SoundCue.Click);});
                if(card.State==WardrobeItemState.ForSale)
                    ProfileGamePainter.Price(p,action,NumberText.Group(item!.Price),WardrobeShelfDesign.PurchaseTouch(cell).Within(r.Area)?()=>Buy(app,browse.Family,browse.Kind,card):(Action?)null,touch:WardrobeShelfDesign.PurchaseTouch(cell));
                if((card.State==WardrobeItemState.Reward||card.State==WardrobeItemState.LevelLocked)&&action.Within(r.Safe))
                {
                    Skin(p,MetaScreenArt.Tag,action);
                    Kit.LockBadge(p,action.Left+action.Height*.4f,action.CenterY,action.Height*.8f);
                    string text=card.State==WardrobeItemState.LevelLocked?PlaytestText.F("wardrobe.unlock_level",item!.UnlockLevel):card.RewardCadence.HasValue?PlaytestText.F("wardrobe.milestone_every",card.RewardCadence.Value):PlaytestText.T("wardrobe.milestone_reward");
                    ProfileGamePainter.Label(p,new Box(action.Left+action.Height,action.Top,action.Right-action.Height*.12f,action.Bottom),text,WardrobeShelfDesign.Status);
                }
            }
            p.PopClip();
            p.VerticalScroll(r.Viewport,delta=>browse.Drag(delta,r.Maximum(items.Count)));
            if(r.Maximum(items.Count)>0){p.FillRound(r.Track,r.Track.Width*.5f,ShopBodyDesign.Card.Line);Box thumb=r.Thumb(items.Count,browse.Offset);p.FillRound(thumb,thumb.Width*.5f,ShopBodyDesign.Card.Face);}
            p.BlockInput(new Box(r.Families.Left,r.Families.Top,r.Categories.Right,r.Categories.Bottom));
            Kit.NavigationBar(p,r.Families,4);
            for(int i=0;i<4;i++)
            {
                Family family=WardrobeService.Families[i];
                Kit.NavigationTab(p,NavigationSkin.Segment(r.Families,i,4),PlaytestText.T("family."+WardrobeService.FamilyKey(family)),NavigationSkin.FamilyIcon(family),i,browse.Family==family,()=>browse.Select(family));
            }
            Kit.NavigationBar(p,r.Categories,4);
            for(int i=0;i<4;i++)
            {
                CosmeticKind kind=CosmeticCatalog.WornKinds[i];string key="wardrobe.kind_"+kind.ToString().ToLowerInvariant();
                Kit.NavigationTab(p,NavigationSkin.Segment(r.Categories,i,4),PlaytestText.T(key),NavigationSkin.Icon(key),i,browse.Kind==kind,()=>browse.Select(kind));
            }
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Wardrobe),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("wardrobe.title"),app.CloseWardrobe,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
            if(app.HomeToastText!=null)Kit.Toast(p,r.Hero,app.HomeToastText);
        }
        public static void Skin(IPainter p,string asset,Box box)
        {
            p.Mark("ui.wardrobe.illustrations");var border=MetaScreenArt.Borders(asset);
            if(border.X>0)Kit.SlicedSprite(p,asset,box,border.X,border.Y,border.Y);else p.Sprite(asset,box);
        }
        public static void Preview(IPainter p,Box well,Family family,Outfit outfit)
        {
            Visuals.Hero(p,HomeStage.Figure(well.CenterX,well.Bottom,well.Height),family,outfit);
        }
        private static Box FitItem(string asset,Box box,CosmeticItem? item)
        {
            HeroFrame? frame=item!=null?HeroGearArt.Icon(item.Id):null;
            if(!frame.HasValue&&asset.StartsWith(PainterBase.HeroMotionPrefix,StringComparison.Ordinal))
                foreach(var family in WardrobeService.Families)
                {
                    string? stock=WardrobeShelfDesign.ItemPicture(family,CosmeticKind.Hat,null);
                    if(stock!=asset)continue;
                    string id=family==Family.Sprig?SprigV2Art.LeafyId:family==Family.Bloom?BloomV2Art.PetalsId:family==Family.Drop?DropV2Art.NativeId:TwigV2Art.AcornId;
                    frame=HeroGearArt.Icon(id);break;
                }
            if(frame.HasValue)return CharacterArt.FitBox(box,frame.Value.Width,frame.Value.Height);
            return MetaScreenArt.Is(asset)?MetaScreenArt.FitHat(asset,box):box;
        }
        private static void Buy(DesignApp app,Family family,CosmeticKind kind,WardrobeShelfItem card)
        {
            var wardrobe=app.Meta.Wardrobe;CosmeticItem? item=card.Item;if(item==null||!wardrobe.IsBuyable(item))return;
            app.WardrobeBrowse.TryOn(wardrobe,item);
            void Refuse(){app.Sound.Play(SoundCue.Refused);app.HomeToast(PlaytestText.T("gameplay.not_enough_petals"));}
            if(app.Meta.Economy.Petals<item.Price){Refuse();return;}
            app.ConfirmPurchase(PurchaseOffer.ForPetals(MetaCards.ItemName(item),item.Price),
                (q,well)=>Preview(q,well,family,WardrobeShelfCatalog.Preview(wardrobe.OutfitOf(family),kind,item)),
                ()=>{if(wardrobe.TryBuy(item.Id)){app.WardrobeBrowse.ResetPreview();wardrobe.Equip(family,item.Id);}else Refuse();});
        }
        private static void Locked(IPainter p,DesignApp app,HomeLook look)
        {
            var r=ScreenLayout.LockedPage(p.Width,p.Height,p.Insets);
            Kit.CardFrame(p,r.Panel);Kit.LockedNotice(p,r.Notice,NavPlace.Wardrobe,app.UnlockLevel(NavPlace.Wardrobe));
            Kit.BottomNav(p,HomeScreen.Nav(p,NavPlace.Wardrobe),look,app.Navigate);
            Kit.PageHeader(p,r.Header,PlaytestText.T("wardrobe.title"),app.CloseWardrobe,app.ShownPetals,look.Store?app.OpenStore:(Action?)null);
        }
    }
}
