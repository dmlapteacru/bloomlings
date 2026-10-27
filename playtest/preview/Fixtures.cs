using System;
using System.Collections;
using Bloomlings.Client.Services.Economy.Online;
using Bloomlings.Client.Services.Clock;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;
using Bloomlings.Client.Services.Backend;
using Bloomlings.Client.Meta.Wardrobe;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloomlings.Client.Meta.Clearing;
using Bloomlings.Client.Meta.DailyReward;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Services.Config;
using Bloomlings.Client.Services.Save;
using Bloomlings.Client.UI.Design;
using Bloomlings.Content.Packs;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Simulation;
using Bloomlings.Core.Tray;
using Bloomlings.Core.Variants;
using Bloomlings.Playtest.Design;
using Bloomlings.Solver;
using C = Bloomlings.Client.UI.Design.DesignTokens.Colors;
using T = Bloomlings.Client.UI.Design.DesignTokens.Type;

namespace Bloomlings.Playtest.Preview
{
    /// <summary>One design board frame, rendered from a scripted state of the real playtest screens.</summary>
    public sealed class Fixture
    {
        public Fixture(int number, string slug, string title, Action<SkiaPainter, string> render)
        {
            Number = number;
            Slug = slug;
            Title = title;
            Render = render;
        }

        public int Number { get; }

        public string Slug { get; }

        public string Title { get; }

        /// <summary>Draws the frame into the painter, with a fresh save in the given folder.</summary>
        public Action<SkiaPainter, string> Render { get; }
    }

    /// <summary>
    /// The scripted states of frames 1–17. They use real levels from the playtest's content and the core's solver for
    /// winning and jamming move lists.
    /// </summary>
    public static class Fixtures
    {
        private const float Frame = 1f / 30f;

        public static IEnumerable<Fixture> All(ContentSet content, string root)
        {
            DesignApp App(string data, bool splash = false) => new DesignApp(data, content, new Silence(), splash);

            DesignApp StackApp(string data)
            {
                var app = App(data);
                app.Meta.SkipTo(5003); app.LoadLevel(5004); CloseDemo(app); app.Level!.SkipGuide();
                return app;
            }
            yield return new Fixture(96, "counted-stack-start", "Visible same-variant stacks before clearing", (p, data) =>
            {
                var app = StackApp(data); Run(app, p, .1f);
                Expect(p.Slots.Contains("tile.stack_count"), "public stack counters are drawn");
            });
            yield return new Fixture(97, "counted-stack-five", "The counter reaches five at the actual worker arrival", (p, data) =>
            {
                var app = StackApp(data);
                var level = app.Level!;
                var cell = level.Session.Definition.Overlays.First(o => o.StackCount == 9).Cell;
                SolveResult win = new Solver.Solver().Solve(Session(content, 5004), new SolveOptions(200000));
                Expect(win.Status == SolveStatus.Solvable, "the counted-stack fixture is solvable");
                bool found = false;
                foreach (Command command in win.Trace)
                {
                    Expect(command is TapPod, "no booster for the stack fixture");
                    level.Tap(((TapPod)command).PodId);
                    for (int frame = 0; frame < 20000; frame++)
                    {
                        level.Advance(.02f);
                        if (level.Animator.Cell(cell).StackRemaining == 5) { found = true; break; }
                        if (level.Animator.Idle) break;
                    }
                    if (found) break;
                }
                Expect(found, "the animation displays 9 to 5 instead of reading the final rules count");
                Run(app, p, 0f);
            });
            yield return new Fixture(98, "large-gate-36x36", "A readable 3x3 gate on the dense board", (p, data) =>
            {
                var app = App(data); app.Meta.SkipTo(5004); app.LoadLevel(5005); CloseDemo(app); app.Level!.SkipGuide();
                Run(app, p, .1f);
                Expect(app.Level.Session.View.Specials.Single().Cells.Count == 9, "dense-board gate occupies 3x3 cells");
            });
            yield return new Fixture(99, "large-fountain-36x36", "A large fountain shows its exact target variant", (p, data) =>
            {
                var app = App(data); app.Meta.SkipTo(5005); app.LoadLevel(5006); CloseDemo(app); app.Level!.SkipGuide();
                Run(app, p, .1f);
                SpecialInfo fountain = app.Level.Session.View.Specials.Single();
                Expect(fountain.Cells.Count == 9 && fountain.Condition.Variant.HasValue, "large fountain has a visible exact-variant condition");
                Expect(p.Slots.Contains("special.fountain"), "the fountain glyph is drawn");
            });

            yield return new Fixture(90, "garden-backgrounds", "Distinct garden scenes remain available through the bundled asset loader", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app);
                void Scene(string picture, Action open)
                {
                    open(); CloseAll(app); p.Slots.Clear(); Run(app, p, .1f);
                    Expect(p.HasSprite(PainterBase.BackgroundPrefix + picture) && p.Slots.Contains(OwnerPictures.SlotOf(picture)),
                        "the page draws its bundled garden: " + picture);
                }
                Scene(OwnerPictures.Home, app.GoHome);
                Expect(p.Slots.Contains("bg.home.fountain_back") && p.Slots.Contains("bg.home.lotus"), "Home retains its separate fountain layers");
                Scene(OwnerPictures.Wardrobe, app.OpenWardrobe);
                Scene(OwnerPictures.Leaderboard, app.OpenLeaderboard);
                Scene(OwnerPictures.Collection, app.OpenCollection);
                Scene(OwnerPictures.Profile, app.OpenProfile);
                app.OpenProfileEdit(ProfileTab.Avatar); p.Slots.Clear(); Run(app, p, .1f);
                Expect(p.Slots.Contains("bg.profile"), "the editor shares the profile's pond terrace");
                foreach (string picture in new[] { "gameplay-daylight", "gameplay-pond", "gameplay-orchard", "gameplay-moonlit", "gameplay-fern", "gameplay-willow", "gameplay-pear", "gameplay-citrus", "gameplay-plum", OwnerPictures.Win })
                    Expect(p.HasSprite(PainterBase.BackgroundPrefix + picture), "theme and victory assets are bundled: " + picture);

                // The new garden cover must reveal the incoming screen, rather than
                // painting the full background back into the iris opening.
                p.BeginFrame();
                p.FillRect(new Box(0, 0, p.Width, p.Height), new Rgba(210, 17, 35));
                LotusPainter.Draw(p, LotusIris.Splash(2.5f, 1f, 2f), PlaytestText.T("splash.loading"), true);
                using (var pixels = SkiaSharp.SKBitmap.Decode(p.Png()))
                {
                    var revealed = pixels.GetPixel((int)(p.Width * .95f), (int)(p.Height * .5f));
                    Expect(revealed.Red > 180 && revealed.Green < 50 && revealed.Blue < 70, "the opening iris preserves the screen underneath its garden cover");
                }
                app.GoHome(); CloseAll(app); Run(app, p, .4f);
            });

            foreach(var target in new[]{(108,"sprig-wardrobe-preview",Family.Sprig),(112,"twig-wardrobe-preview",Family.Twig)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"Fitted equipment is available in Wardrobe",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(scene.Item3);
                    p.DrawnSprites.Clear();Run(app,p,.15f);
                    var frame=HeroGearArt.Layer(scene.Item3,app.Meta.Wardrobe.OutfitOf(scene.Item3),0)!.Value;
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Name),"Wardrobe draws new stock family base");
                });
            }
            foreach(var target in new[]{(109,"simple-heroes-home",false),(113,"simple-heroes-saved-outfit",true),(114,"simple-heroes-reloaded",true),(117,"simple-heroes-fresh-home",false)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"All four new models draw on Home, including an existing equipment save",(p,data)=>
                {
                    var app=App(data);if(scene.Item1!=117)app=Progressed(app,content,87);CloseAll(app);
                    if(scene.Item3)
                    {
                        Expect(app.Meta.Wardrobe.Equip(Family.Twig,"hat.twig_flower_cap"),"legacy Twig selection is retained");
                        foreach(string id in new[]{SprigV2Art.CapId,SprigV2Art.GlassesId,"backpack.sprig_leaf"})Expect(app.Meta.Wardrobe.Equip(Family.Sprig,id),"legacy Sprig selections are retained");
                        if(scene.Item1==114){app=App(data);CloseAll(app);}
                    }
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.5f);
                    foreach(Family family in CharacterArt.Families)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+HomeHeroArt.Layer(family,app.Meta.Wardrobe.OutfitOf(family),0)!.Value.Name),"Home draws new "+family);
                    Expect(!p.DrawnSprites.Any(n=>n.StartsWith(PainterBase.HeroMotionPrefix+"sprigmod-",StringComparison.Ordinal)||n.StartsWith(PainterBase.HeroMotionPrefix+"twigmod-",StringComparison.Ordinal)),"previous fitted meshes cannot override the new models");
                    if(scene.Item3)Expect(App(data).Meta.Wardrobe.OutfitOf(Family.Twig).Hat?.Id==TwigV2Art.BucketId,"displaying the new base never erases saved gear");
                    Expect(!p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+"twigv2-base-none"),"Home never draws the modeling-only bald base");
                });
            }
            foreach(var target in new[]{(110,"simple-sprig-wardrobe",Family.Sprig),(115,"simple-twig-wardrobe",Family.Twig),(118,"simple-bloom-wardrobe",Family.Bloom),(119,"simple-drop-wardrobe",Family.Drop)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"The Wardrobe previews the owner's new "+scene.Item3,(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);app.WardrobeBrowse.Select(scene.Item3);app.OpenWardrobe();
                    p.DrawnSprites.Clear();Run(app,p,.5f);
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+HeroGearArt.Layer(scene.Item3,app.Meta.Wardrobe.OutfitOf(scene.Item3),0)!.Value.Name),"Wardrobe draws the new base");
                    Expect(!WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,scene.Item3).Any(c=>c.Item?.MetaOnly==true&&!HeroGearArt.CanPreview(c.Item)),"unfitted previous 3D attachments are withheld from the shelf");
                    if(scene.Item3==Family.Twig)Expect(!p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+"twigv2-base-none"),"the wardrobe Default preview restores the stock hat");
                });
            }
            foreach(var target in new[]{(111,"simple-sprig-win",12,Family.Sprig),(116,"simple-twig-win",13,Family.Twig)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"A real solved level uses the new victory hero",(p,data)=>
                {
                    var app=App(data);app.Meta.SkipTo(scene.Item3-1);app.LoadLevel(scene.Item3);CloseDemo(app);Run(app,p,.1f);
                    SolveResult win=new Solver.Solver().Solve(Session(content,scene.Item3),SolveOptions.Default);
                    Expect(win.Status==SolveStatus.Solvable,"the actual level is solvable");
                    p.DrawnSprites.Clear();Play(app,p,win.Trace,win.Trace.Count);Settle(app,p);Run(app,p,1.1f);
                    Expect(app.Level!.Won&&app.Meta.CurrentLevel==scene.Item3+1,"victory preserves progression");
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+HeroGearArt.Layer(scene.Item4,app.Meta.Wardrobe.OutfitOf(scene.Item4),0)!.Value.Name),"victory draws the new "+scene.Item4);
                });
            }

            yield return new Fixture(120,"twig-modular-wardrobe-trial","Wardrobe fits and persists every delivered Twig item",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Twig);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                long petals=app.Meta.Economy.Petals;
                void Choose(string id)
                {
                    app.Meta.Wardrobe.Catalog.TryGet(id,out var item);app.WardrobeBrowse.Select(item!.Kind);
                    var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Kind,Family.Twig).ToList();
                    int index=shelf.FindIndex(c=>c.Item?.Id==id);Expect(index>0,"item is present in its Wardrobe category");
                    var card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(shelf.Count));Run(app,p,.1f);
                    Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.EquippedFor(Family.Twig,item.Kind)?.Id==id,"actual shelf card equips "+id);
                    Expect(App(data).Meta.Wardrobe.EquippedFor(Family.Twig,item.Kind)?.Id==id,"restart retains "+id);
                }
                foreach(string id in TwigV2Art.ItemIds)Choose(id);
                foreach(var kind in CosmeticCatalog.WornKinds)
                {
                    app.WardrobeBrowse.Select(kind);app.WardrobeBrowse.SetOffset(0,0);Run(app,p,.1f);
                    Outfit before=app.Meta.Wardrobe.OutfitOf(Family.Twig);Tap(p,r.Cell(0));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Twig).Get(kind)==null,"Default clears only "+kind);
                    foreach(var other in CosmeticCatalog.WornKinds)if(other!=kind)Expect(app.Meta.Wardrobe.OutfitOf(Family.Twig).Get(other)?.Id==before.Get(other)?.Id,"other slots survive Default");
                }
                Choose(TwigV2Art.BeretId);Choose(TwigV2Art.GlassesId);Choose(TwigV2Art.ScarfId);Choose(TwigV2Art.CoatId);
                Expect(app.Meta.Economy.Petals==petals&&app.Overlays.Count==0,"free trial spends nothing");
            });
            foreach(var target in new[]{(121,"twig-modular-home-acorn",TwigV2Art.AcornId),(122,"twig-modular-home-panama",TwigV2Art.BucketId),(123,"twig-modular-wardrobe-panama",TwigV2Art.BucketId)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"The same Twig base wears the selected fitted headwear",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);Expect(app.Meta.Wardrobe.Equip(Family.Twig,scene.Item3),"fitted hat equips");
                    if(scene.Item1==123){app.WardrobeBrowse.Select(Family.Twig);app.OpenWardrobe();}else app.GoHome();
                    p.DrawnSprites.Clear();Run(app,p,.5f);
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+(scene.Item1==123?HeroGearArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),0):HomeHeroArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),0))!.Value.Name),"the actual host draws the selected new Twig state");
                    foreach(Family family in CharacterArt.Families)if(scene.Item1!=123&&family!=Family.Twig)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+HomeHeroArt.Layer(family,app.Meta.Wardrobe.OutfitOf(family),0)!.Value.Name),"other simplified heroes remain");
                });
            }
            yield return new Fixture(124,"twig-modular-win-panama","The Panama remains on Twig after a solved level",(p,data)=>
            {
                var app=App(data);app.Meta.SkipTo(12);Expect(app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.BucketId),"Panama equips before L40");
                Expect(app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.GlassesId)&&app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.ScarfId),"glasses and scarf equip independently");
                Expect(app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.CoatId),"autumn coat equips on victory celebrant");
                app.LoadLevel(13);CloseDemo(app);Run(app,p,.1f);
                SolveResult win=new Solver.Solver().Solve(Session(content,13),SolveOptions.Default);Expect(win.Status==SolveStatus.Solvable,"actual L13 is solvable");
                p.DrawnSprites.Clear();Play(app,p,win.Trace,win.Trace.Count);Settle(app,p);Run(app,p,1.1f);
                Expect(app.Level!.Won&&app.Meta.CurrentLevel==14,"progression completes normally");
                Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+"twigv2-base-buckethat"),"victory keeps the selected Panama");
                for(int layer=1;layer<HeroGearArt.LayerCount;layer++)if(HeroGearArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),layer) is HeroFrame worn)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+worn.Name),"victory keeps the saved accessory layer");
            });

            yield return new Fixture(125,"twig-v2-wardrobe-trial","Wardrobe fits and persists every delivered Twig item",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Twig);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                long petals=app.Meta.Economy.Petals;
                void Choose(string id)
                {
                    app.Meta.Wardrobe.Catalog.TryGet(id,out var item);app.WardrobeBrowse.Select(item!.Kind);
                    var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Kind,Family.Twig).ToList();
                    int index=shelf.FindIndex(c=>c.Item?.Id==id);Expect(index>0,"item is present in its Wardrobe category");
                    var card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(shelf.Count));Run(app,p,.1f);
                    Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.EquippedFor(Family.Twig,item.Kind)?.Id==id,"actual shelf card equips "+id);
                    Expect(App(data).Meta.Wardrobe.EquippedFor(Family.Twig,item.Kind)?.Id==id,"restart retains "+id);
                }
                foreach(string id in TwigV2Art.ItemIds)Choose(id);
                foreach(var kind in CosmeticCatalog.WornKinds)
                {
                    app.WardrobeBrowse.Select(kind);app.WardrobeBrowse.SetOffset(0,0);Run(app,p,.1f);
                    Outfit before=app.Meta.Wardrobe.OutfitOf(Family.Twig);Tap(p,r.Cell(0));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Twig).Get(kind)==null,"Default clears only "+kind);
                    foreach(var other in CosmeticCatalog.WornKinds)if(other!=kind)Expect(app.Meta.Wardrobe.OutfitOf(Family.Twig).Get(other)?.Id==before.Get(other)?.Id,"other slots survive Default");
                }
                Choose(TwigV2Art.BeretId);Choose(TwigV2Art.GlassesId);Choose(TwigV2Art.ScarfId);Choose(TwigV2Art.CoatId);
                Expect(app.Meta.Economy.Petals==petals&&app.Overlays.Count==0,"free trial spends nothing");
            });
            foreach(var target in new[]{(126,"twig-v2-home-beret",TwigV2Art.BeretId,false),(127,"twig-v2-home-winter",TwigV2Art.EarmuffsId,true),(128,"twig-v2-wardrobe-holiday",TwigV2Art.XmasId,false)})
            {
                var scene=target;
                yield return new Fixture(scene.Item1,scene.Item2,"V2 headwear and scarf share the saved appearance across hosts",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    Expect(app.Meta.Wardrobe.Equip(Family.Twig,scene.Item3)&&app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.ScarfId),"V2 outfit equips");
                    if(scene.Item4)Expect(app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.GlassesId),"glasses combine with headwear");
                    app=App(data);CloseAll(app);
                    if(scene.Item1==128){app.WardrobeBrowse.Select(Family.Twig);app.OpenWardrobe();}else app.GoHome();
                    p.DrawnSprites.Clear();Run(app,p,.4f);
                    for(int layer=0;layer<HeroGearArt.LayerCount;layer++)if((scene.Item1==128?HeroGearArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),layer):HomeHeroArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),layer)) is HeroFrame frame)
                        Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Name),"host draws saved "+frame.Name);
                });
            }

            foreach(var selection in new[]{(129,"twig-v2-winter-jacket",TwigV2Art.JacketId,TwigV2Art.EarmuffsId),(130,"twig-v2-autumn-coat",TwigV2Art.CoatId,TwigV2Art.BeretId),(131,"twig-v2-garden-overalls",TwigV2Art.OverallsId,TwigV2Art.BucketId),(132,"twig-v2-winter-jacket-fit",TwigV2Art.JacketId,TwigV2Art.EarmuffsId),(133,"twig-v2-autumn-coat-fit",TwigV2Art.CoatId,TwigV2Art.BeretId)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"Garment, headwear, glasses and scarf persist independently across hosts",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    foreach(string id in new[]{scene.Item3,scene.Item4,TwigV2Art.GlassesId})Expect(app.Meta.Wardrobe.Equip(Family.Twig,id),"outfit equips "+id);
                    if(scene.Item1<131)Expect(app.Meta.Wardrobe.Equip(Family.Twig,TwigV2Art.ScarfId),"scarf combines with garment");
                    app=App(data);CloseAll(app);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Twig).Clothes?.Id==scene.Item3,"clothes survive restart");
                    if(scene.Item1>=131){app.WardrobeBrowse.Select(Family.Twig);app.OpenWardrobe();}else app.GoHome();
                    p.DrawnSprites.Clear();Run(app,p,.4f);
                    for(int layer=0;layer<HeroGearArt.LayerCount;layer++){
                        HeroFrame? frame=scene.Item1>=131?HeroGearArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),layer):HomeHeroArt.Layer(Family.Twig,app.Meta.Wardrobe.OutfitOf(Family.Twig),layer);
                        if(frame.HasValue)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Value.Name),"host draws each saved layer");
                    }
                });
            }

            foreach(var selection in new[]{(135,"sprig-v2-home-stock",SprigV2Art.LeafyId,""),(136,"sprig-v2-home-winter",SprigV2Art.EarmuffsId,SprigV2Art.JacketId),(137,"sprig-v2-home-summer",SprigV2Art.BucketId,SprigV2Art.OverallsId)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"New Sprig and aligned wearables appear on Home",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    Expect(app.Meta.Wardrobe.Equip(Family.Sprig,scene.Item3),"new head equips");
                    if(scene.Item4.Length>0){app.Meta.Wardrobe.Equip(Family.Sprig,scene.Item4);app.Meta.Wardrobe.Equip(Family.Sprig,SprigV2Art.GlassesId);}
                    if(scene.Item1==136)app.Meta.Wardrobe.Equip(Family.Sprig,SprigV2Art.ScarfId);
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    var outfit=app.Meta.Wardrobe.OutfitOf(Family.Sprig);
                    for(int i=0;i<HeroGearArt.LayerCount;i++)if(HomeHeroArt.Layer(Family.Sprig,outfit,i) is HeroFrame layer)
                        Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"Home draws saved Sprig layer "+i);
                    Expect(!p.DrawnSprites.Any(n=>n.Contains("sprig-idle-",StringComparison.Ordinal)||n.Contains("sprigmod-",StringComparison.Ordinal)),"retired Sprig cannot replace V2");
                });
            }
            foreach(var selection in new[]{(138,"sprig-v2-wardrobe-head",CosmeticKind.Hat),(139,"sprig-v2-wardrobe-face",CosmeticKind.Glasses),(140,"sprig-v2-wardrobe-body",CosmeticKind.Clothes),(141,"sprig-v2-wardrobe-accessories",CosmeticKind.Scarf)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"Wardrobe categories isolate equip and Default actions",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    foreach(string id in new[]{SprigV2Art.SwirlId,SprigV2Art.CoatId,SprigV2Art.GlassesId,SprigV2Art.ScarfId})app.Meta.Wardrobe.Equip(Family.Sprig,id);
                    app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Sprig);app.WardrobeBrowse.Select(scene.Item3);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                    Outfit before=app.Meta.Wardrobe.OutfitOf(Family.Sprig);
                    Tap(p,r.Cell(0));Run(app,p,.1f);
                    var reset=App(data).Meta.Wardrobe.OutfitOf(Family.Sprig);
                    Expect(reset.Get(scene.Item3)==null,"Default persists only current slot");
                    foreach(var other in CosmeticCatalog.WornKinds)if(other!=scene.Item3)Expect(reset.Get(other)?.Id==before.Get(other)?.Id,"Default preserves "+other);
                    foreach(string label in new[]{"Head","Face","Body","Accessories"})Expect(Shows(p,label),"category label "+label);
                    Expect(!Shows(p,"Skins")&&!Shows(p,"Trails")&&!Shows(p,"Faces"),"unused categories are hidden");
                    Tap(p,r.Cell(1));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Sprig).Get(scene.Item3)!=null,"category item equips through real tap");
                });
            }
            foreach(var selection in new[]{(146,"bloom-v2-home-stock",BloomV2Art.PetalsId,""),(147,"bloom-v2-home-winter",BloomV2Art.EarmuffsId,BloomV2Art.JacketId),(148,"bloom-v2-home-summer",BloomV2Art.BucketId,BloomV2Art.OverallsId)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"New Bloom and aligned wearables appear on Home",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    Expect(app.Meta.Wardrobe.Equip(Family.Bloom,scene.Item3),"new head equips");
                    if(scene.Item4.Length>0){app.Meta.Wardrobe.Equip(Family.Bloom,scene.Item4);app.Meta.Wardrobe.Equip(Family.Bloom,BloomV2Art.GlassesId);}
                    if(scene.Item1==147)app.Meta.Wardrobe.Equip(Family.Bloom,BloomV2Art.KnitId);
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    var outfit=app.Meta.Wardrobe.OutfitOf(Family.Bloom);
                    for(int i=0;i<HeroGearArt.LayerCount;i++)if(HomeHeroArt.Layer(Family.Bloom,outfit,i) is HeroFrame layer)
                        Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"Home draws saved Bloom layer "+i);
                    Expect(!p.DrawnSprites.Any(n=>n.Contains("bloom-idle-",StringComparison.Ordinal)||n.Contains("bloommod-",StringComparison.Ordinal)),"retired Bloom cannot replace V2");
                });
            }
            foreach(var selection in new[]{(149,"bloom-v2-wardrobe-head",CosmeticKind.Hat),(150,"bloom-v2-wardrobe-face",CosmeticKind.Glasses),(151,"bloom-v2-wardrobe-body",CosmeticKind.Clothes),(152,"bloom-v2-wardrobe-accessories",CosmeticKind.Scarf)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"Wardrobe categories isolate equip and Default actions",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    foreach(string id in new[]{BloomV2Art.RoseId,BloomV2Art.CoatId,BloomV2Art.GlassesId,BloomV2Art.KnitId})app.Meta.Wardrobe.Equip(Family.Bloom,id);
                    app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Bloom);app.WardrobeBrowse.Select(scene.Item3);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                    Outfit before=app.Meta.Wardrobe.OutfitOf(Family.Bloom);
                    Tap(p,r.Cell(0));Run(app,p,.1f);
                    var reset=App(data).Meta.Wardrobe.OutfitOf(Family.Bloom);
                    Expect(reset.Get(scene.Item3)==null,"Default persists only current slot");
                    foreach(var other in CosmeticCatalog.WornKinds)if(other!=scene.Item3)Expect(reset.Get(other)?.Id==before.Get(other)?.Id,"Default preserves "+other);
                    foreach(string label in new[]{"Head","Face","Body","Accessories"})Expect(Shows(p,label),"category label "+label);
                    Expect(!Shows(p,"Skins")&&!Shows(p,"Trails")&&!Shows(p,"Faces"),"unused categories are hidden");
                    Tap(p,r.Cell(1));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Bloom).Get(scene.Item3)!=null,"category item equips through real tap");
                });
            }
            foreach(var selection in new[]{(144,"sprig-stable-wardrobe-shelves",Family.Sprig),(145,"twig-stable-wardrobe-shelves",Family.Twig),(153,"bloom-stable-wardrobe-shelves",Family.Bloom),(162,"drop-stable-wardrobe-shelves",Family.Drop)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"Only the fitting stand changes while other shelf previews stay identical",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(scene.Item3);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                    string[] ids=scene.Item3==Family.Sprig
                        ?new[]{SprigV2Art.BucketId,SprigV2Art.GlassesId,SprigV2Art.JacketId,SprigV2Art.ScarfId}
                        :scene.Item3==Family.Bloom?new[]{BloomV2Art.BucketId,BloomV2Art.GlassesId,BloomV2Art.JacketId,BloomV2Art.StripeId}
                        :scene.Item3==Family.Drop?new[]{DropV2Art.BucketId,DropV2Art.GlassesId,DropV2Art.JacketId,DropV2Art.StripeId}
                        :new[]{TwigV2Art.BucketId,TwigV2Art.GlassesId,TwigV2Art.JacketId,TwigV2Art.ScarfId};
                    void Category(CosmeticKind kind)
                    {
                        int index=CosmeticCatalog.WornKinds.ToList().IndexOf(kind);
                        Tap(p,NavigationSkin.Segment(r.Categories,index,4));Run(app,p,.1f);
                    }
                    foreach(var kind in new[]{CosmeticKind.Hat,CosmeticKind.Glasses,CosmeticKind.Scarf,CosmeticKind.Clothes})
                    {
                        app.Meta.Wardrobe.UnequipAll(scene.Item3);Category(kind);app.WardrobeBrowse.SetOffset(0,0);Run(app,p,.1f);
                        using var before=SkiaSharp.SKBitmap.Decode(p.Png());
                        int count=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,kind,scene.Item3).Count;
                        foreach(string id in ids)
                        {
                            app.Meta.Wardrobe.Catalog.TryGet(id,out var item);if(item!.Kind==kind)continue;
                            Category(item.Kind);
                            var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Kind,scene.Item3).ToList();
                            int index=shelf.FindIndex(c=>c.Item?.Id==id);Expect(index>0,"fitted item exists");
                            Box card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(shelf.Count));Run(app,p,.1f);
                            Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                            Expect(app.Meta.Wardrobe.EquippedFor(scene.Item3,item.Kind)?.Id==id,"real shelf tap equips item");
                        }
                        Category(kind);app.WardrobeBrowse.SetOffset(0,0);Run(app,p,.1f);
                        using var after=SkiaSharp.SKBitmap.Decode(p.Png());
                        for(int i=0;i<Math.Min(3,count);i++)
                        {
                            Box picture=WardrobeShelfDesign.Picture(r.Cell(i)).Inset(2);
                            for(int y=(int)picture.Top;y<(int)picture.Bottom;y+=2)
                            for(int x=(int)picture.Left;x<(int)picture.Right;x+=2)
                                Expect(before.GetPixel(x,y)==after.GetPixel(x,y),"shelf preview stays unchanged after equipping other categories: "+kind+" item "+i);
                        }
                        bool changed=false;
                        for(int y=(int)r.Hero.Top;y<(int)r.Hero.Bottom&&!changed;y+=2)
                        for(int x=(int)r.Hero.Left;x<(int)r.Hero.Right;x+=2)if(before.GetPixel(x,y)!=after.GetPixel(x,y)){changed=true;break;}
                        Expect(changed,"main fitting hero shows the combined outfit");
                    }
                });
            }
            yield return new Fixture(143,"sprig-v2-victory-outfit","New Sprig retains its complete outfit on victory",(p,data)=>
            {
                var app=App(data);app.Meta.SkipTo(11);
                foreach(string id in new[]{SprigV2Art.SwirlId,SprigV2Art.OverallsId,SprigV2Art.GlassesId,SprigV2Art.ScarfId})
                    Expect(app.Meta.Wardrobe.Equip(Family.Sprig,id),"Sprig outfit equips before victory");
                app.LoadLevel(12);CloseDemo(app);Run(app,p,.1f);
                SolveResult win=new Solver.Solver().Solve(Session(content,12),SolveOptions.Default);
                Expect(win.Status==SolveStatus.Solvable,"actual L12 is solvable");
                Play(app,p,win.Trace,win.Trace.Count);Settle(app,p);p.DrawnSprites.Clear();Run(app,p,1.1f);
                Expect(app.Level!.Won&&app.Meta.CurrentLevel==13,"victory preserves progression");
                for(int i=0;i<HeroGearArt.LayerCount;i++)if(HeroGearArt.Layer(Family.Sprig,app.Meta.Wardrobe.OutfitOf(Family.Sprig),i) is HeroFrame layer)
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"victory draws saved Sprig layer "+i);
            });
            yield return new Fixture(154,"bloom-v2-home-striped","Bloom wears the alternate scarf with her complete outfit on Home",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);
                foreach(string id in new[]{BloomV2Art.RoseId,BloomV2Art.OverallsId,BloomV2Art.GlassesId,BloomV2Art.StripeId})
                    Expect(app.Meta.Wardrobe.Equip(Family.Bloom,id),"Bloom outfit equips");
                app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                for(int i=0;i<HeroGearArt.LayerCount;i++)if(HomeHeroArt.Layer(Family.Bloom,app.Meta.Wardrobe.OutfitOf(Family.Bloom),i) is HeroFrame layer)
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"Home draws saved Bloom layer "+i);
            });
            foreach(var selection in new[]{(155,"four-v2-home-stock",0),(156,"four-v2-home-winter",1),(157,"four-v2-home-summer",2)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"All four current heroes and family-colored round glasses share one Home",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    if(scene.Item3>0)
                    {
                        bool winter=scene.Item3==1;
                        var looks=new[]{
                            (Family.Sprig,new[]{winter?SprigV2Art.EarmuffsId:SprigV2Art.BucketId,SprigV2Art.GlassesId,winter?SprigV2Art.JacketId:SprigV2Art.OverallsId}),
                            (Family.Bloom,new[]{winter?BloomV2Art.EarmuffsId:BloomV2Art.BucketId,BloomV2Art.GlassesId,winter?BloomV2Art.JacketId:BloomV2Art.OverallsId}),
                            (Family.Drop,new[]{winter?DropV2Art.EarmuffsId:DropV2Art.BucketId,DropV2Art.GlassesId,winter?DropV2Art.JacketId:DropV2Art.OverallsId}),
                            (Family.Twig,new[]{winter?TwigV2Art.EarmuffsId:TwigV2Art.BucketId,TwigV2Art.GlassesId,winter?TwigV2Art.JacketId:TwigV2Art.OverallsId})};
                        foreach(var look in looks)foreach(string id in look.Item2)Expect(app.Meta.Wardrobe.Equip(look.Item1,id),"family gear equips "+id);
                        if(winter)foreach(var scarf in new[]{(Family.Sprig,SprigV2Art.ScarfId),(Family.Bloom,BloomV2Art.KnitId),(Family.Drop,DropV2Art.KnitId),(Family.Twig,TwigV2Art.ScarfId)})
                            Expect(app.Meta.Wardrobe.Equip(scarf.Item1,scarf.Item2),"scarf fits over winter clothing");
                        app=App(data);CloseAll(app);
                    }
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    foreach(Family family in CharacterArt.Families)
                    for(int i=0;i<HeroGearArt.LayerCount;i++)if(HomeHeroArt.Layer(family,app.Meta.Wardrobe.OutfitOf(family),i) is HeroFrame layer)
                        Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"Home draws current saved layer "+layer.Name);
                    foreach(Family family in CharacterArt.Families)
                    for(int i=0;i<HeroGearArt.LayerCount;i++)if(HeroGearArt.Layer(family,app.Meta.Wardrobe.OutfitOf(family),i) is HeroFrame front)
                        Expect(!p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+front.Name),"Home never substitutes a frontal outfit layer");
                    Expect(!p.DrawnSprites.Any(n=>n.Contains("drop-idle-",StringComparison.Ordinal)||n.Contains("drop-react-",StringComparison.Ordinal)||n.Contains("simplehero-drop",StringComparison.Ordinal)),"legacy Drop never replaces the fitted model");
                });
            }
            foreach(var selection in new[]{(158,"drop-v2-wardrobe-head",CosmeticKind.Hat),(159,"drop-v2-wardrobe-face",CosmeticKind.Glasses),(160,"drop-v2-wardrobe-body",CosmeticKind.Clothes),(161,"drop-v2-wardrobe-accessories",CosmeticKind.Scarf)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,scene.Item2,"Drop has separate fitted Head, Face, Body and Accessories",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);
                    foreach(string id in new[]{DropV2Art.WhirlpoolId,DropV2Art.CoatId,DropV2Art.GlassesId,DropV2Art.StripeId})Expect(app.Meta.Wardrobe.Equip(Family.Drop,id),"Drop item equips");
                    app=App(data);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Drop);app.WardrobeBrowse.Select(scene.Item3);p.DrawnSprites.Clear();Run(app,p,.2f);
                    for(int i=0;i<HeroGearArt.LayerCount;i++)if(HeroGearArt.Layer(Family.Drop,app.Meta.Wardrobe.OutfitOf(Family.Drop),i) is HeroFrame layer)
                        Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+layer.Name),"fitting stand draws saved Drop layer "+i);
                    foreach(string label in new[]{"Head","Face","Body","Accessories"})Expect(Shows(p,label),"category label "+label);
                    Expect(!p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+"dropv2-base-none"),"bald scaffold is never a game look");
                });
            }
            yield return new Fixture(142,"shop-without-cosmetics","Shop contains Shop and Animations only",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);app.OpenStore();Run(app,p,.1f);
                Expect(!Shows(p,PlaytestText.T("store.tab_cosmetics")),"Cosmetics tab is absent");
                Box tabs=ScreenLayout.ReferenceStore(p.Width,p.Height,p.Insets).Tabs;
                Tap(p,NavigationSkin.Segment(tabs,1,2));Run(app,p,.2f);
                Expect(app.StoreTab==1&&app.StoreMoving,"second tab opens animation previews");
                Expect(!Shows(p,PlaytestText.T("store.tab_cosmetics")),"Cosmetics stays absent");
            });

            yield return new Fixture(100, "hero-medals", "The four completed milestone heroes become wearable profile medals", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app);
                foreach (var hero in Core.Progression.HeroMilestones.All)
                    app.Meta.Save.Collection.Add(new CollectionEntry(hero.PictureId, 1, "preview", hero.Level));
                app.OpenProfile(); Run(app, p, .1f);
                var r = ProfileGameDesign.Page(p.Width, p.Height, p.Insets);
                Tap(p, r.Achievements[1]); Run(app, p, .1f);
                Expect(app.Meta.Wardrobe.Profile.Badge?.Id == "badge.achievement.hero_sprig", "the completed hero medal can be worn");
            });

            yield return new Fixture(107,"canonical-first-level","A fresh profile plays the actual small tutorial, never catalog L11",(p,data)=>
            {
                Expect(Enumerable.Range(1,2500).All(n=>content.LevelNumbers.Contains(n)),"every launch number, including tutorials, is bundled");
                for(int n=1;n<=10;n++)
                {
                    var canonical=Content.Json.DefinitionJson.Read(File.ReadAllText(Path.Combine(root,$"content/curated/level-{n:0000}.json")));
                    Expect(Content.Json.DefinitionJson.Write(content.GetLevel(n))==Content.Json.DefinitionJson.Write(canonical),"L"+n+" is its canonical tutorial");
                }
                var app=App(data);
                Expect(app.Meta.CurrentLevel==1&&app.Resolve(1)==1,"fresh progress resolves to L1");
                app.LoadLevel(1);
                Expect(app.Level!.Session.Definition.LevelNumber==1,"displayed and actual level numbers match");
                Expect(app.Level.Session.Picture.Width==11&&app.Level.Session.Picture.Height==12,"the real first board is 11x12");
                Expect(app.Level.Session.Definition.Pods.All(pod=>pod.Variant==VariantId.Leaf||pod.Variant==VariantId.Water),"the first board only uses leaf and water");
                Expect(Core.Progression.LevelMechanics.UnlocksUsed(app.Level.Session.Definition,app.Level.Session.Picture).Count==0,"L1 has no special mechanics");
                var one=content.GetLevel(1);var eleven=content.GetLevel(11);
                var sparse=new ContentSet(0,20000,new[]{one,eleven},new[]{content.GetPicture(one.Picture),content.GetPicture(eleven.Picture)});
                var broken=new DesignApp(Path.Combine(data,"missing-level"),sparse,new Silence());
                bool rejected=false;try{broken.Resolve(2);}catch(ContentIntegrityException){rejected=true;}
                Expect(rejected,"a missing in-range level is rejected instead of silently substituted");
                app.Level.SkipGuide();CloseAll(app);Run(app,p,.4f);
            });

            foreach (var garden in new[] { (Frame:101, Level:250, Name:"fern"), (Frame:102, Level:300, Name:"willow"), (Frame:103, Level:350, Name:"pear"), (Frame:104, Level:400, Name:"citrus"), (Frame:105, Level:450, Name:"plum") })
                yield return new Fixture(garden.Frame, "gameplay-"+garden.Name, "The quiet garden behind the real gameplay board", (p, data) =>
                {
                    var app = Progressed(App(data), content, garden.Level-1);
                    app.LoadLevel(garden.Level); app.Level!.SkipGuide(); app.Level.CloseDemo(); CloseAll(app);
                    Run(app, p, .4f);
                    Expect(p.Slots.Contains("bg.theme."+garden.Name), "the level uses its new garden");
                });

            yield return new Fixture(86, "gameplay-purchase-sizes", "Empty boosters show only their icons and refill plus", (p, data) =>
            {
                var app = Progressed(App(data), content, 87);
                app.Meta.Economy.Grant(5000, null);
                foreach (var booster in LevelScreen.Boosters)
                    while (app.Meta.Save.Boosters.TryUse(booster.Kind)) { }
                app.LoadLevel(88); app.Level!.SkipGuide(); app.Level.CloseDemo(); CloseAll(app);
                Run(app, p, .4f);
                foreach (var booster in LevelScreen.Boosters)
                    Expect(!Shows(p, NumberText.Group(app.Meta.Economy.Price(booster.Kind))), "gameplay booster tiles have no price labels");
            });

            yield return new Fixture(89, "booster-plus-purchase", "Refill confirmation opens immediately for every empty booster", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app);
                app.Meta.Economy.Grant(20000, null);
                foreach (var booster in LevelScreen.Boosters)
                    while (app.Meta.Save.Boosters.TryUse(booster.Kind)) { }
                app.LoadLevel(88); var level = app.Level!; level.SkipGuide(); level.CloseDemo();
                level.PressBooster(BoosterKind.Return, Recovery.Return);
                var confirm = ScreenLayout.PurchaseConfirm(p.Width, p.Height, p.Insets);
                foreach (var booster in LevelScreen.Boosters)
                {
                    Run(app, p, .1f);
                    Box plus = GameplaySkin.BoosterBuyPlus(level.BoosterBoxes[booster.Kind]);
                    int petals = app.Meta.Economy.Petals, price = app.Meta.Economy.Price(booster.Kind);
                    void Ask()
                    {
                        Tap(p, plus); Run(app, p, .3f);
                        Expect(app.IsOpen(Overlay.Purchase) && app.Purchase.Offer?.Petals == price && level.Targeting == null,
                            "the plus asks before targeting or using " + booster.Kind);
                        Expect(app.Meta.Economy.Petals == petals && app.Meta.Economy.Charges(booster.Kind) == 0,
                            "opening a refill spends nothing");
                    }
                    Ask(); Tap(p, confirm.Cancel); Run(app, p, .1f);
                    Expect(app.Meta.Economy.Petals == petals && app.Meta.Economy.Charges(booster.Kind) == 0, "Cancel buys nothing");
                    Ask(); app.Back(); Run(app, p, .1f);
                    Expect(app.Meta.Economy.Petals == petals && app.Meta.Economy.Charges(booster.Kind) == 0, "Back buys nothing");
                    Ask(); Tap(p, Box.FromCenter(confirm.Confirm.Left + confirm.Confirm.Width * .03f, confirm.Confirm.CenterY, 1, 1)); app.BuyConfirmed();
                    Expect(app.Meta.Economy.Petals == petals - price
                        && app.Meta.Economy.Charges(booster.Kind) == Client.Services.Economy.EconomyService.PurchaseQuantity(booster.Kind)
                        && level.Session.BoostersUsed == 0 && level.Targeting == null,
                        "confirm buys exactly one pack without using a booster");
                }
                while (app.Meta.Save.Boosters.TryUse(BoosterKind.BloomBurst)) { }
                Run(app, p, .1f); Tap(p, GameplaySkin.BoosterBuyPlus(level.BoosterBoxes[BoosterKind.BloomBurst])); Run(app, p, .5f);
            });

            yield return new Fixture(87, "collection-cached-scroll", "Cached collection thumbnails, scrolling and close control", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app);
                app.OpenCollection(); Run(app, p, .2f);
                long before = PaintStats.Snapshot().Draws;
                Run(app, p, .2f);
                Expect((PaintStats.Snapshot().Draws - before) / 6 < 400, "settled gallery uses cached thumbnails instead of redrawing every cube");
                var gallery = CollectionGalleryDesign.Page(p.Width, p.Height, p.Insets);
                app.CollectionScrollOffset = gallery.Maximum(app.Meta.Collection.Count) * .65f;
                float offset = app.CollectionScrollOffset;
                app.CollectionDetail = 2; Run(app, p, .2f);
                Tap(p, gallery.DetailClose);
                Expect(app.CollectionDetail == -1 && app.Screen == Design.Screen.Collection && app.CollectionScrollOffset == offset,
                    "the close cross restores the gallery's exact scroll position");
                Run(app, p, .2f);
            });

            yield return new Fixture(88, "wardrobe-purchase-sizes", "Wardrobe shelf scrolled to items for sale", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app); app.OpenWardrobe();
                var r = WardrobeShelfDesign.Page(p.Width, p.Height, p.Insets);
                var cards = WardrobeShelfCatalog.Build(app.Meta.Wardrobe, CosmeticKind.Hat);
                int first = Enumerable.Range(0, cards.Count).First(i => cards[i].State == WardrobeItemState.ForSale);
                app.WardrobeBrowse.SetOffset(r.Cell(first).Top - r.Area.Top - r.W * .025f, r.Maximum(cards.Count));
                Run(app, p, .4f);
            });

            yield return new Fixture(85, "phone-scroll", "Quick swipe, glide, interrupt and fresh Shop entry", (p, data) =>
            {
                var app = Progressed(App(data), content, 87); CloseAll(app); app.OpenStore(); Run(app, p, .1f);
                var r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
                Box area = ShopBodyDesign.Bounds(r);
                float x = area.CenterX, y = area.Top + p.Width * .35f;
                p.TouchDown(x, y); p.Now += .04f;
                p.TouchMove(x, y - p.Width * .14f); p.Now += .02f;
                p.TouchUp(x, y - p.Width * .22f);
                float released = app.ShopScrollOffset;
                Expect(p.Scrolling && released > 0 && app.Overlays.Count == 0, "a quick swipe starts a glide without buying");
                Run(app, p, .18f);
                Expect(app.ShopScrollOffset > released, "the shop keeps scrolling after the finger lifts");
                p.TouchDown(x, y); float stopped = app.ShopScrollOffset; Run(app, p, .15f);
                Expect(!p.Scrolling && app.ShopScrollOffset == stopped, "a new finger stops the glide immediately"); p.TouchCancel();
                // Input samples use real time even while an incomplete frame pauses the scene clock.
                app.ShopScrollOffset = 0f; Run(app, p, .1f);
                p.InputTime = 10f; p.TouchDown(x, y);
                p.InputTime = 10.04f; p.TouchMove(x, y - p.Width * .14f);
                p.InputTime = 10.06f; p.TouchUp(x, y - p.Width * .22f);
                float waitingOffset = app.ShopScrollOffset;
                Expect(p.Scrolling, "a swipe retains its physical velocity while the scene clock is paused");
                p.BeginFrame(); app.Draw(p, 0f);
                Expect(app.ShopScrollOffset == waitingOffset, "a retained page does not coast behind the loading indicator");
                p.InputTime = null; Run(app, p, .18f);
                Expect(app.ShopScrollOffset > waitingOffset, "coasting resumes after the ready page is revealed");
                p.TouchCancel();
                app.StoreTab = 1; app.CloseStore(); Run(app, p, .1f);
                Tap(p, ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets).Petals);
                Expect(app.Screen == Design.Screen.Store && app.StoreTab == 0, "Home Petals always opens the Shop tab");
                Run(app, p, .1f);
            });
            yield return new Fixture(80,"home-promos-idle","Garden objects at rest",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);Run(app,p,.4f);
                Tap(p,ScreenLayout.ReferenceHome(p.Width,p.Height,p.Insets).Daily);
                Expect(app.IsOpen(Overlay.DailyChallenge),"the puzzle board opens Daily Challenge");
                app.Back();Run(app,p,.4f);
            });
            yield return new Fixture(81,"home-daily-complete","Quiet completed reward album",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);
                foreach(DailyRewardStep step in app.Meta.DailyReward.Steps)app.Meta.DailyReward.Claim(step.Number,adWatched:true);
                RunTo(app,p,HomePromo.DailyAt+1.1f);
                Expect(!app.Meta.DailyReward.CanClaim && p.Slots.Contains("ui.promo.daily.seal"),"completed day shows check seal");
                Expect(HomePromo.AttentionAt(PromoScene.Daily,app.PromoSeconds,false)<0,"completed album stays quiet");
            });
            yield return new Fixture(82,"daily-challenge-complete","Daily Challenge completed / replay",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);Run(app,p,.4f);
                MetaCards.DailyChallenge(p,app,1f,completed:true);
                Expect(Shows(p,PlaytestText.T("daily.play_again")) && Shows(p,PlaytestText.T("daily.reward_collected")),"replay never advertises another reward");
            });
            yield return new Fixture(83,"home-noads-hello","Sprig knocks the AD sign away",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);RunTo(app,p,HomePromo.NoAdsAt+1.1f);
            });
            yield return new Fixture(84,"home-daily-stamp","Album stamp and one warm glint",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);RunTo(app,p,HomePromo.DailyAt+2.5f);
            });

            yield return new Fixture(79,"privacy-policy-mock","Privacy policy: placeholder and return to Settings",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,14);CloseAll(app);app.OpenOverlay(Overlay.Settings);Run(app,p,.5f);
                Expect(!Shows(p,PlaytestText.T("settings.music_label")) && !Shows(p,PlaytestText.T("settings.speed_label")),"music and fast forward stay out of Settings (Sound, Haptics and the symbols mode only)");
                Tap(p,p.Texts.First(t=>t.Text==PlaytestText.T("settings.privacy_policy")).Box);Run(app,p,.5f);
                Expect(app.IsOpen(Overlay.PrivacyPolicy) && Shows(p,PlaytestText.T("settings.privacy_mock")) && !Shows(p,PlaytestText.T("settings.title")),"policy mock replaces the visible settings card");
                Expect(app.Back() && app.IsOpen(Overlay.Settings),"back returns to retained Settings");Run(app,p,.1f);
                Tap(p,p.Texts.First(t=>t.Text==PlaytestText.T("settings.privacy_policy")).Box);Run(app,p,.5f);
            });
            yield return new Fixture(78,"settings-controls","Settings: toggles persist, level controls work",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,14);CloseAll(app);app.OpenOverlay(Overlay.Settings);Run(app,p,.5f);
                Expect(!Shows(p,PlaytestText.T("settings.restore")),"Restore Purchases is absent from Settings");
                Box body=ScreenLayout.Card(p.Width,p.Height,p.Insets,PopupDesign.SettingsContent).Body;
                Box[] rows=ScreenLayout.Column(new Box(body.Left,body.Top+p.U(12),body.Right,body.Bottom),PopupDesign.SettingsRows,p.U(126),p.U(12));
                bool music=app.Meta.Save.Settings.Music,sound=app.Meta.Save.Settings.Sfx,haptics=app.Meta.Save.Settings.Haptics,speed=app.Meta.Save.Settings.Speed2x;
                Expect(!app.Meta.Save.Settings.Symbols,"the symbols mode is off by default (spec 006 FR-047)");
                for(int i=0;i<PopupDesign.SettingsRows;i++){Tap(p,CardLook.SettingsToggleBox(rows[i],p.Scale));Run(app,p,.1f);}
                DesignApp restored=App(data);
                Expect(restored.Meta.Save.Settings.Music==music && restored.Meta.Save.Settings.Sfx!=sound && restored.Meta.Save.Settings.Haptics!=haptics && restored.Meta.Save.Settings.Speed2x==speed,"sound and haptics persist; settings no longer change music or HUD speed");
                Expect(restored.Meta.Save.Settings.Symbols,"the symbols mode persists");
                int level=app.Meta.CurrentLevel;
                Tap(p,p.Texts.First(t=>t.Text=="+10").Box);Run(app,p,.1f);Expect(app.Meta.CurrentLevel==level+10,"+10 advances the level");
                Tap(p,p.Texts.First(t=>t.Text=="−1").Box);Run(app,p,.1f);Expect(app.Meta.CurrentLevel==level+9,"minus returns one level");
                Tap(p,p.Texts.First(t=>t.Text=="+1").Box);Run(app,p,.1f);Expect(app.Meta.CurrentLevel==level+10,"plus advances one level");
                Tap(p,p.Texts.First(t=>t.Text=="Reset").Box);Run(app,p,.1f);Expect(app.Meta.CurrentLevel==1 && app.Meta.Progression.HighestCompletedLevel==0,"the retained playtest Reset starts from level one");
            });
            yield return new Fixture(76,"daily-challenge","Separate Daily Challenge window",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);Run(app,p,.1f);
                app.OpenOverlay(Overlay.DailyChallenge);Run(app,p,.5f);
                Expect(Shows(p,PlaytestText.T("daily.title")) && Shows(p,PlaytestText.T("common.play")),"daily challenge has its own title and play action");
                Expect(Shows(p,PlaytestText.F("daily.reward",50)),"challenge advertises 50 Petals");
                Expect(!p.Texts.Any(t=>t.Text.Contains("(UTC)")),"no date row in Daily Challenge");
            });
            yield return new Fixture(77,"daily-reward-complete","All five rewards claimed, no action remains",(p,data)=>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);app.OpenDailyReward();Run(app,p,.5f);
                Box body=ScreenLayout.Card(p.Width,p.Height,p.Insets,DailyRewardCard.ContentUnits(5)).Body;
                Box claim=DailyRewardCard.Layout(body,p.Scale,5).Claim;
                long before=app.Meta.Economy.Petals;
                for(int i=0;i<5;i++){Tap(p,claim);Run(app,p,.15f);}
                Expect(app.Meta.DailyReward.ClaimedToday==5 && app.Meta.Economy.Petals==before+220,"all five steps pay once in order");
                Tap(p,claim);Run(app,p,.15f);Expect(app.Meta.Economy.Petals==before+220,"completed reward cannot pay again");
                Expect(!Shows(p,PlaytestText.T("daily_reward.claim")) && !Shows(p,PlaytestText.T("daily_reward.watch_claim")),"completed day hides the claim action");
            });
            yield return new Fixture(1, "splash", "Splash", (p, data) =>
            {
                // The lotus loader (spec 005 FR-039): the ring half full.
                DesignApp app = App(data, splash: true);
                Run(app, p, 1.05f);
                Expect(app.Screen == Design.Screen.Splash && !app.SplashOpening, "the splash still loads");
                Expect(Shows(p, PlaytestText.T("splash.loading")), "the splash says it is loading");
                Expect(p.Slots.Contains("ui.lotus_iris") && p.Slots.Contains("currency.petal") && p.Slots.Contains("brand.wordmark"), "the parchment, the lotus and the logo");
            });

            yield return new Fixture(2, "home-early", "Home (early levels)", (p, data) =>
            {
                // Level 5 with the pictures of the four won levels, as a player has them: the bottom menu shows its five
                // places (spec 005 FR-030), the Shop, the Wardrobe and the Leaderboard with their padlocks.
                DesignApp app = Early(App(data), content, 4);
                Run(app, p, 0.3f);
                Expect(Nav(p, app).Places.Count == 5, "every place shows on an early Home");
                Expect(!app.PlaceOpen(NavPlace.Shop) && !app.PlaceOpen(NavPlace.Wardrobe) && !app.PlaceOpen(NavPlace.Leaderboard), "the Shop, the Wardrobe and the Leaderboard are locked at Level 5");
                Expect(app.PlaceOpen(NavPlace.Home) && app.PlaceOpen(NavPlace.Collection), "Home and the Collection are open at Level 5");

                // The header row (the owner's request of 2026-10-04): a tap on the avatar opens the profile page (spec 005
                // FR-037, frame 39); the system back returns, so the frame shows Home as it stands.
                Tap(p, ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Avatar);
                Expect(app.Screen == Design.Screen.Profile && app.Overlays.Count == 0, "a tap on the avatar opens the profile page");
                Run(app, p, 0.1f);
                Expect(Shows(p, PlaytestText.T("profile.title")), "the profile page shows its banner");
                Expect(app.Back() && app.Screen == Design.Screen.Home, "the system back returns from the profile page to Home");
                Run(app, p, 0.1f);
                Expect(p.Slots.Contains(OwnerPictures.AvatarSlot), "Home's avatar shows the avatar picture");

                // The promo scenes (spec 005 FR-032): at Level 5 only No Ads, under the logo at the left (the Daily
                // Reward's comes with its unlock at L7). The frame waits for No Ads to finish its attention sequence, so
                // it shows the scene idling: Sprig pushing the crossed AD sign.
                Expect(p.Slots.Contains(HomePromo.Slot(PromoScene.NoAds)) && !p.Slots.Contains(HomePromo.Slot(PromoScene.Daily)), "an early Home shows the No Ads scene alone");
                RunTo(app, p, HomePromo.NoAdsAt + HomePromo.NoAdsSeconds + 0.4f);
                Expect(HomePromo.AttentionAt(PromoScene.NoAds, app.PromoSeconds, true) < 0f, "the No Ads scene idles again");
            });

            yield return new Fixture(3, "home-progressed", "Home (progressed)", (p, data) =>
            {
                // Both promo scenes under the logo (spec 005 FR-032), the Daily Challenge under the Daily Reward's. Home no
                // longer opens the Daily Reward by itself: its scene wears the "!" while its steps wait and the card was not
                // opened today (spec 005 FR-050; frame 4 opens it). With today's steps waiting, the frame catches the
                // scene's attention sequence as the stamp has pressed and the petals burst out of the album.
                DesignApp app = Progressed(App(data), content, 87);
                Expect(!app.IsOpen(Overlay.DailyReward), "Home does not open the Daily Reward by itself");
                CloseAll(app);
                Run(app, p, 0.3f);
                Expect(p.Slots.Contains(HomePromo.Slot(PromoScene.NoAds)) && p.Slots.Contains(HomePromo.Slot(PromoScene.Daily)), "a progressed Home shows both promo scenes");
                Expect(p.Slots.Contains("ui.promo.daily.seal"), "the Daily Reward wears a leaf seal while its steps wait unopened today");
                RunTo(app, p, HomePromo.DailyAt - .3f);
                Expect(HomePromo.AttentionAt(PromoScene.Daily, app.PromoSeconds, app.Meta.DailyReward.CanClaim) < 0f, "the Daily album idles between attention sequences");
            });

            yield return new Fixture(4, "daily-reward", "Daily Reward (popup)", (p, data) =>
            {
                // The Daily Reward's five steps (spec 001 FR-055 as amended on 2026-10-09, spec 005 FR-050): a tap on Home's
                // Daily scene opens the card and hides the "!" for the day; the system back closes it. Claimed in order,
                // step 1 for 20 Petals and step 2 after its ad's stand-in for 30, the card staying open: the frame shows
                // them checked, step 3's ad Claim waiting and steps 4 and 5's Claims greyed.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                Run(app, p, 0.3f);
                Box scene = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).DailyReward;
                Tap(p, scene);
                Expect(app.Overlays.Count == 1 && app.IsOpen(Overlay.DailyReward), "a tap on the Daily Reward's scene opens its card");
                Expect(!app.Meta.DailyReward.ShowsBadge, "opening the card hides the \"!\" for the day");
                Expect(app.Back() && app.Overlays.Count == 0 && app.Screen == Design.Screen.Home, "the system back closes the card");
                Run(app, p, 0.1f);
                Expect(!app.Meta.DailyReward.ShowsBadge && app.Meta.DailyReward.CanClaim, "Home without the \"!\" for the rest of the day, its steps still waiting");

                Tap(p, scene);
                Run(app, p, 0.6f);
                Expect(p.Slots.Contains("ui.daily.step"), "the card shows the steps' rows");
                DailyRewardRegions d = DailyRewardCard.Layout(ScreenLayout.Card(p.Width, p.Height, p.Insets, DailyRewardCard.ContentUnits(DailyRewardService.StepCount)).Body, p.U(1f), DailyRewardService.StepCount);
                long petals = app.Meta.Economy.Petals;
                Tap(p, DailyRewardCard.Row(d.Rows[1]).Button);
                Expect(app.Meta.DailyReward.ClaimedToday == 0, "step 2's greyed Claim waits for step 1");
                Tap(p, d.Claim);
                Expect(app.Meta.DailyReward.ClaimedToday == 1 && app.Meta.Economy.Petals == petals + 20 && app.IsOpen(Overlay.DailyReward), "step 1's Claim pays 20 Petals and the card stays open");
                Run(app, p, 0.2f);
                Tap(p, d.Claim);
                Expect(app.Meta.DailyReward.ClaimedToday == 2 && app.Meta.Economy.Petals == petals + 50, "step 2 pays 30 Petals after its ad's stand-in");
                Run(app, p, 0.3f);
                Expect(app.Meta.DailyReward.StateOf(3) == DailyStepState.Ready && app.Meta.DailyReward.StateOf(4) == DailyStepState.Locked, "step 3 is next, 4 and 5 wait");
                Run(app, p, 0.9f);
            });

            yield return new Fixture(5, "leaderboard", "Leaderboard", (p, data) =>
            {
                // The Leaderboard page from the bottom menu (spec 005 FR-030; the owner's request of 2026-10-04: every
                // place a page): its back and the system back return to Home, its menu goes straight to the other pages,
                // and its Shop opens the Store page over it, whose back returns here.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Leaderboard && app.Overlays.Count == 0, "the bottom menu's Leaderboard opens the page");
                Run(app, p, 0.1f);
                Expect(Nav(p, app).Active == NavPlace.Leaderboard, "the Leaderboard page raises the Leaderboard");
                Expect(Shows(p, "You") && Shows(p, "87"), "the Leaderboard page shows the player's own row with the highest completed level");
                Tap(p, Nav(p, app).Medallion);
                Expect(app.Screen == Design.Screen.Leaderboard, "a tap on the active place does nothing");
                Tap(p, ScreenLayout.ReferenceLeaderboard(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the Leaderboard page's back returns to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Back() && app.Screen == Design.Screen.Home, "the system back closes the Leaderboard page");

                // From page to page: the Leaderboard's Collection, the Collection's Leaderboard, its Shop and Home.
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Collection, "the Leaderboard page's Collection opens the Collection page");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Leaderboard, "the Collection page's Leaderboard opens the Leaderboard page");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Leaderboard, "the Leaderboard page's Shop opens the Store page over it");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Leaderboard, "the Store page's back returns to the Leaderboard page");
                Run(app, p, 0.1f);
                Tap(p, LeaderboardHonorDesign.Page(p.Width, p.Height, p.Insets).Refresh);
                Expect(app.HomeToastText == PlaytestText.T("leaderboard.offline_empty"), "Refresh says the playtest's leaderboard is offline");
                Run(app, p, 1.7f);
                Tap(p, NavTouch(p, app, NavPlace.Home));
                Expect(app.Screen == Design.Screen.Home, "the Leaderboard page's Home returns to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Run(app, p, 0.5f);
            });

            yield return new Fixture(6, "collection", "Collection", (p, data) =>
            {
                // The Collection page of a player at Level 88 with every picture won (newest first, a page at a time): its
                // page arrows turn the pages, a tap on a picture shows its detail, back (and the system back) returns to
                // the grid and a second back to Home.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                for (int level = 1; level <= 87; level++)
                {
                    if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                    {
                        app.Meta.Collection.Add(definition, level);
                    }
                }

                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Collection && app.Overlays.Count == 0, "the bottom menu's Collection opens the page");
                Run(app, p, 0.1f);
                var r = CollectionGalleryDesign.Page(p.Width, p.Height, p.Insets);
                int count = app.Meta.Collection.Count;
                Expect(Shows(p, count + " pictures"), "the count line counts every picture");
                Expect(r.Maximum(count) > 0, "the gallery scrolls through all collected mosaics");
                Drag(p, r.Area.CenterX, r.Area.CenterY, r.Area.CenterX, r.Area.CenterY - p.Width * .35f);
                Expect(app.CollectionScrollOffset > 0 && app.CollectionDetail == -1, "swiping the gallery never opens a picture");
                p.TouchCancel(); app.CollectionScrollOffset = 0;
                Run(app, p, .1f);
                Tap(p, r.Cell(0));
                Expect(app.CollectionDetail == count - 1, "a tap on the first picture (the newest) shows its detail");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Completed at Level " + NumberText.Group(app.Meta.Collection.Entries[count - 1].LevelNumber)), "the detail says its level");
                Tap(p, r.DetailClose);
                Expect(app.Screen == Design.Screen.Collection && app.CollectionDetail == -1, "the preview's cross returns to the gallery");
                Run(app, p, .1f);
                Tap(p, r.Cell(0)); Run(app, p, .1f);
                Tap(p, r.Header.Back);
                Expect(app.Screen == Design.Screen.Collection && app.CollectionDetail == -1, "back from a picture's detail returns to the grid");
                Run(app, p, 0.1f);
                Tap(p, r.Cell(1));
                Expect(app.CollectionDetail == count - 2 && app.Back() && app.Screen == Design.Screen.Collection && app.CollectionDetail == -1, "the system back from the detail returns to the grid");
                Expect(app.Back() && app.Screen == Design.Screen.Home, "a second back leaves the page for Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Run(app, p, 0.1f);
                Tap(p, r.Header.Back);
                Expect(app.Screen == Design.Screen.Home, "the Collection page's back returns to Home");
                app.OpenCollection();
                Run(app, p, 0.5f);
            });

            yield return new Fixture(7, "gameplay-normal", "Gameplay (normal)", (p, data) => Playing(App(data), p, content, 12, taps: 3));
            yield return new Fixture(8, "gameplay-hard", "Gameplay (hard)", (p, data) => Playing(App(data), p, content, 55, taps: 3));
            yield return new Fixture(9, "gameplay-super-hard", "Gameplay (super hard)", (p, data) => Playing(App(data), p, content, 82, taps: 3));

            yield return new Fixture(10, "jam", "Jam (centered card)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(27);
                app.Meta.Economy.Grant(3000, null);
                app.LoadLevel(28);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult jam = new Solver.Solver().FindJam(Session(content, app.Level!.Level), SolveOptions.Default);
                Play(app, p, jam.Trace, jam.Trace.Count);
                Settle(app, p);
                Run(app, p, 0.6f);
            });

            yield return new Fixture(11, "pause", "Pause menu", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                app.OpenOverlay(Overlay.Pause);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(12, "pod-states", "Pod states", (p, data) => PodStates(p));
            yield return new Fixture(13, "slot-states", "Waiting slot states", (p, data) => SlotStates(p));
            yield return new Fixture(14, "booster-bar", "Booster bar (progressive unlock)", (p, data) => BoosterBar(p));

            yield return new Fixture(15, "win", "Win screen", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(11);
                app.LoadLevel(12);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 12), SolveOptions.Default);
                Play(app, p, win.Trace, win.Trace.Count);
                Settle(app, p);
                var archived=new GuestWinArchive(Path.Combine(data,SaveService.FolderName,"guest-wins")).Read(12);
                Expect(archived!=null,"real game archives the win before progression");
                var verified=archived!.ValidateAndReplay(app.Level!.Session.Definition,app.Level.Session.Picture,app.Level.Session.Options);
                Expect(verified.Level==12,"archived game commands replay against trusted level content");
                // Long enough for the Petals to finish counting up (spec 003 FR-020), still within the confetti.
                Run(app, p, 1.1f);
            });

            yield return new Fixture(16, "milestone", "Milestone win", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(24);
                app.LoadLevel(25);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 25), SolveOptions.Default);
                Play(app, p, win.Trace, win.Trace.Count);
                Settle(app, p);
                Run(app, p, 1f);
                app.Level!.Next();
                Run(app, p, 2.5f);
            });

            yield return new Fixture(17, "store", "Store", (p, data) =>
            {
                // The Store page from Home's Petals "+"; its back and the system back return to Home.
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                Run(app, p, 0.1f);
                // The pill stands centered in the header row's box (the owner's request of 2026-10-04), so its middle is on it.
                Tap(p, ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Petals);
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "Home's Petals \"+\" opens the Store page");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the Store page's back returns to Home");
                app.OpenStore();
                Expect(app.Back() && app.Screen == Design.Screen.Home, "the system back closes the Store page");

                // The bottom menu (FR-030): its Shop opens the page, a tap on the medallion (the active place) does nothing,
                // its Home returns to Home.
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "the bottom menu's Shop opens the Store page");
                Run(app, p, 0.1f);
                Tap(p, Nav(p, app).Medallion);
                Expect(app.Screen == Design.Screen.Store, "a tap on the active place does nothing");
                Tap(p, NavTouch(p, app, NavPlace.Home));
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the bottom menu's Home returns to Home");
                app.OpenStore();
                Run(app, p, 0.5f);
            });
        }

        /// <summary>
        /// Extra review images beyond the board's 17 frames: themes, Settings, a Collection picture, a demo, boosters in use,
        /// the kit, the Store's cosmetics, the Wardrobe, Home's heroes in outfits, the bottom menu's locked places (the
        /// Store page, the Wardrobe and the Leaderboard page before their unlock), and the Remove Ads card.
        /// </summary>
        public static IEnumerable<Fixture> Extras(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);

            yield return new Fixture(50, "mosaic-24x24", "Prototype: 24×24 color mosaic", (p, data) => Playing(App(data), p, content, 5001, taps: 0));
            yield return new Fixture(51, "mosaic-30x32", "Prototype: 30×32 color mosaic", (p, data) => Playing(App(data), p, content, 5002, taps: 0));
            yield return new Fixture(52, "mosaic-36x36", "Prototype: 36×36 color mosaic", (p, data) => Playing(App(data), p, content, 5003, taps: 0));

            DesignApp WonMosaic(string data)
            {
                var app = App(data);
                app.Meta.SkipTo(5002);
                app.LoadLevel(5003); CloseDemo(app); app.Level!.SkipGuide();
                SolveResult win = new Solver.Solver().Solve(Session(content, 5003), new SolveOptions(200000));
                Expect(win.Status == SolveStatus.Solvable, "36×36 has a winning line");
                foreach (Command command in win.Trace)
                {
                    Expect(command is TapPod, "the large fixture never needs a booster");
                    string id = ((TapPod)command).PodId;
                    int wait = 0;
                    while (app.Level.Animator.FreeOnScreen(app.Level.Session.View) < 1 && wait++ < 1000)
                        app.Level.Advance(.2f);
                    Expect(wait < 1000, "a large-board slot becomes available");
                    int before = app.Level.Session.CommandLog.Count;
                    app.Level.Tap(id);
                    Expect(app.Level.Session.CommandLog.Count > before, "large-board solution tap accepted");
                    app.Level.Advance(.2f);
                }
                for (int wait = 0; wait < 5000 && !app.Level.Animator.Settled; wait++) app.Level.Advance(.2f);
                Expect(app.Level.Won && app.Level.Animator.Settled, "large-board win and animations complete");
                return app;
            }

            yield return new Fixture(94, "mosaic-36-win", "36×36 completed mosaic on the real win screen", (p, data) =>
            {
                var app = WonMosaic(data); Run(app, p, 1.5f);
                Expect(p.Slots.Contains("tile.picture"), "the win shows the completed large mosaic");
            });
            yield return new Fixture(95, "mosaic-36-collection", "36×36 collection detail after a real completed level", (p, data) =>
            {
                var app = WonMosaic(data); app.GoHome(); CloseAll(app); app.OpenCollection(); Run(app, p, .2f);
                var gallery = CollectionGalleryDesign.Page(p.Width, p.Height, p.Insets);
                Tap(p, gallery.Cell(0)); Run(app, p, .4f);
                Expect(app.CollectionDetail >= 0 && p.Slots.Contains("tile.picture"), "the earned large mosaic opens in collection");
            });

            yield return new Fixture(53, "store-packs", "Extra: vertically scrolled Petals packs and boosters", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                app.OpenStore();
                ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
                Box area = ShopBodyDesign.Bounds(r);
                app.ShopScrollOffset = ShopBodyDesign.Build(area, PlaytestText.T, _ => true, app.Meta.Economy.Price, app.Meta.Economy.IsUnlocked).First(n => n.Kind == ShopVisualKind.Card && n.Group == ShopGroup.Petals).Box.Top - area.Top;
                Run(app, p, 0.5f);
            });

            yield return new Fixture(54, "store-body-purchases", "Garden Store: preview offers do not charge, boosters still confirm", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 49);
                CloseAll(app);
                app.OpenStore();
                Run(app, p, 0.1f);
                ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
                Box area = ShopBodyDesign.Bounds(r);
                List<ShopVisual> nodes = ShopBodyDesign.Build(area, PlaytestText.T, _ => true, app.Meta.Economy.Price, app.Meta.Economy.IsUnlocked);
                Box featured = nodes.First(n => n.Value == "shop/hero-card-v2").Box;
                Box inset = ShopBodyDesign.FeaturedInset(featured);
                Expect(nodes.Where(n => n.Kind == ShopVisualKind.Sprite && n.Value.StartsWith("shop/boost-", StringComparison.Ordinal)).Take(4).All(n => n.Box.Within(inset)), "all featured reward icons stay inside the asset's inset at this screen shape");
                Expect(nodes.First(n => n.Id == "garden_small").Box.Within(ShopBodyDesign.FeaturedFooter(featured)), "featured price button stays inside the red footer");
                Box Button(string id) => nodes.First(n => n.Kind == ShopVisualKind.Button && n.Id == id).Box.Offset(0, -app.ShopScrollOffset);
                int petals = app.Meta.Economy.Petals;
                Tap(p, Button("remove_ads"));
                Expect(app.Meta.Economy.Petals == petals && !app.Meta.Save.Purchases.RemoveAds && app.Overlays.Count == 0, "money previews cannot spend Petals or grant Remove Ads");
                Run(app, p, 3f);
                Box header = p.Texts.First(t => t.Text == PlaytestText.T("store.title")).Box;
                Box nav = NavTouch(p, app, NavPlace.Home);
                Box money = Button("garden_small");
                p.TouchDown(money.CenterX, money.CenterY);
                p.TouchMove(money.CenterX, money.CenterY - p.Width * 0.35f);
                p.TouchUp(money.CenterX, money.CenterY - p.Width * 0.35f);
                Run(app, p, 0.1f);
                Expect(app.ShopScrollOffset > 0 && app.Meta.Economy.Petals == petals && app.Overlays.Count == 0, "vertical dragging a price scrolls content without buying");
                Expect(p.Texts.First(t => t.Text == PlaytestText.T("store.title")).Box.Equals(header) && NavTouch(p, app, NavPlace.Home).Equals(nav), "header and bottom navigation stay fixed while content scrolls");
                float offset = app.ShopScrollOffset;
                p.TouchDown(area.CenterX, area.CenterY);
                p.TouchMove(area.CenterX + p.Width * 0.25f, area.CenterY);
                p.TouchUp(area.CenterX + p.Width * 0.25f, area.CenterY);
                Expect(app.ShopScrollOffset == offset && app.StoreRowsPage == 0, "horizontal dragging never changes the shop page or offset");
                Expect(p.ScrollAt(header.CenterX, header.CenterY) == -1 && p.ScrollAt(nav.CenterX, nav.CenterY) == -1, "fixed navigation is outside the content scroll area");
                ShopVisual booster = nodes.First(n => n.Kind == ShopVisualKind.Button && n.Id == "booster_ExtraSlot");
                app.ShopScrollOffset = booster.Box.CenterY - area.CenterY;
                Run(app, p, 0.1f);
                int charges = app.Meta.Economy.Charges(BoosterKind.ExtraSlot);
                Tap(p, Button("booster_ExtraSlot"));
                Expect(app.IsOpen(Overlay.Purchase), "the booster price asks for confirmation");
                Run(app, p, 0.1f);
                PurchaseConfirmRegions confirm = ScreenLayout.PurchaseConfirm(p.Width, p.Height, p.Insets);
                Tap(p, confirm.Cancel);
                Expect(app.Meta.Economy.Petals == petals && app.Meta.Economy.Charges(BoosterKind.ExtraSlot) == charges, "cancel changes nothing");
                Run(app, p, 0.1f);
                Tap(p, Button("booster_ExtraSlot"));
                Run(app, p, 0.1f);
                Tap(p, confirm.Confirm);
                Expect(app.Meta.Economy.Petals == petals - app.Meta.Economy.Price(BoosterKind.ExtraSlot) && app.Meta.Economy.Charges(BoosterKind.ExtraSlot) == charges + 3, "the real Petal button grants exactly the agreed three charges");
                Run(app, p, 0.1f);
                p.TouchDown(area.CenterX, area.CenterY);
                p.TouchMove(area.CenterX, area.CenterY - p.Height * 5);
                p.TouchUp(area.CenterX, area.CenterY - p.Height * 5);
                Run(app, p, 0.1f);
                float max = ShopBodyDesign.ContentHeight(nodes, area) - area.Height;
                Expect(Math.Abs(app.ShopScrollOffset - max) < 1, "scroll clamps at content bottom");
                p.TouchDown(area.CenterX, area.CenterY);
                p.TouchMove(area.CenterX, area.CenterY + p.Height * 5);
                p.TouchUp(area.CenterX, area.CenterY + p.Height * 5);
                Run(app, p, 0.1f);
                Expect(app.ShopScrollOffset == 0, "scroll clamps at content top");
                app.ShopScrollOffset = booster.Box.CenterY - area.CenterY;
                Run(app, p, 0.1f);
            });

            yield return new Fixture(18, "themes", "Extra: background themes", (p, data) =>
            {
                p.BeginFrame();
                (int Level, string Name)[] themes = { (1, "Daylight Garden"), (100, "Pond"), (150, "Orchard"), (200, "Moonlit Garden") };
                float w = p.Width / themes.Length;
                Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
                for (int i = 0; i < themes.Length; i++)
                {
                    var column = new Box(i * w, 0f, (i + 1) * w, p.Height);
                    Client.Gameplay.Themes.BackgroundTheme theme = Client.Gameplay.Themes.ThemeRotation.Default.ThemeFor(themes[i].Level);
                    p.Mark("bg.theme." + theme.Id);

                    // The theme's own gameplay picture (the owner's, pictures.md B2 to B5) as the level shows it, cover-fitted
                    // to the whole screen around the column's middle; the drawn lawn while the picture is missing.
                    var wide = new Box(column.CenterX - (p.Width / 2f), 0f, column.CenterX + (p.Width / 2f), p.Height);
                    Visuals.Background(
                        p,
                        column,
                        OwnerPictures.Gameplay(theme.Id),
                        () =>
                        {
                            p.PushClip(column);
                            p.Backdrop(wide, DesignTokens.Backdrop(theme.Background, theme.Accent), BackdropScene.Gameplay, theme.Id + "/wide");
                            p.PopClip();
                        },
                        (pw, ph) =>
                        {
                            float scale = Math.Max(wide.Width / pw, wide.Height / ph);
                            return Box.FromCenter(wide.CenterX, wide.CenterY, pw * scale, ph * scale);
                        });

                    // The theme's name on a small wooden plaque, and a corner of a board on its lawn: candy tiles in the
                    // stone border (spec 005 §4.1, §4.2).
                    Kit.WoodSign(p, Box.FromCenter(column.CenterX, safe.Top + p.U(90f), w * 0.9f, p.U(84f)), themes[i].Name, T.Caption);
                    float cell = Math.Min(w * 0.22f, p.U(64f));
                    var grid = Box.FromCenter(column.CenterX, p.Height * 0.5f, cell * 3f, cell * 3f);
                    Kit.StoneBorder(p, grid, cell);
                    for (int c = 0; c < 9; c++)
                    {
                        VariantId variant = ThemeTiles[(c + (i * 2)) % ThemeTiles.Length];
                        float x = grid.Left + ((c % 3) * cell);
                        float y = grid.Top + ((c / 3) * cell);
                        Kit.CandyTile(p, new Box(x, y, x + cell, y + cell).Inset(cell * 0.02f), variant, TileStyle.Board);
                    }
                }
            });

            yield return new Fixture(19, "settings", "Extra: Settings", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                app.OpenOverlay(Overlay.Pause);
                app.OpenOverlay(Overlay.Settings);
                Run(app, p, 0.5f);
            });

            yield return new Fixture(20, "collection-detail", "Extra: Collection picture", (p, data) =>
            {
                // A picture's detail on the Collection page: the fourth frame of the grid (newest first) is the third
                // picture won.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                app.OpenCollection();
                Run(app, p, 0.1f);
                Expect(Nav(p, app).Active == NavPlace.Collection, "the Collection page raises the Collection");
                var gallery = CollectionGalleryDesign.Page(p.Width, p.Height, p.Insets);
                int slot = app.Meta.Collection.Count - 1 - 2;
                app.CollectionScrollOffset = Math.Max(0, Math.Min(gallery.Maximum(app.Meta.Collection.Count), gallery.Cell(slot).Top - gallery.Area.Top));
                Run(app, p, .1f);
                Tap(p, gallery.Cell(slot).Offset(0, -app.CollectionScrollOffset));
                Expect(app.Screen == Design.Screen.Collection && app.CollectionDetail == 2, "a tap on a picture shows its detail on the page");
                Run(app, p, 0.5f);
            });

            yield return new Fixture(21, "demo", "Extra: booster demo (the guided, forced and free first use, spec 005 FR-035)", (p, data) =>
            {
                // Level 3: Extra Slot's button lit, the screen dimmed; a tap elsewhere does nothing (forced).
                DesignApp app = App(data);
                app.Meta.SkipTo(2);
                app.LoadLevel(3);
                Run(app, p, 0.5f);
                LevelScreen level = app.Level!;
                Expect(level.Guide?.Kind == GuideKind.Booster && level.Guide.Booster == BoosterKind.ExtraSlot && level.Guide.Forced, "Level 3 opens on Extra Slot's forced demo");
                Tap(p, level.PodBoxes.Values.First());
                Expect(level.Guide?.Kind == GuideKind.Booster && level.Session.CommandLog.Count == 0, "a tap off the lit booster does nothing");
                Run(app, p, 0.5f);
            });

            yield return new Fixture(22, "shuffle", "Extra: Shuffle in use", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 55), SolveOptions.Default);
                Play(app, p, win.Trace, 2);
                Run(app, p, 0.4f);
                app.Level!.UseBooster(BoosterKind.Shuffle, new UseShuffle(), free: true);
                Run(app, p, 0.15f);
            });

            yield return new Fixture(23, "bloom-burst", "Extra: Bloom Burst in use", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(54);
                app.LoadLevel(55);
                CloseDemo(app);
                Run(app, p, 0.1f);
                VariantId variant = app.Level!.Session.Definition.Pods[0].Variant;
                app.Level.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant), free: true);
                Run(app, p, 0.2f);
            });
            yield return new Fixture(24, "bloomlings", "Extra: Bloomlings", (p, data) => Bloomlings(p));
            yield return new Fixture(26,"wardrobe-purchases","Wardrobe purchases confirm before spending",(p,data)=>
            {
                var app=Progressed(App(data),content,87);app.Meta.Economy.Grant(12000,null);CloseAll(app);app.OpenWardrobe();Run(app,p,.1f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig).ToList();
                int index=shelf.FindIndex(c=>c.State==WardrobeItemState.ForSale);Expect(index>0,"paid head item remains available");
                var card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(shelf.Count));Run(app,p,.1f);
                long petals=app.Meta.Economy.Petals;
                Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                Expect(!app.IsOpen(Overlay.Purchase)&&app.WardrobeBrowse.PreviewItem?.Id==shelf[index].Item?.Id,"card tap only fits the item");
                Tap(p,WardrobeShelfDesign.Action(card.Offset(0,-app.WardrobeBrowse.Offset)));Run(app,p,.1f);
                Expect(app.IsOpen(Overlay.Purchase),"Wardrobe asks before buying");Expect(app.Meta.Economy.Petals==petals,"opening confirmation spends nothing");
                app.Back();Run(app,p,.1f);Expect(app.Meta.Economy.Petals==petals,"cancel spends nothing");
            });
            yield return new Fixture(55,"wardrobe-bottom","Wardrobe scroll reaches its last item above the menu",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig);
                app.WardrobeBrowse.SetOffset(r.Maximum(shelf.Count),r.Maximum(shelf.Count));Run(app,p,.1f);
                Expect(r.Cell(shelf.Count-1).Bottom-app.WardrobeBrowse.Offset<=r.NavTop,"last item clears menu");
                Expect(app.Overlays.Count==0,"scroll opens no purchase");
            });
            yield return new Fixture(56,"wardrobe-bloom","Wardrobe family switching preserves category",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(CosmeticKind.Glasses);Run(app,p,.1f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);Tap(p,NavigationSkin.Segment(r.Families,1,4));Run(app,p,.1f);
                Expect(app.WardrobeBrowse.Family==Family.Bloom&&app.WardrobeBrowse.Kind==CosmeticKind.Glasses,"family switch retains selected slot");
            });
            yield return new Fixture(25, "kit", "Extra: reference look kit", (p, data) => KitSheet(p));
            yield return new Fixture(73, "leaderboard-customized", "Leaderboard: saved avatars, frames, badges and long names", (p,data) =>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);
                var own=new LeaderboardEntry(248,"You",87,true);
                var rows=new List<LeaderboardEntry>
                {
                    new LeaderboardEntry(1,"AVeryLongGardenersNameThatMustNotCoverTheLevel",50000,false,Appearance:new LeaderboardAppearance("avatar.drop_sailor_sticker","frame.golden_ribbon","badge.sprout",null)),
                    new LeaderboardEntry(2,"Rosie",49999,false,Appearance:new LeaderboardAppearance("avatar.bloom_default","frame.flower_wreath","badge.achievement.natural_talent",null)),
                    new LeaderboardEntry(3,"Sprig",49998,false,Appearance:new LeaderboardAppearance("avatar.sprig_default","frame.leaf_ring","badge.achievement.key_keeper",null)),
                    own,
                };
                for(int rank=4;rank<=14;rank++)rows.Add(new LeaderboardEntry(rank,rank==4?"GardenerWithAnExtremelyLongDisplayName":"Gardener "+rank,49000-rank,false,Appearance:new LeaderboardAppearance("avatar.twig_default",rank%2==0?"frame.stone_ring":"frame.wood_ring",null,null)));
                app.PreviewLeaderboard=new LeaderboardPage(rows,own);app.Navigate(NavPlace.Leaderboard);Run(app,p,.1f);
                Expect(Shows(p,"You")&&Shows(p,"87"),"own row remains visible separately from the neighbour scroll");
                var r=LeaderboardHonorDesign.Page(p.Width,p.Height,p.Insets);
                Drag(p,r.Rows.CenterX,r.Rows.CenterY,r.Rows.CenterX,r.Rows.CenterY-r.W*.12f);Run(app,p,.1f);
                Expect(app.LeaderboardScrollOffset>0,"rank rows scroll with fixed podium and own row");
            });
            yield return new Fixture(74, "wardrobe-scroll", "Wardrobe: drag, filter memory and purchase confirmation", (p,data) =>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();
                app.Meta.Wardrobe.Equip(Family.Sprig,SprigV2Art.RosetteId);Run(app,p,.1f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                Drag(p,r.Area.CenterX,r.Area.CenterY,r.Area.CenterX,r.Area.Top-6000);Run(app,p,.1f);
                float offset=app.WardrobeBrowse.Offset;Expect(offset>0,"a long inventory scrolls to the last shelf");
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig,CosmeticKind.Hat)?.Id==SprigV2Art.RosetteId,"swiping cannot equip or buy an item");
                Drag(p,r.Categories.CenterX,r.Categories.CenterY,r.Categories.CenterX,r.Categories.CenterY-r.W*.2f);Run(app,p,.1f);
                Expect(app.WardrobeBrowse.Offset==offset,"filters intercept drags");
                var nav=HomeScreen.Nav(p,NavPlace.Wardrobe);Box menu=GameNavigationDesign.NavButton(nav,1);
                Drag(p,menu.CenterX,menu.CenterY,menu.CenterX,menu.CenterY-r.W*.2f);Run(app,p,.1f);
                Expect(app.WardrobeBrowse.Offset==offset&&app.Screen==Design.Screen.Wardrobe&&app.Overlays.Count==0,"dragging off the menu never scrolls, fits or opens a purchase");
                Tap(p,NavigationSkin.Segment(r.Families,1,4));Run(app,p,.1f);Expect(app.WardrobeBrowse.Family==Family.Bloom&&app.WardrobeBrowse.Offset==0,"new family starts at its own offset");
                Tap(p,NavigationSkin.Segment(r.Families,0,4));Run(app,p,.1f);Expect(app.WardrobeBrowse.Offset==offset,"returning to family restores its offset");
                Tap(p,NavigationSkin.Segment(r.Categories,1,4));Run(app,p,.1f);Expect(app.WardrobeBrowse.Kind==CosmeticKind.Glasses,"category filters the catalog");
                Tap(p,NavigationSkin.Segment(r.Categories,0,4));Run(app,p,.1f);Expect(app.WardrobeBrowse.Offset==offset,"category restores its own scroll");
                var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig);int bucket=Enumerable.Range(0,cards.Count).First(i=>cards[i].Item?.Id==SprigV2Art.BucketId);
                Box bucketBox=r.Cell(bucket);app.WardrobeBrowse.SetOffset(bucketBox.Bottom-r.Area.Bottom+r.W*.025f,r.Maximum(cards.Count));Run(app,p,.1f);offset=app.WardrobeBrowse.Offset;
                long before=app.Meta.Economy.Petals;Tap(p,WardrobeShelfDesign.Action(bucketBox.Offset(0,-offset)));Run(app,p,.1f);
                Expect(app.IsOpen(Overlay.Purchase)&&app.Meta.Economy.Petals==before,"tap opens confirmation without spending");
                Tap(p,ScreenLayout.PurchaseConfirm(p.Width,p.Height,p.Insets).Cancel);Run(app,p,.1f);
                Expect(app.Meta.Economy.Petals==before&&app.WardrobeBrowse.Offset==offset,"cancel preserves balance and scroll");
            });
            yield return new Fixture(75, "wardrobe-purchase", "Wardrobe: confirmed purchase equips once without losing scroll", (p,data) =>
            {
                DesignApp app=Progressed(App(data),content,87);CloseAll(app);app.OpenWardrobe();Run(app,p,.1f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                Drag(p,r.Area.CenterX,r.Area.CenterY,r.Area.CenterX,r.Area.Top-6000);Run(app,p,.1f);
                float offset=app.WardrobeBrowse.Offset;
                var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig);
                int index=Enumerable.Range(0,cards.Count).First(i=>cards[i].Item?.Id==SprigV2Art.BucketId);
                long balance=app.Meta.Economy.Petals;
                app.WardrobeBrowse.SetOffset(r.Cell(index).Bottom-r.Area.Bottom+r.W*.025f,r.Maximum(cards.Count));Run(app,p,.1f);offset=app.WardrobeBrowse.Offset;
                Box bucket=r.Cell(index).Offset(0,-offset);
                Tap(p,bucket);Run(app,p,.1f);
                Expect(!app.IsOpen(Overlay.Purchase)&&app.WardrobeBrowse.PreviewItem?.Id==SprigV2Art.BucketId,"unowned card is a free temporary fitting");
                Tap(p,WardrobeShelfDesign.Action(bucket));Run(app,p,.1f);
                Expect(app.IsOpen(Overlay.Purchase),"green price button asks for confirmation");
                Tap(p,ScreenLayout.PurchaseConfirm(p.Width,p.Height,p.Insets).Confirm);Run(app,p,.1f);
                Expect(app.Meta.Save.Cosmetics.Owned.Contains(SprigV2Art.BucketId)&&app.Meta.Economy.Petals==balance-650,"confirmation buys at the catalog price once");
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig,CosmeticKind.Hat)?.Id==SprigV2Art.BucketId&&app.WardrobeBrowse.Offset==offset,"purchase equips without moving the shelves");
                Tap(p,bucket);Run(app,p,.1f);
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig,CosmeticKind.Hat)?.Id==SprigV2Art.BucketId&&app.Meta.Economy.Petals==balance-650,"owned item stays equipped without another charge");
                Tap(p,bucket);Run(app,p,.1f);
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig,CosmeticKind.Hat)?.Id==SprigV2Art.BucketId&&app.Overlays.Count==0,"owned item equips immediately");
            });
            foreach(var selection in new[]{
                (171,"sprig-round",Family.Sprig,SprigV2Art.GlassesId),(172,"sprig-square",Family.Sprig,EyewearArt.SprigSquareId),
                (173,"bloom-pink",Family.Bloom,BloomV2Art.GlassesId),(174,"bloom-heart",Family.Bloom,EyewearArt.BloomHeartId),
                (175,"twig-brown",Family.Twig,TwigV2Art.GlassesId),(176,"twig-octagon",Family.Twig,EyewearArt.TwigOctagonId)})
            {
                var scene=selection;
                yield return new Fixture(scene.Item1,"eyewear-"+scene.Item2,"Owner eyewear: real Face card equips and persists in both views",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    if(scene.Item1%2==0)
                        foreach(string id in scene.Item3 switch {
                            Family.Sprig=>new[]{SprigV2Art.EarmuffsId,SprigV2Art.JacketId,SprigV2Art.ScarfId},
                            Family.Bloom=>new[]{BloomV2Art.RoseId,BloomV2Art.CoatId,BloomV2Art.KnitId},
                            _=>new[]{TwigV2Art.BucketId,TwigV2Art.CoatId,TwigV2Art.ScarfId}})
                            Expect(app.Meta.Wardrobe.Equip(scene.Item3,id),"headwear, clothes and scarf fit together with new eyewear");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(scene.Item3);app.WardrobeBrowse.Select(CosmeticKind.Glasses);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Glasses,scene.Item3).ToList();
                    int index=cards.FindIndex(c=>c.Item?.Id==scene.Item4);Expect(index>0,"delivered Face item appears");Tap(p,r.Cell(index));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(scene.Item3).Glasses?.Id==scene.Item4,"Face card equips without changing Head");
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    var home=HomeHeroArt.Layer(scene.Item3,app.Meta.Wardrobe.OutfitOf(scene.Item3),3)!.Value;
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+home.Name),"Home draws the new angled eyewear");
                    app.OpenWardrobe();p.DrawnSprites.Clear();Run(app,p,.1f);
                    var front=HeroGearArt.Layer(scene.Item3,app.Meta.Wardrobe.OutfitOf(scene.Item3),3)!.Value;
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+front.Name),"fitting stand draws the new frontal eyewear");
                });
            }
            yield return new Fixture(177,"eyewear-home","New square, heart and octagon glasses on the fountain heroes",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                foreach(var pair in new[]{(Family.Sprig,EyewearArt.SprigSquareId),(Family.Bloom,EyewearArt.BloomHeartId),(Family.Twig,EyewearArt.TwigOctagonId)})
                    Expect(app.Meta.Wardrobe.Equip(pair.Item1,pair.Item2),"new Face item equips");
                app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                foreach(var family in new[]{Family.Sprig,Family.Bloom,Family.Twig})
                    Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+HomeHeroArt.Layer(family,app.Meta.Wardrobe.OutfitOf(family),3)!.Value.Name),"equipped eyewear remains on Home");
            });
            foreach(var selection in new[]{
                (178,"boater","hat.bloom_boater",CosmeticKind.Hat),
                (179,"buckethatgreen","hat.bloom_bucket_green",CosmeticKind.Hat),
                (180,"crownhair","hat.bloom_crown",CosmeticKind.Hat),
                (181,"dresspurple","clothes.bloom_dress_purple",CosmeticKind.Clothes),
                (182,"flowerhat","hat.bloom_flower_hat",CosmeticKind.Hat),
                (183,"headscarf","hat.bloom_headscarf",CosmeticKind.Hat),
                (184,"nightcap","hat.bloom_nightcap",CosmeticKind.Hat),
                (185,"panama","hat.bloom_summer_panama",CosmeticKind.Hat),
                (186,"topflower","clothes.bloom_top_flower",CosmeticKind.Clothes)})
            {
                var item=selection;
                yield return new Fixture(item.Item1,"bloom-extra-"+item.Item2,"Bloom fitted item: selection, Home and return",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    app.Meta.Wardrobe.Equip(Family.Bloom,EyewearArt.BloomHeartId);
                    if(item.Item4==CosmeticKind.Hat)app.Meta.Wardrobe.Equip(Family.Bloom,"clothes.bloom_dress_purple");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Bloom);app.WardrobeBrowse.Select(item.Item4);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Item4,Family.Bloom).ToList();
                    int index=cards.FindIndex(c=>c.Item?.Id==item.Item3);Expect(index>0,"new Bloom item appears in its category");
                    var card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(cards.Count));Run(app,p,.1f);
                    Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Bloom).Get(item.Item4)?.Id==item.Item3,"new item equips from its real card");
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    for(int layer=0;layer<HeroGearArt.LayerCount;layer++)
                    {
                        var frame=HomeHeroArt.Layer(Family.Bloom,app.Meta.Wardrobe.OutfitOf(Family.Bloom),layer);
                        if(frame.HasValue)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Value.Name),"Home draws the complete new outfit");
                    }
                    app.OpenWardrobe();Run(app,p,.1f);
                });
            }
            yield return new Fixture(187,"bloom-extra-outfit","Bloom crown, dress, heart glasses and scarf",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                foreach(string id in new[]{"hat.bloom_crown","clothes.bloom_dress_purple",EyewearArt.BloomHeartId,BloomV2Art.KnitId})Expect(app.Meta.Wardrobe.Equip(Family.Bloom,id),"combined Bloom outfit equips");
                app.OpenWardrobe();app.WardrobeBrowse.Select(Family.Bloom);app.WardrobeBrowse.Select(CosmeticKind.Clothes);Run(app,p,.1f);
            });
            yield return new Fixture(188,"bloom-extra-home","Fitted Bloom accessories on Home",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                foreach(string id in new[]{"hat.bloom_flower_hat","clothes.bloom_top_flower",EyewearArt.BloomHeartId,BloomV2Art.StripeId})Expect(app.Meta.Wardrobe.Equip(Family.Bloom,id),"combined Home outfit equips");
                app.GoHome();Run(app,p,.4f);
            });
            foreach(var selection in new[]{
                (189,"sprig-bowlerhat",Family.Sprig,"hat.sprig_bowler",CosmeticKind.Hat),
                (190,"sprig-explorerhat",Family.Sprig,"hat.sprig_explorer",CosmeticKind.Hat),
                (191,"sprig-hairclover",Family.Sprig,"hat.sprig_clover",CosmeticKind.Hat),
                (192,"sprig-hairsprout",Family.Sprig,"hat.sprig_sprout",CosmeticKind.Hat),
                (193,"sprig-headscarf",Family.Sprig,"hat.sprig_headscarf",CosmeticKind.Hat),
                (194,"sprig-nightcap",Family.Sprig,"hat.sprig_nightcap",CosmeticKind.Hat),
                (195,"sprig-vestexplorer",Family.Sprig,"clothes.sprig_explorer_vest",CosmeticKind.Clothes),
                (196,"twig-vestexplorer",Family.Twig,"clothes.twig_explorer_vest",CosmeticKind.Clothes),
                (197,"sprig-boater",Family.Sprig,"hat.sprig_boater",CosmeticKind.Hat),
                (198,"twig-hairpinecone",Family.Twig,"hat.twig_pinecone",CosmeticKind.Hat),
                (199,"twig-headscarf",Family.Twig,"hat.twig_headscarf",CosmeticKind.Hat),
                (200,"twig-mushroomcap",Family.Twig,"hat.twig_mushroom",CosmeticKind.Hat),
                (201,"twig-nightcap",Family.Twig,"hat.twig_nightcap",CosmeticKind.Hat),
                (202,"twig-shirtexplorer",Family.Twig,"clothes.twig_explorer_shirt",CosmeticKind.Clothes),
                (203,"twig-boater",Family.Twig,"hat.twig_boater",CosmeticKind.Hat),
                (204,"twig-bowlerhat",Family.Twig,"hat.twig_bowler",CosmeticKind.Hat),
                (205,"twig-explorerhat",Family.Twig,"hat.twig_explorer",CosmeticKind.Hat)})
            {
                var item=selection;
                yield return new Fixture(item.Item1,"addition-"+item.Item2,"Fitted Sprig/Twig item: select, Home, return",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    app.Meta.Wardrobe.Equip(item.Item3,item.Item3==Family.Sprig?EyewearArt.SprigSquareId:EyewearArt.TwigOctagonId);
                    if(item.Item5==CosmeticKind.Hat)app.Meta.Wardrobe.Equip(item.Item3,item.Item3==Family.Sprig?"clothes.sprig_explorer_vest":"clothes.twig_explorer_vest");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(item.Item3);app.WardrobeBrowse.Select(item.Item5);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Item5,item.Item3).ToList();
                    int index=cards.FindIndex(c=>c.Item?.Id==item.Item4);Expect(index>0,"new item has its own card");var card=r.Cell(index);
                    app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(cards.Count));Run(app,p,.1f);
                    Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(item.Item3).Get(item.Item5)?.Id==item.Item4,"new item equips in its correct slot");
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    for(int layer=0;layer<HeroGearArt.LayerCount;layer++)
                    {
                        var frame=HomeHeroArt.Layer(item.Item3,app.Meta.Wardrobe.OutfitOf(item.Item3),layer);
                        if(frame.HasValue)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Value.Name),"Home keeps every fitted outfit layer");
                    }
                    app.OpenWardrobe();Run(app,p,.1f);
                });
            }
            foreach(var combo in new[]{(206,Family.Sprig,"hat.sprig_headscarf","clothes.sprig_explorer_vest",EyewearArt.SprigSquareId,SprigV2Art.ScarfId),(207,Family.Twig,"hat.twig_mushroom","clothes.twig_explorer_shirt",EyewearArt.TwigOctagonId,TwigV2Art.ScarfId)})
            {
                var look=combo;
                yield return new Fixture(look.Item1,"addition-outfit-"+look.Item2.ToString().ToLowerInvariant(),"Fitted Head, Body, Face and scarf",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    foreach(string id in new[]{look.Item3,look.Item4,look.Item5,look.Item6})Expect(app.Meta.Wardrobe.Equip(look.Item2,id),"combined outfit equips");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(look.Item2);app.WardrobeBrowse.Select(CosmeticKind.Clothes);Run(app,p,.1f);
                });
            }
            yield return new Fixture(208,"addition-home","New Sprig and Twig outfits around the lotus",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                foreach(var item in new[]{(Family.Sprig,"hat.sprig_clover"),(Family.Sprig,"clothes.sprig_explorer_vest"),(Family.Sprig,SprigV2Art.GlassesId),(Family.Twig,"hat.twig_pinecone"),(Family.Twig,"clothes.twig_explorer_vest"),(Family.Twig,TwigV2Art.GlassesId)})Expect(app.Meta.Wardrobe.Equip(item.Item1,item.Item2),"new Home outfit equips");
                app.GoHome();Run(app,p,.4f);
            });
            foreach(var selection in new[]{
                (209,"nightcap",Family.Drop,"hat.drop_nightcap",CosmeticKind.Hat),
                (210,"topsailor",Family.Drop,"clothes.drop_sailor_top",CosmeticKind.Clothes),
                (211,"hairshell",Family.Drop,"hat.drop_shell",CosmeticKind.Hat),
                (212,"sailorhat",Family.Drop,"hat.drop_sailor_hat",CosmeticKind.Hat),
                (213,"headscarf",Family.Drop,"hat.drop_headscarf",CosmeticKind.Hat),
                (214,"shirtmint",Family.Drop,"clothes.drop_mint_shirt",CosmeticKind.Clothes),
                (215,"explorerhat",Family.Drop,"hat.drop_explorer",CosmeticKind.Hat),
                (216,"boater",Family.Drop,"hat.drop_boater",CosmeticKind.Hat),
                (217,"rainhat",Family.Drop,"hat.drop_rain_hat",CosmeticKind.Hat)})
            {
                var item=selection;
                yield return new Fixture(item.Item1,"drop-extra-"+item.Item2,"Fitted Drop item: select, Home, return",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    app.Meta.Wardrobe.Equip(item.Item3,DropV2Art.GlassesId);
                    if(item.Item5==CosmeticKind.Hat)app.Meta.Wardrobe.Equip(item.Item3,"clothes.drop_sailor_top");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(item.Item3);app.WardrobeBrowse.Select(item.Item5);Run(app,p,.1f);
                    var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,item.Item5,item.Item3).ToList();
                    int index=cards.FindIndex(c=>c.Item?.Id==item.Item4);Expect(index>0,"new item has its own card");var card=r.Cell(index);
                    app.WardrobeBrowse.SetOffset(card.Top-r.Area.Top-r.Area.Height*.08f,r.Maximum(cards.Count));Run(app,p,.1f);
                    Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);
                    Expect(app.Meta.Wardrobe.OutfitOf(item.Item3).Get(item.Item5)?.Id==item.Item4,"new item equips in its correct slot");
                    app.GoHome();p.DrawnSprites.Clear();Run(app,p,.4f);
                    for(int layer=0;layer<HeroGearArt.LayerCount;layer++)
                    {
                        var frame=HomeHeroArt.Layer(item.Item3,app.Meta.Wardrobe.OutfitOf(item.Item3),layer);
                        if(frame.HasValue)Expect(p.DrawnSprites.Contains(PainterBase.HeroMotionPrefix+frame.Value.Name),"Home keeps every fitted outfit layer");
                    }
                    app.OpenWardrobe();Run(app,p,.1f);
                });
            }
            foreach(var combo in new[]{(218,Family.Sprig,"hat.sprig_clover","clothes.sprig_explorer_vest",EyewearArt.SprigSquareId,SprigV2Art.ScarfId),(219,Family.Twig,"hat.twig_pinecone","clothes.twig_coat",EyewearArt.TwigOctagonId,TwigV2Art.ScarfId),(220,Family.Bloom,"hat.bloom_crown","clothes.bloom_dress_purple",EyewearArt.BloomHeartId,BloomV2Art.KnitId),(221,Family.Drop,"hat.drop_boater","clothes.drop_mint_shirt",DropV2Art.GlassesId,DropV2Art.StripeId)})
            {
                var look=combo;
                yield return new Fixture(look.Item1,"clothes-finish-"+look.Item2.ToString().ToLowerInvariant(),"Cleaned garments with Head, Face and scarf",(p,data)=>
                {
                    var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                    foreach(string id in new[]{look.Item3,look.Item4,look.Item5,look.Item6})Expect(app.Meta.Wardrobe.Equip(look.Item2,id),"cleaned outfit equips");
                    app.OpenWardrobe();app.WardrobeBrowse.Select(look.Item2);app.WardrobeBrowse.Select(CosmeticKind.Clothes);Run(app,p,.1f);
                });
            }
            yield return new Fixture(222,"clothes-finish-home","Accepted quartet with cleaned garments and Drop additions",(p,data)=>
            {
                var app=Progressed(App(data),content,87);CloseAll(app);OwnHeroGear(app);
                foreach(var look in new[]{(Family.Sprig,"hat.sprig_clover","clothes.sprig_explorer_vest",EyewearArt.SprigSquareId),(Family.Bloom,"hat.bloom_crown","clothes.bloom_top_flower",EyewearArt.BloomHeartId),(Family.Drop,"hat.drop_rain_hat","clothes.drop_sailor_top",DropV2Art.GlassesId),(Family.Twig,"hat.twig_pinecone","clothes.twig_explorer_shirt",EyewearArt.TwigOctagonId)})
                    foreach(string id in new[]{look.Item2,look.Item3,look.Item4})Expect(app.Meta.Wardrobe.Equip(look.Item1,id),"Home outfit equips");
                app.GoHome();Run(app,p,.4f);
            });
            yield return new Fixture(223,"wardrobe-release-before-40","Wardrobe remains locked before Level 40",(p,data)=>
            {
                var app=Progressed(App(data),content,38);CloseAll(app);app.OpenWardrobe();Run(app,p,.3f);
                Expect(!app.Meta.Wardrobe.IsAvailable&&!app.Meta.Wardrobe.HasFreeHeroItems,"starter gear cannot bypass the L40 gate");
            });
            foreach(var sample in new[]{(224,40,Family.Sprig,CosmeticKind.Hat),(225,40,Family.Bloom,CosmeticKind.Clothes),(226,60,Family.Twig,CosmeticKind.Hat),(227,80,Family.Drop,CosmeticKind.Hat),(228,100,Family.Bloom,CosmeticKind.Hat)})
            {
                var scene=sample;
                yield return new Fixture(scene.Item1,"wardrobe-release-l"+scene.Item2+"-"+scene.Item3.ToString().ToLowerInvariant(),"Release assortment: starters, prices and level locks",(p,data)=>
                {
                    var app=Progressed(App(data),content,scene.Item2-1);CloseAll(app);app.OpenWardrobe();app.WardrobeBrowse.Select(scene.Item3);app.WardrobeBrowse.Select(scene.Item4);Run(app,p,.3f);
                    var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,scene.Item4,scene.Item3);
                    Expect(cards.All(c=>c.Item==null||(!c.Item.Retired&&!c.Item.DefaultLook)),"only fitted, distinct gear appears");
                    Expect(cards.Any(c=>c.State==WardrobeItemState.ForSale),"new players see affordable choices");
                    Expect(Shows(p,"350")||Shows(p,"650")||Shows(p,"850")||Shows(p,NumberText.Group(1000)),"available price is drawn");
                    if(scene.Item2<100)Expect(cards.Any(c=>c.State==WardrobeItemState.LevelLocked),"later gear remains visibly locked");
                    else Expect(cards.All(c=>c.State!=WardrobeItemState.LevelLocked),"all choices are open by L100");
                });
            }
            yield return new Fixture(229,"wardrobe-release-buy","Real card: try, cancel, buy, reload, re-tap",(p,data)=>
            {
                var app=Progressed(App(data),content,39);CloseAll(app);app.OpenWardrobe();Run(app,p,.2f);
                var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig).ToList();
                int index=cards.FindIndex(c=>c.Item?.Id==SprigV2Art.CapId);var item=cards[index].Item!;
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);Box cell=r.Cell(index);
                app.WardrobeBrowse.SetOffset(cell.Bottom-r.Area.Bottom+r.W*.025f,r.Maximum(cards.Count));Run(app,p,.1f);cell=cell.Offset(0,-app.WardrobeBrowse.Offset);
                int balance=app.Meta.Economy.Petals;
                Tap(p,cell);Run(app,p,.1f);
                Expect(app.WardrobeBrowse.PreviewItem?.Id==item.Id&&app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat==null,"unowned tap only fits");
                Tap(p,WardrobeShelfDesign.Action(cell));Run(app,p,.3f);
                Expect(app.IsOpen(Overlay.Purchase)&&app.Meta.Economy.Petals==balance,"green Petal button asks first");
                app.Back();Run(app,p,.1f);Expect(!app.Meta.Wardrobe.Owns(item),"cancel keeps item unowned");
                Tap(p,WardrobeShelfDesign.Action(cell));Run(app,p,.3f);Tap(p,ScreenLayout.PurchaseConfirm(p.Width,p.Height,p.Insets).Confirm);Run(app,p,.1f);
                Expect(app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat?.Id==item.Id&&app.Meta.Economy.Petals==balance-item.Price,"confirmation pays exact catalog price and equips");
                Tap(p,cell);Run(app,p,.1f);Expect(app.Meta.Economy.Petals==balance-item.Price,"owned tap never charges again");
                var restored=App(data);Expect(restored.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat?.Id==item.Id&&restored.Meta.Economy.Petals==balance-item.Price,"purchase and outfit survive disk reload");
                app.GoHome();Run(app,p,.1f);app.OpenWardrobe();Run(app,p,.1f);
            });
            foreach(var sample in new[]{(230,false),(231,true)})
            {
                var scene=sample;
                yield return new Fixture(scene.Item1,"settings-petals-"+(scene.Item2?"pause":"home"),"Petal controls persist without resetting progress or outfit",(p,data)=>
                {
                    var app=Progressed(App(data),content,39);CloseAll(app);
                    app.Meta.Wardrobe.Equip(Family.Sprig,SprigV2Art.RosetteId);
                    if(scene.Item2){app.LoadLevel(12);CloseDemo(app);Run(app,p,.1f);app.OpenOverlay(Overlay.Pause);}
                    app.OpenOverlay(Overlay.Settings);Run(app,p,.5f);
                    int level=app.Meta.CurrentLevel,initial=app.Meta.Economy.Petals;
                    Tap(p,p.Texts.First(t=>t.Text=="+1000").Box);Run(app,p,.1f);
                    Expect(app.Meta.Economy.Petals==initial+1000,"+1000 credits the live wallet");
                    Tap(p,p.Texts.First(t=>t.Text=="+100").Box);Run(app,p,.1f);
                    Tap(p,p.Texts.First(t=>t.Text=="−100").Box);Run(app,p,.1f);
                    Expect(app.Meta.Economy.Petals==initial+1000,"plus and minus are balanced");
                    Tap(p,p.Texts.Where(t=>t.Text=="Reset").OrderBy(t=>t.Box.CenterY).Last().Box);Run(app,p,.1f);
                    Tap(p,p.Texts.First(t=>t.Text=="−100").Box);Run(app,p,.1f);
                    Expect(app.Meta.Economy.Petals==0&&app.Meta.CurrentLevel==level,"wallet reset and subtract at zero leave level intact");
                    Expect(app.IsOpen(Overlay.Settings)&&app.Screen==(scene.Item2?Design.Screen.Level:Design.Screen.Home),"wallet buttons keep the current screen and Settings open");
                    Expect(app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat?.Id==SprigV2Art.RosetteId,"wallet changes preserve outfit");
                    Tap(p,p.Texts.First(t=>t.Text=="+1000").Box);Run(app,p,.1f);
                    var restored=App(data);Expect(restored.Meta.Economy.Petals==1000&&restored.Meta.CurrentLevel==level,"balance persists to disk");
                    Expect(Shows(p,PlaytestText.T("settings.petals_label")+" "+NumberText.Group(1000)),"label refreshes on the same card");
                });
            }
            yield return new Fixture(232,"wardrobe-release-locked-preview","Locked item can be tried on without purchase or saved changes",(p,data)=>
            {
                var app=Progressed(App(data),content,39);CloseAll(app);app.OpenWardrobe();Run(app,p,.2f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig).ToList();
                int index=cards.FindIndex(c=>c.State==WardrobeItemState.LevelLocked);var item=cards[index].Item!;int petals=app.Meta.Economy.Petals;
                Box cell=r.Cell(index);app.WardrobeBrowse.SetOffset(cell.Bottom-r.Area.Bottom+r.W*.025f,r.Maximum(cards.Count));Run(app,p,.1f);cell=cell.Offset(0,-app.WardrobeBrowse.Offset);
                Tap(p,cell);Run(app,p,.1f);
                Expect(app.WardrobeBrowse.ShownOutfit(app.Meta.Wardrobe).Hat?.Id==item.Id,"locked gear appears on the fitting stand");
                Tap(p,WardrobeShelfDesign.Action(cell));Run(app,p,.1f);
                Expect(!app.IsOpen(Overlay.Purchase)&&app.Meta.Economy.Petals==petals&&!app.Meta.Wardrobe.TryBuy(item.Id),"level lock has no active price button and cannot be bypassed");
                Expect(app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat==null,"fitting never persists");
                Expect(Shows(p,PlaytestText.F("wardrobe.unlock_level",item.UnlockLevel)),"lock explains its exact level");
            });
            yield return new Fixture(170,"wardrobe-fitting","Wardrobe: temporary fittings never leak into saved outfits",(p,data)=>
            {
                var app=Progressed(App(data),content,87);app.Meta.Economy.Grant(12000,null);CloseAll(app);app.OpenWardrobe();
                app.Meta.Save.Cosmetics.Owned.Add(SprigV2Art.BucketId);app.Meta.Wardrobe.Equip(Family.Sprig,SprigV2Art.BucketId);Run(app,p,.1f);
                var r=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                var cards=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig).ToList();
                int index=cards.FindIndex(c=>c.State==WardrobeItemState.ForSale);var item=cards[index].Item!;
                Box card=r.Cell(index);app.WardrobeBrowse.SetOffset(card.Bottom-r.Area.Bottom+r.W*.025f,r.Maximum(cards.Count));Run(app,p,.1f);
                void Fit(){Tap(p,card.Offset(0,-app.WardrobeBrowse.Offset));Run(app,p,.1f);}
                long balance=app.Meta.Economy.Petals;Fit();
                Expect(app.WardrobeBrowse.ShownOutfit(app.Meta.Wardrobe).Hat?.Id==item.Id,"stand shows the selected unowned item");
                Expect(app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat?.Id==SprigV2Art.BucketId,"saved outfit stays intact");
                Expect(app.Meta.Economy.Petals==balance&&!app.Meta.Wardrobe.Owns(item)&&app.Overlays.Count==0,"fitting spends nothing and opens no modal");
                Tap(p,NavigationSkin.Segment(r.Categories,1,4));Run(app,p,.1f);
                Expect(app.WardrobeBrowse.PreviewItem==null,"category switch ends the fitting");
                Tap(p,NavigationSkin.Segment(r.Categories,0,4));Run(app,p,.1f);Fit();
                Tap(p,NavigationSkin.Segment(r.Families,1,4));Run(app,p,.1f);
                Expect(app.WardrobeBrowse.PreviewItem==null,"family switch ends the fitting");
                Tap(p,NavigationSkin.Segment(r.Families,0,4));Run(app,p,.1f);Fit();
                app.OpenStore();app.CloseStore();Run(app,p,.1f);
                Expect(app.Screen==Design.Screen.Wardrobe&&app.WardrobeBrowse.PreviewItem==null,"leaving for Shop and returning restores the saved outfit");
                Fit();app.CloseWardrobe();app.OpenWardrobe();Run(app,p,.1f);
                Expect(app.WardrobeBrowse.PreviewItem==null&&app.Meta.Wardrobe.OutfitOf(Family.Sprig).Hat?.Id==SprigV2Art.BucketId,"reopening discards the fitting");
                Expect(!p.DrawnSprites.Contains(MetaScreenArt.Stage)&&!p.DrawnSprites.Contains(MetaScreenArt.Shelf),"new layout has no arch or shelves");
                Fit(); // Render the temporary preview state, including its label and green purchase button.
            });
            yield return new Fixture(27, "wardrobe", "Extra: Wardrobe (spec 005 FR-025)", (p, data) =>
            {
                // From the bottom menu's Wardrobe (FR-030); its Shop opens the Store page over it (whose back returns
                // here), its Leaderboard the Leaderboard page, whose Collection opens the Collection page. Then a tap on
                // the starter cap's card puts it on Sprig, so the hero and the green worn card show an outfit.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                Run(app, p, 0.1f);
                Expect(Nav(p, app).Places.Count == 5, "every place shows on a progressed Home");
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe, "the bottom menu's Wardrobe opens the Wardrobe");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Wardrobe, "the Wardrobe's Shop opens the Store page over it");
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Wardrobe, "the Store page's back returns to the Wardrobe");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Leaderboard && app.Overlays.Count == 0, "the Wardrobe's Leaderboard opens the Leaderboard page");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Collection && app.Overlays.Count == 0, "the Leaderboard page's Collection opens the Collection page");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe, "the Collection page's Wardrobe opens the Wardrobe again");
                Run(app, p, 0.1f);
                var shelf=WardrobeShelfCatalog.Build(app.Meta.Wardrobe,CosmeticKind.Hat,Family.Sprig);
                int starter=Enumerable.Range(0,shelf.Count).First(i=>shelf[i].Item?.Id=="hat.sprout_cap");
                var shelfPage=WardrobeShelfDesign.Page(p.Width,p.Height,p.Insets);
                float reveal=Math.Max(0,shelfPage.Cell(starter).Bottom-shelfPage.Area.Bottom)+shelfPage.W*.03f;
                Drag(p,shelfPage.Area.CenterX,shelfPage.Area.CenterY,shelfPage.Area.CenterX,shelfPage.Area.CenterY-reveal);Run(app,p,.1f);
                Tap(p, shelfPage.Cell(starter).Offset(0,-app.WardrobeBrowse.Offset));
                Expect(app.Meta.Wardrobe.EquippedFor(Family.Sprig, Client.Meta.Wardrobe.CosmeticKind.Hat)?.Id == "hat.sprout_cap", "a tap on an owned item's card wears it");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(28, "home-outfits", "Extra: Home's animated heroes in outfits (spec 005 FR-028)", (p, data) =>
            {
                // The progressed Home with an outfit on every hero (a hat each, Sprig's speckles, Bloom's wink, Twig's
                // sparkle trail), so the frames' outfit layers show: the hats turn with the heads and the skin keeps to
                // each frame. A tap on Play still plays; a tap on Twig's body makes it react at once, shown mid-reaction.
                DesignApp app = Progressed(App(data), content, 87);
                CloseAll(app);
                foreach (string id in new[] { "hat.sprout_cap", "hat.straw_hat", "hat.flower_crown", "hat.acorn_cap", "skin.speckles", "trail.petal_sparkle", "expression.wink" })
                {
                    app.Meta.Save.Cosmetics.Owned.Add(id);
                }

                foreach ((Family family, string id) in new[]
                {
                    (Family.Sprig, "hat.sprout_cap"), (Family.Bloom, "hat.flower_crown"), (Family.Drop, "hat.straw_hat"), (Family.Twig, "hat.acorn_cap"),
                    (Family.Sprig, "skin.speckles"), (Family.Twig, "trail.petal_sparkle"), (Family.Bloom, "expression.wink"),
                })
                {
                    Expect(app.Meta.Wardrobe.Equip(family, id), "an owned " + id + " can be worn by " + family);
                }

                Run(app, p, 0.5f);
                Tap(p, ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Play);
                Expect(app.Screen == Design.Screen.Level, "a tap on Play plays, whatever hero stands behind it");
                app.GoHome();
                CloseAll(app);
                Run(app, p, 0.5f);
                Box picture = HomeLayers.Stage(new Box(0f, 0f, p.Width, p.Height));
                Box twig = HeroMotion.PictureBox(HomeLayers.HeroCell(picture, Family.Twig), HeroMotion.Frame(Family.Twig, MotionClip.Idle, 0));
                Expect(!app.HomeMotion.Player(Family.Twig).Busy(app.Now), "Twig idles before the tap");
                p.Dispatch(twig.CenterX, twig.Top + (twig.Height * 0.7f));
                Expect(app.HomeMotion.Player(Family.Twig).Busy(app.Now), "a tap on Twig makes it react at once");
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "a tap on a hero opens nothing");
                Run(app, p, 0.6f);
            });
            yield return new Fixture(29, "store-locked", "Extra: locked Store page (spec 005 FR-030)", (p, data) =>
            {
                // Level 5: the bottom menu's Shop, locked, still opens the Store page, which says from which level it is
                // available (the roadmap's L12), the Shop raised in the medallion; its back returns to Home.
                DesignApp app = Early(App(data), content, 4);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Home, "the locked Shop opens the Store page");
                Expect(app.UnlockLevel(NavPlace.Shop) == 12, "the Store opens from the roadmap's level 12");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Available from level 12"), "the locked Store page says its level");
                Expect(Nav(p, app).Active == NavPlace.Shop, "the locked Store page raises the Shop");
                Tap(p, ScreenLayout.LockedPage(p.Width, p.Height, p.Insets).Header.Back);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 0, "the locked Store page's back returns to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store, "the locked Shop opens the Store page again");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(30, "wardrobe-locked", "Extra: locked Wardrobe (spec 005 FR-030)", (p, data) =>
            {
                // Level 15: the Store, the Leaderboard and the Collection are open, the Wardrobe not yet (L40). The menu's
                // Wardrobe opens the Wardrobe locked, saying its level; its Shop opens the open Store page over it, whose
                // back returns here, and so does its Petals "+".
                DesignApp app = Progressed(App(data), content, 14);
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Wardrobe));
                Expect(app.Screen == Design.Screen.Wardrobe && !app.PlaceOpen(NavPlace.Wardrobe), "the locked Wardrobe place opens the Wardrobe");
                Expect(app.UnlockLevel(NavPlace.Wardrobe) == 40, "the Wardrobe opens from the roadmap's level 40");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Available from level 40"), "the locked Wardrobe says its level");
                Tap(p, NavTouch(p, app, NavPlace.Shop));
                Expect(app.Screen == Design.Screen.Store && app.StoreReturn == Design.Screen.Wardrobe, "the locked Wardrobe's Shop opens the Store page over it");
                Run(app, p, 0.1f);
                Expect(!Shows(p, "Available from level 12"), "the Store page is open at Level 15");
                Tap(p, ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Back);
                Expect(app.Screen == Design.Screen.Wardrobe, "the Store page's back returns to the locked Wardrobe");
                Run(app, p, 0.1f);
                Box petals = ScreenLayout.LockedPage(p.Width, p.Height, p.Insets).Header.Petals;
                Tap(p, Box.FromCenter(petals.Right - petals.Height, petals.CenterY, 1f, 1f));
                Expect(app.Screen == Design.Screen.Store, "the locked Wardrobe's Petals \"+\" opens the Store page");
                Expect(app.Back() && app.Screen == Design.Screen.Wardrobe, "the system back returns to the locked Wardrobe");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(31, "leaderboard-locked", "Extra: locked Leaderboard page (spec 005 FR-030)", (p, data) =>
            {
                // A new profile on Home (Level 1): the menu's Collection opens its page saying it is available from level 2
                // (its first picture comes with Level 1's win), its back returning to Home; the Leaderboard's page says
                // level 10 (the roadmap's), the Leaderboard raised, and the system back returns to Home.
                DesignApp app = App(data);
                app.GoHome();
                CloseAll(app);
                Run(app, p, 0.1f);
                Expect(app.Meta.CurrentLevel == 1 && !app.PlaceOpen(NavPlace.Collection), "a new profile's Collection is locked");
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Expect(app.Screen == Design.Screen.Collection && app.Overlays.Count == 0, "the locked Collection opens its page");
                Run(app, p, 0.5f);
                Expect(Shows(p, "Available from level 2"), "the locked Collection page says level 2");
                Expect(Nav(p, app).Active == NavPlace.Collection, "the locked Collection page raises the Collection");
                Tap(p, ScreenLayout.LockedPage(p.Width, p.Height, p.Insets).Header.Back);
                Expect(app.Screen == Design.Screen.Home, "the locked Collection page's back returns to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Leaderboard && app.Overlays.Count == 0, "the locked Leaderboard opens its page");
                Expect(app.UnlockLevel(NavPlace.Leaderboard) == 10, "the Leaderboard opens from the roadmap's level 10");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Available from level 10"), "the locked Leaderboard page says level 10");
                Expect(app.Back() && app.Screen == Design.Screen.Home, "the system back returns from the locked Leaderboard page to Home");
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Collection));
                Run(app, p, 0.1f);
                Tap(p, NavTouch(p, app, NavPlace.Leaderboard));
                Expect(app.Screen == Design.Screen.Leaderboard, "the locked Collection page's Leaderboard opens the locked Leaderboard page");
                Run(app, p, 0.5f);
            });
            yield return new Fixture(32, "remove-ads", "Extra: Remove Ads card (spec 005 FR-033)", (p, data) =>
            {
                // Level 15, past the Store's unlock: a tap on Home's No Ads scene still opens the Remove Ads card, not the
                // Store page (the owner: "a separate Remove Ads popup; it is always there"). Purchases are off in the
                // playtest: its button reads "Unavailable" over the offline line, and Restore says purchases are offline
                // under the card. The system back closes it; the frame shows it open again.
                DesignApp app = Progressed(App(data), content, 14);
                CloseAll(app);
                Run(app, p, 0.1f);
                Expect(app.PlaceOpen(NavPlace.Shop), "the Store is open at Level 15");
                Box noAds = ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).NoAds;
                Tap(p, noAds);
                Expect(app.Screen == Design.Screen.Home && app.Overlays.Count == 1 && app.IsOpen(Overlay.RemoveAds), "a tap on the No Ads scene opens the Remove Ads card");
                Run(app, p, 0.5f);
                string offline = PlaytestText.T("store.offline");
                Expect(Shows(p, PlaytestText.T("remove_ads.title")) && Shows(p, PlaytestText.T("store.unavailable")) && Shows(p, offline), "the card says purchases are unavailable");
                Expect(!Shows(p,PlaytestText.T("remove_ads.restore")),"Restore Purchases is absent from the No Ads window");
                Expect(app.Back() && app.Overlays.Count == 0 && app.Screen == Design.Screen.Home, "the system back closes the Remove Ads card");
                Run(app, p, 0.1f);
                Tap(p, noAds);
                Expect(app.IsOpen(Overlay.RemoveAds), "the No Ads scene opens the card again");
                Run(app, p, 0.5f);
            });
        }

        /// <summary>
        /// The profile page and its edit card (spec 005 FR-037): the page at Level 15 with a bought avatar filling its disc
        /// in a free frame picked before the Wardrobe opens (39), the card's avatars with a purchase and short Petals (40),
        /// and at Level 45 a new name and the five free frames with the owned ones (41; the owner, 2026-10-06).
        /// </summary>
        public static IEnumerable<Fixture> Profile(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false) { TextPrompt = new FixedPrompt("Rosie") };
            Box HomeAvatar(SkiaPainter p) => ScreenLayout.ReferenceHome(p.Width, p.Height, p.Insets, HomeScreen.DevReserve(p)).Avatar;

            yield return new Fixture(39, "profile", "Extra: profile page (spec 005 FR-037)", (p, data) =>
            {
                // Level 15 with 1240 Petals and Drop's sailor avatar bought: Home's avatar opens the page, which shows it in
                // the card with the default name, the ID, the joining month, the Level plaque, the stats and the
                // achievements on their way to bronze. The Wardrobe is still closed, yet the card's Frame tab lists the five
                // free frames (the owner, 2026-10-06): the Wooden Frame, the buttons' wood since spec 005 FR-047, is picked and
                // saved, and the page shows it with the references' flowers over two corners.
                DesignApp app = Progressed(App(data), content, 14);
                CloseAll(app);
                Expect(app.Meta.Profile.TryBuy("avatar.drop_sailor_sticker"), "an avatar for 300 Petals");
                Run(app, p, 0.1f);
                Tap(p, HomeAvatar(p));
                Expect(app.Screen == Design.Screen.Profile, "Home's avatar opens the profile page");
                Run(app, p, 0.4f);
                Expect(p.Slots.Contains(OwnerPictures.AvatarSlot) && !p.Slots.Contains("cosmetic.frame"), "the avatar's picture, no frame until one is picked");
                Tap(p, ScreenLayout.ReferenceProfile(p.Width, p.Height, p.Insets).Avatar);
                Run(app, p, 0.5f);
                ProfileGameEditRegions edit = ProfileGameDesign.Edit(p.Width, p.Height, p.Insets);
                Tap(p, Box.FromCenter(edit.Tabs.Left + (edit.Tabs.Width * 1.5f / ProfileEditor.Tabs.Count), edit.Tabs.CenterY, 1f, 1f));
                Run(app, p, 0.1f);
                ProfileEditor editor = app.ProfileEditor!;
                Expect(editor.Tab == ProfileTab.Frame && !editor.ProfileItemsOpen && !editor.IsLocked(Client.Meta.Wardrobe.CosmeticKind.Frame), "the Frame tab is open before the Wardrobe");
                PickProfileCell(app,p,4,frames:true);Run(app,p,.1f);
                Expect(editor.Owned(Client.Meta.Wardrobe.CosmeticKind.Frame).Count == 5 && ProfileFrames.All.All(s => p.Slots.Contains(ProfileFrames.Slot(s))), "the five free frames, each drawn");
                app.ProfileScrollOffset=0;Run(app,p,.1f);
                Tap(p, edit.Cell(0,true));
                Run(app, p, 0.1f);
                Expect(editor.FrameId == "frame.wood_ring", "the Wooden Frame picked");
                Tap(p, edit.Button);
                Expect(!app.IsOpen(Overlay.ProfileEdit) && app.Meta.Wardrobe.Profile.Frame?.Id == "frame.wood_ring", "Save shows it on the profile at Level 15");
                Run(app, p, 0.4f);
                ProfileService profile = app.Meta.Profile;
                Expect(Shows(p, PlaytestText.F("profile.default_name", profile.DefaultNumber)), "the default name");
                Expect(Shows(p, PlaytestText.F("profile.id", profile.ShortId)) && Shows(p, PlaytestText.F("profile.joined", profile.JoinedMonth)), "the ID and the joining month");
                Expect(Shows(p, PlaytestText.F("common.level", 15)), "the Level plaque");
                Expect(Achievements.All.Take(3).All(a => Shows(p, PlaytestText.T(a.NameKey))), "the first achievement page by name");
                Expect(Shows(p, PlaytestText.F("profile.achievement_progress", 0, 1)), "mastery starts with new winning attempts");
                Expect(p.Slots.Contains(OwnerPictures.AvatarSlot) && p.Slots.Contains("ui.achievement"), "the avatar picture and the achievement tiles");
                Expect(p.Slots.Contains("cosmetic.frame.wood_ring"), "the page preserves the selected illustrated Wooden Frame");
            });
            yield return new Fixture(40, "profile-edit", "Extra: profile edit card, avatars (spec 005 FR-037)", (p, data) =>
            {
                // The avatar's tap opens the card on Avatar: the free four, then the ten with their prices. A 600 one is
                // bought (640 Petals left), a 1200 one is too dear: its button says "Buy for 1 200" and Petals are short.
                DesignApp app = Progressed(App(data), content, 14);
                CloseAll(app);
                Run(app, p, 0.1f);
                Tap(p, HomeAvatar(p));
                Run(app, p, 0.1f);
                Tap(p, ScreenLayout.ReferenceProfile(p.Width, p.Height, p.Insets).Avatar);
                Expect(app.IsOpen(Overlay.ProfileEdit) && app.ProfileEditor!.Tab == ProfileTab.Avatar, "the avatar opens the card on Avatar");
                Run(app, p, 0.5f);
                ProfileGameEditRegions r = ProfileGameDesign.Edit(p.Width, p.Height, p.Insets);
                PickProfileCell(app,p,9);
                Run(app, p, 0.1f);
                Expect(Shows(p, PlaytestText.F("profile.buy", NumberText.Group(600))), "a 600 avatar to buy");
                Tap(p, r.Button);
                Expect(app.IsOpen(Overlay.Purchase) && app.Meta.Economy.Petals == 1240, "Buy asks the purchase confirmation first, nothing spent (FR-040)");
                Run(app, p, 0.5f);
                Tap(p, ScreenLayout.PurchaseConfirm(p.Width, p.Height, p.Insets).Confirm);
                Expect(!app.IsOpen(Overlay.Purchase) && app.IsOpen(Overlay.ProfileEdit), "the confirmation's Buy closes it over the edit card");
                Expect(app.Meta.Economy.Petals == 640 && app.Meta.Profile.Avatar.Id == "avatar.sprig_flower_crown_3d", "bought and shown");
                Run(app, p, 0.1f);
                Expect(Shows(p, PlaytestText.T("profile.bought")) && Shows(p, PlaytestText.T("profile.save")), "the card stays, its button now Save");
                PickProfileCell(app,p,13);
                Run(app, p, 0.1f);
                Tap(p, r.Button);
                Expect(app.HomeToastText == PlaytestText.T("gameplay.not_enough_petals") && app.Meta.Economy.Petals == 640, "Petals short for 1 200");
                Run(app, p, 1.8f);
                Expect(Shows(p, PlaytestText.F("profile.buy", NumberText.Group(1200))), "the button keeps the price");
            });
            yield return new Fixture(41, "profile-frames", "Extra: profile edit card, name and frames (spec 005 FR-037)", (p, data) =>
            {
                // Level 45, the Wardrobe open: the pencil opens the card on Name, "Change name" asks the host (here "Rosie")
                // and Save keeps it; the card again on Frame lists the five free frames, then the owned ones, on the avatar;
                // the shown one (the newest owned, Ivy) is checked until a tap picks the Leaf Frame.
                DesignApp app = Progressed(App(data), content, 44);
                CloseAll(app);
                // Two frames and a badge as later milestones give them.
                foreach (string item in new[] { "frame.daisy", "frame.ivy", "badge.sprout" })
                {
                    app.Meta.Save.Cosmetics.Owned.Add(item);
                }

                Run(app, p, 0.1f);
                Tap(p, HomeAvatar(p));
                Run(app, p, 0.1f);
                ReferenceProfileRegions page = ScreenLayout.ReferenceProfile(p.Width, p.Height, p.Insets);
                Tap(p, page.Edit);
                Expect(app.IsOpen(Overlay.ProfileEdit) && app.ProfileEditor!.Tab == ProfileTab.Name, "the pencil opens the card on Name");
                Run(app, p, 0.5f);
                ProfileGameEditRegions r = ProfileGameDesign.Edit(p.Width, p.Height, p.Insets);
                Tap(p, r.NameField);
                Run(app, p, 0.1f);
                Expect(app.Meta.Profile.Name == null && Shows(p, "Rosie"), "the typed name waits for Save");
                Tap(p, r.Button);
                Expect(!app.IsOpen(Overlay.ProfileEdit) && app.Meta.Profile.Name == "Rosie", "Save keeps the name and closes the card");
                Run(app, p, 0.1f);
                Expect(Shows(p, "Rosie"), "the page shows the name");
                Tap(p, page.Avatar);
                Run(app, p, 0.1f);
                Tap(p, Box.FromCenter(r.Tabs.Left + (r.Tabs.Width * 1.5f / ProfileEditor.Tabs.Count), r.Tabs.CenterY, 1f, 1f));
                Expect(app.ProfileEditor!.Tab == ProfileTab.Frame, "the Frame tab");
                Run(app, p, 0.5f);
                ProfileEditor editor = app.ProfileEditor;
                Expect(editor.ProfileItemsOpen && editor.Owned(Client.Meta.Wardrobe.CosmeticKind.Frame).Count == 7, "the five free frames and the two owned ones at Level 45");
                Expect(editor.FrameId == "frame.ivy", "the newest owned frame is shown by default, never a free one");
                Expect(p.Slots.Contains("cosmetic.frame"), "visible frame cards and the fixed profile preview");
                Tap(p, r.Cell(1,true));
                Run(app, p, 0.2f);
                Expect(editor.FrameId == "frame.leaf_ring", "a tap picks the Leaf Frame");
            });
        }

        public static IEnumerable<Fixture> ProfileGame(ContentSet content)
        {
            DesignApp Ready(string data)
            {
                var app=Progressed(new DesignApp(data,content,new Silence(),false){TextPrompt=new FixedPrompt("Rosie")},content,44);CloseAll(app);
                foreach(string id in new[]{"frame.daisy","frame.ivy","badge.sprout","badge.watering_can"})app.Meta.Save.Cosmetics.Owned.Add(id);
                app.Meta.Profile.Rename("Rosie");app.Meta.Profile.Choose(AvatarCatalog.DefaultId);app.Meta.Wardrobe.Show("frame.leaf_ring");app.Meta.Wardrobe.Show("badge.sprout");app.OpenProfile();return app;
            }
            yield return new Fixture(57,"profile-game-avatar","Game profile: Avatar, unchanged shared summary",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Avatar);Run(app,p,.2f);
                var editor=app.ProfileEditor!;string avatar=editor.AvatarId,frame=editor.FrameId!,badge=editor.BadgeId!,name=editor.Name!;
                foreach(var tab in ProfileEditor.Tabs){editor.Tab=tab;Run(app,p,.1f);Expect(editor.AvatarId==avatar&&editor.FrameId==frame&&editor.BadgeId==badge&&editor.Name==name,"switching tabs preserves the exact pending profile");}
                editor.Tab=ProfileTab.Avatar;Run(app,p,.1f);
                var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);int petals=app.Meta.Economy.Petals;
                Drag(p,r.Grid.CenterX,r.Grid.CenterY,r.Grid.CenterX,r.Grid.CenterY-180);Run(app,p,.1f);
                Expect(app.ProfileScrollOffset>0&&app.Overlays.Count==1&&app.Meta.Economy.Petals==petals,"drag never buys an avatar");
                Drag(p,r.Grid.CenterX,r.Grid.CenterY,r.Grid.CenterX,r.Grid.CenterY+6000);Run(app,p,.1f);Expect(app.ProfileScrollOffset==0,"catalog returns to initial viewport");
                Tap(p,NavigationSkin.Segment(r.Tabs,3,4));Run(app,p,.1f);Expect(app.ProfileEditor!.Tab==ProfileTab.Name,"fixed tabs still respond after a scroll");
                Tap(p,r.NameField);Run(app,p,.1f);Tap(p,NavigationSkin.Segment(r.Tabs,0,4));Run(app,p,.1f);
            });
            yield return new Fixture(58,"profile-game-badge","Game profile: compact owned badge selector",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Badge);Run(app,p,.1f);var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);
                int petals=app.Meta.Economy.Petals;Tap(p,r.Cell(0));Run(app,p,.1f);Expect(app.ProfileEditor!.BadgeId==null&&app.Meta.Wardrobe.Profile.Badge!=null,"No badge is pending until Save");
                Tap(p,r.Button);Run(app,p,.1f);Expect(app.Meta.Wardrobe.Profile.Badge==null&&app.Meta.Economy.Petals==petals,"No badge saves without spending");
                app.OpenProfileEdit(ProfileTab.Badge);Run(app,p,.1f);
                int sprout=app.ProfileEditor!.Owned(Client.Meta.Wardrobe.CosmeticKind.Badge).ToList().FindIndex(i=>i.Id=="badge.sprout")+1;PickProfileCell(app,p,sprout);Run(app,p,.1f);
                Tap(p,r.Button);Run(app,p,.1f);Expect(app.Meta.Wardrobe.Profile.Badge!.Id=="badge.sprout","selected badge saved");app.OpenProfileEdit(ProfileTab.Badge);Run(app,p,.1f);
            });
            yield return new Fixture(59,"profile-game-name","Game profile: direct name field",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Name);Run(app,p,.1f);var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);
                Expect(!app.ProfileEditor!.EnterName(" !!! "),"invalid names remain rejected");Tap(p,r.NameField);Run(app,p,.1f);
                Expect(app.ProfileEditor.Name=="Rosie","the field opens the native host prompt");
                Tap(p,r.Button);Run(app,p,.1f);Expect(app.Meta.Profile.Name=="Rosie"&&!app.IsOpen(Overlay.ProfileEdit),"Save keeps the name and returns to Profile");app.OpenProfileEdit(ProfileTab.Name);Run(app,p,.1f);
            });
            yield return new Fixture(60,"profile-game","Game profile: approved landing composition",(p,data)=>{var app=Ready(data);Run(app,p,.1f);});
            yield return new Fixture(61,"profile-game-avatar-buy","Game profile: purchase button and cancellation",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Avatar);Run(app,p,.1f);var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);
                Box card=r.Cell(4);app.ProfileScrollOffset=Math.Max(0,card.Bottom-r.Grid.Bottom+r.W*.025f);Run(app,p,.1f);card=card.Offset(0,-app.ProfileScrollOffset);
                int petals=app.Meta.Economy.Petals;Tap(p,ProfileGameDesign.CellPrice(card));Run(app,p,.2f);
                Expect(app.IsOpen(Overlay.Purchase)&&app.Meta.Economy.Petals==petals,"green card price asks before buying");app.Back();Run(app,p,.1f);
                Expect(app.Meta.Economy.Petals==petals&&!app.Meta.Profile.Owns(app.Meta.Profile.Avatars[4]),"Cancel buys nothing");
                Tap(p,ProfileGameDesign.CellPrice(card));Run(app,p,.3f);
            });
            yield return new Fixture(62,"profile-game-badges-scroll","Game profile: all earned badges remain reachable",(p,data)=>
            {
                var app=Ready(data);app.Meta.Save.Progression.HighestCompletedLevel=5000;
                foreach(var def in Achievements.All)if(def.Kind==AchievementKind.WinFact)app.Meta.Save.Stats.Groups[Achievements.FactPrefix+def.Id]=new SortedDictionary<string,long>{{"fixture",1000}};
                Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Badge);Run(app,p,.1f);var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);int count=app.ProfileEditor!.Owned(Client.Meta.Wardrobe.CosmeticKind.Badge).Count+1;
                Expect(count>14,"more than the old fixed grid's capacity");PickProfileCell(app,p,count-1);Run(app,p,.1f);Expect(app.ProfileScrollOffset>0&&app.ProfileEditor.BadgeId!=null,"final earned badge remains selectable after scrolling");
                string shown=app.Meta.Wardrobe.Profile.Badge!.Id;Expect(app.Back()&&app.Meta.Wardrobe.Profile.Badge!.Id==shown,"back discards pending edits");
                app.OpenProfileEdit(ProfileTab.Badge);Run(app,p,.1f);PickProfileCell(app,p,count-1);Run(app,p,.1f);
            });
            yield return new Fixture(63,"profile-game-achievements-last","Game profile: all achievement pages",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);var r=ScreenLayout.ReferenceProfile(p.Width,p.Height,p.Insets);Box next=Box.FromCenter(r.AchievementsTitle.Right-r.AchievementsTitle.Height*.5f,r.AchievementsTitle.CenterY,r.AchievementsTitle.Height,r.AchievementsTitle.Height);
                Tap(p,next);Run(app,p,.1f);Tap(p,next);Run(app,p,.1f);Expect(app.ProfileAchievementPage==2&&Shows(p,PlaytestText.T("profile.achievement.space_garden")),"the final achievement page remains reachable");
            });
            yield return new Fixture(65,"profile-game-frame","Game profile: Frame with the same summary as every other tab",(p,data)=>{var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Frame);Run(app,p,.1f);});
            yield return new Fixture(64,"profile-game-store-return","Game profile: shopping retains the edit draft",(p,data)=>
            {
                var app=Ready(data);Run(app,p,.1f);app.OpenProfileEdit(ProfileTab.Frame);Run(app,p,.1f);var editor=app.ProfileEditor!;editor.PickFrame("frame.ivy");
                app.OpenStoreFromProfileEdit();Run(app,p,.1f);Expect(app.Screen==Design.Screen.Store,"plus opens Shop");app.CloseStore();Run(app,p,.1f);
                Expect(app.ProfileEditor==editor&&editor.FrameId=="frame.ivy"&&editor.Tab==ProfileTab.Frame,"return from Shop retains draft and tab");
            });
        }

        public static IEnumerable<Fixture> NavigationControls(ContentSet content)
        {
            DesignApp Ready(string data)
            {
                var app=Progressed(new DesignApp(data,content,new Silence(),false),content,87);CloseAll(app);
                app.Meta.Profile.Rename("Rosie");app.Meta.Profile.Choose(AvatarCatalog.DefaultId);app.Meta.Save.Cosmetics.Owned.Add("badge.watering_can");app.Meta.Save.Cosmetics.Owned.Add("badge.sprout");return app;
            }
            var examples=new[]{(Number:66,Slug:"home-controls-wood",Frame:"frame.wood_ring",Badge:(string?)null),(Number:67,Slug:"home-controls-leaf",Frame:"frame.leaf_ring",Badge:(string?)"badge.sprout"),(Number:68,Slug:"home-controls-gold",Frame:"frame.golden_ribbon",Badge:(string?)"badge.watering_can")};
            foreach(var example in examples)
            {
                var item=example;
                yield return new Fixture(item.Number,item.Slug,"Illustrated navigation: live frame and badge",(p,data)=>
                {
                    var app=Ready(data);app.Meta.Wardrobe.Show(item.Frame);
                    if(item.Badge==null)app.Meta.Wardrobe.HideProfile(Client.Meta.Wardrobe.CosmeticKind.Badge);else app.Meta.Wardrobe.Show(item.Badge);
                    Run(app,p,.2f);Expect(app.Meta.Wardrobe.Profile.Frame?.Id==item.Frame&&app.Meta.Wardrobe.Profile.Badge?.Id==item.Badge,"Home uses the selected live cosmetics");
                    var r=ScreenLayout.ReferenceHome(p.Width,p.Height,p.Insets);Tap(p,GameNavigationDesign.ProfileBounds(r.Avatar));Run(app,p,.1f);
                    Expect(app.Screen==Design.Screen.Profile&&Shows(p,"Rosie"),"the full profile opens from the portrait");
                    app.Back();RunTo(app,p,HomePromo.DailyAt+1.6f);Expect(!Shows(p,"Rosie"),"Home omits the player name beneath the live portrait");
                });
            }
            yield return new Fixture(69,"shop-wallet-large","Illustrated wallet: grouped six-digit balance",(p,data)=>
            {
                var app=Ready(data);app.Meta.Save.Wallet.AddPetals(120000-app.Meta.Economy.Petals);Run(app,p,1.5f);app.OpenStore();Run(app,p,1.5f);
                Expect(Shows(p,NumberText.Group(120000)),"the small page wallet shows a large live balance");
            });
            yield return new Fixture(71,"shop-scroll-behind-menu","Shop content continues behind the opaque wooden menu",(p,data)=>
            {
                var app=Ready(data);app.OpenStore();Run(app,p,.3f);
                var r=ScreenLayout.ReferenceStore(p.Width,p.Height,p.Insets);Box area=ShopBodyDesign.Bounds(r);
                var nav=HomeScreen.Nav(p,NavPlace.Shop);Box face=GameNavigationDesign.NavButton(nav,0),wall=GameNavigationDesign.NavBacking(nav,p.Width);
                Expect(p.ScrollAt(area.CenterX,area.CenterY)>=0,"catalog receives vertical drags");
                Expect(p.ScrollAt(face.CenterX,face.CenterY)<0,"selected menu tile blocks catalog drags");
                Expect(p.ScrollAt(face.CenterX,face.Top+nav.W*.006f)<0,"raised selected inset blocks catalog drags above the console");
                var captions=p.Texts.Where(t=>t.Box.Top>=nav.Top-nav.W*.025f&&BottomNav.Order.Any(place=>t.Text==PlaytestText.T(GameNavigationDesign.NameKey(place)))).ToList();
                Expect(captions.Count==1&&captions[0].Text==PlaytestText.T(GameNavigationDesign.NameKey(NavPlace.Shop)),"only the selected destination has a caption");
                Expect(p.ScrollAt(area.CenterX,wall.Top+nav.W*.005f)<0,"console top lip blocks catalog drags");
                Box backing=GameNavigationDesign.NavBacking(nav,p.Width);
                Expect(p.ScrollAt(nav.Safe.Left+nav.W*.01f,backing.Top+nav.W*.02f)<0,"opaque console shields gaps and side edges");
                for(int i=1;i<nav.Places.Count;i++)Expect(p.ScrollAt(nav.PlaceBoxes[i].Left,backing.Top+nav.W*.06f)<0,"dividers block catalog drags");
                Expect(p.ScrollAt(backing.CenterX,backing.Bottom-nav.W*.005f)<0,"bottom inset blocks catalog drags");
                Expect(p.ScrollAt(r.Tabs.CenterX,r.Tabs.CenterY)<0,"fixed top tabs shield catalog drag starts");
                float before=app.ShopScrollOffset;Drag(p,face.CenterX,face.CenterY,face.CenterX,face.CenterY-nav.W*.1f);Run(app,p,.1f);
                Expect(app.ShopScrollOffset==before&&app.Screen==Design.Screen.Store,"dragging selected navigation does not scroll or purchase");
                Box home=GameNavigationDesign.NavTouch(nav,2);
                Drag(p,home.CenterX,home.CenterY,home.CenterX,home.CenterY-nav.W*.1f);Run(app,p,.1f);
                Expect(app.ShopScrollOffset==before&&app.Screen==Design.Screen.Store,"dragging another destination neither navigates nor scrolls");
                Drag(p,area.CenterX,area.CenterY,area.CenterX,area.CenterY-nav.W*.35f);Run(app,p,.1f);
                Expect(app.ShopScrollOffset>before,"catalog scrolls under the fixed menu");
                app.ShopScrollOffset=nav.W*.30f;Run(app,p,.1f);
            });
            yield return new Fixture(70,"gameplay-controls-fast","Illustrated gameplay controls: enabled speed",(p,data)=>
            {
                var app=Ready(data);app.Meta.Save.Settings.Speed2x=true;app.LoadLevel(12);CloseDemo(app);Run(app,p,.3f);
                Expect(app.Level!.Animator.Speed>1f&&Shows(p,"2x"),"speed material follows the actual speed state");
            });
        }
        private static void PickProfileCell(DesignApp app,SkiaPainter p,int index,bool frames=false)
        {
            var r=ProfileGameDesign.Edit(p.Width,p.Height,p.Insets);Box cell=r.Cell(index,frames).Offset(0,-app.ProfileScrollOffset);
            if(cell.Bottom>r.Grid.Bottom)
            {
                Drag(p,r.Grid.CenterX,r.Grid.CenterY,r.Grid.CenterX,r.Grid.CenterY-(cell.Bottom-r.Grid.Bottom)-r.W*.015f);Run(app,p,.1f);
            }
            cell=r.Cell(index,frames).Offset(0,-app.ProfileScrollOffset);Tap(p,ProfileGameDesign.Picture(cell,frames));
        }

        /// <summary>
        /// The Store's Animations tab (spec 005 FR-038): the free style's card (Munchers) and the six bought clearing styles (Blossom first), each with
        /// its live preview; at Level 45 Fireflies is bought and chosen, the others show their price; a tap buys one.
        /// </summary>
        public static IEnumerable<Fixture> Clearing(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);
            yield return new Fixture(42, "store-animations", "Extra: the Store's Animations, the clearing styles (spec 005 FR-038)", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 44);
                app.Meta.Economy.Grant(12000, null);
                Expect(app.Meta.Clearing.Tap(ClearStyle.Fireflies, app.Meta.CurrentLevel) == ClearingTap.Bought, "Fireflies bought at Level 45");
                Expect(app.Meta.Clearing.StyleFor(46) == ClearStyle.Fireflies, "the chosen style plays on every level");
                CloseAll(app);
                app.OpenStore();
                Run(app, p, 0.1f);
                Box tabs = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Tabs;
                Tap(p, Box.FromCenter(tabs.Right - (tabs.Width / 4f), tabs.CenterY, 1f, 1f));
                Expect(app.StoreTab == 1, "a tap on the Animations tab shows the clearing styles");
                Run(app, p, 6f);
                Expect(p.Slots.Contains("ui.card.clearing") && p.Slots.Contains("fx.clear.fireflies"), "the cards' live previews");
            });

            yield return new Fixture(43, "store-animations-locked", "Extra: the Store's Animations before Level 40, the previews bright under their padlocks (spec 005 FR-038)", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 19);
                app.Meta.Economy.Grant(12000, null);
                CloseAll(app);
                app.OpenStore();
                Run(app, p, 0.1f);

                // Free Sprig accessories add Cosmetics before L40; paid animations retain their lock.
                Box tabs = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Tabs;
                Tap(p, Box.FromCenter(tabs.Right - (tabs.Width / 4f), tabs.CenterY, 1f, 1f));
                Expect(app.StoreTab == 1, "a tap on the Animations tab shows the clearing styles");
                Run(app, p, 0.1f);
                ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
                Box bubbles = ShopCatalogDesign.AnimationCard(ShopCatalogDesign.Viewport(r,false),ClearStyles.Free.Count+ClearStyles.Bought.ToList().IndexOf(ClearStyle.Bubbles));
                int petals = app.Meta.Economy.Petals;
                Tap(p, Box.FromCenter(bubbles.CenterX, bubbles.CenterY, 1f, 1f));
                Expect(!app.Meta.Clearing.Owns(ClearStyle.Bubbles) && app.Meta.Economy.Petals == petals, "a style is not bought before Level 40");
                Run(app, p, 4f);
                Expect(p.Slots.Contains("ui.lock") && p.Slots.Contains("fx.clear.bubbles"), "the padlocks over the live previews");
            });
        }

        /// <summary>
        /// The purchase confirmation (spec 005 FR-040) and the Animations tab's buttons (FR-038 as amended on 2026-10-06):
        /// at Level 45 a booster used without charges asks first, Cancel and the system back buy nothing, Buy buys; on the
        /// Store's Animations a style's Buy asks, its Buy buys and chooses it ("Chosen"), the free style's card then reads
        /// "Choose"; the frame shows the confirmation for Pushers over the tab, its live preview in the well.
        /// </summary>
        public static IEnumerable<Fixture> Purchases(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);
            yield return new Fixture(49, "purchase-confirm", "Extra: the purchase confirmation over the Store's Animations (spec 005 FR-040)", (p, data) =>
            {
                DesignApp app = Progressed(App(data), content, 44);
                app.Meta.Economy.Grant(12000, null);
                CloseAll(app);
                PurchaseConfirmRegions confirm = ScreenLayout.PurchaseConfirm(p.Width, p.Height, p.Insets);

                // A level: Bloom Burst without charges buys one for Petals, so it asks first.
                app.LoadLevel(app.Meta.CurrentLevel);
                CloseDemo(app);
                while (app.Meta.Save.Boosters.TryUse(BoosterKind.BloomBurst))
                {
                }

                Run(app, p, 0.1f);
                LevelScreen level = app.Level!;
                VariantId variant = level.Session.Definition.Pods[0].Variant;
                int petals = app.Meta.Economy.Petals;
                int price = app.Meta.Economy.Price(BoosterKind.BloomBurst);
                level.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant));
                Expect(app.IsOpen(Overlay.Purchase) && app.Purchase.Offer!.Petals == price && app.Meta.Economy.Petals == petals && level.Session.BoostersUsed == 0, "a booster without charges asks before buying");
                Run(app, p, 0.5f);
                Expect(Shows(p, app.Purchase.Offer!.Name) && Shows(p,PlaytestText.T("purchase.buy")+"  "+NumberText.Group(price)), "item and actual price on the buy action are visible before spending");
                Tap(p, confirm.Cancel);
                Expect(app.Overlays.Count == 0 && app.Meta.Economy.Petals == petals && level.Session.BoostersUsed == 0, "Cancel buys nothing");
                Run(app, p, 0.1f);
                level.UseBooster(BoosterKind.BloomBurst, new UseBloomBurst(variant));
                Run(app, p, 0.5f);
                Tap(p, confirm.Confirm);
                Expect(app.Overlays.Count == 0 && app.Meta.Economy.Petals == petals - price && level.Session.BoostersUsed == 1 && app.Meta.Economy.Charges(BoosterKind.BloomBurst) == 0, "Buy buys the charge and uses it");

                // The Store's Animations: the cards' buttons.
                app.GoHome();
                CloseAll(app);
                app.OpenStore();
                Run(app, p, 0.1f);
                Box tabs = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets).Tabs;
                Tap(p, Box.FromCenter(tabs.Right - (tabs.Width / 4f), tabs.CenterY, 1f, 1f));
                Expect(app.StoreTab == 1, "the Animations tab");
                Run(app, p, 0.2f);
                Expect(app.NeedsFrames && app.StoreMoving, "the live previews keep the host drawing");
                ReferenceStoreRegions r = ScreenLayout.ReferenceStore(p.Width, p.Height, p.Insets, hasCosmetics: true);
                Expect(Shows(p, PlaytestText.T("clearing.chosen")) && Shows(p, NumberText.Group(app.Meta.Clearing.PriceOf(ClearStyle.Bubbles))), "Munchers Chosen, the others their price (no Buy word)");
                petals = app.Meta.Economy.Petals;
                int bubblesCard = ClearStyles.Free.Count + ClearStyles.Bought.ToList().IndexOf(ClearStyle.Bubbles);
                Box bubbles = ShopCatalogDesign.Parts(ShopCatalogDesign.AnimationCard(ShopCatalogDesign.Viewport(r,false),bubblesCard),false).Action;
                Tap(p, bubbles);
                Expect(app.IsOpen(Overlay.Purchase) && app.Purchase.Offer!.Name == PlaytestText.T("clearing.bubbles") && !app.Meta.Clearing.Owns(ClearStyle.Bubbles), "Bubbles' Buy asks first");
                Run(app, p, 0.3f);
                Tap(p, confirm.Cancel);
                Expect(!app.Meta.Clearing.Owns(ClearStyle.Bubbles) && app.Meta.Economy.Petals == petals, "Cancel buys nothing");
                Run(app, p, 0.1f);
                Tap(p, Box.FromCenter(ShopCatalogDesign.AnimationCard(ShopCatalogDesign.Viewport(r,false),bubblesCard).CenterX, ShopCatalogDesign.AnimationCard(ShopCatalogDesign.Viewport(r,false),bubblesCard).Top + (ShopCatalogDesign.AnimationCard(ShopCatalogDesign.Viewport(r,false),bubblesCard).Height * 0.3f), 1f, 1f));
                Expect(app.IsOpen(Overlay.Purchase), "a tap on the card's preview asks too");
                Expect(app.Back() && app.Overlays.Count == 0 && !app.Meta.Clearing.Owns(ClearStyle.Bubbles), "the system back buys nothing");
                Run(app, p, 0.1f);
                Tap(p, bubbles);
                Run(app, p, 0.5f);
                Tap(p, confirm.Confirm);
                Expect(app.Meta.Clearing.Owns(ClearStyle.Bubbles) && app.Meta.Clearing.IsChosen(ClearStyle.Bubbles) && app.Meta.Economy.Petals == petals - app.Meta.Clearing.PriceOf(ClearStyle.Bubbles), "Buy buys Bubbles and chooses it");
                Run(app, p, 0.1f);
                Expect(Shows(p, PlaytestText.T("clearing.choose")) && Shows(p, PlaytestText.T("clearing.chosen")), "Munchers now Choose, Bubbles Chosen");

                // The frame: Pushers' confirmation over the tab, its live preview in the well.
                Box animationArea = ShopCatalogDesign.Viewport(r, false);
                int pushersIndex = ClearStyles.Free.Count + ClearStyles.Bought.ToList().IndexOf(ClearStyle.Pushers);
                Box pushersCard = ShopCatalogDesign.AnimationCard(animationArea, pushersIndex);
                app.ShopScrollOffset = Math.Max(0, pushersCard.Bottom - animationArea.Bottom + animationArea.Width * .025f);
                Run(app, p, .1f);
                Tap(p, ShopCatalogDesign.Parts(pushersCard.Offset(0, -app.ShopScrollOffset), false, pushersIndex == 6).Action);
                Expect(app.IsOpen(Overlay.Purchase), "Pushers' Buy asks");
                Run(app, p, 2.5f);
                Expect(p.Slots.Contains("ui.card.purchase") && p.Slots.Contains("fx.clear.pushers"), "the card with the live preview");
            });
        }

        private sealed class PurchaseFixtureOperation : IPurchaseOperation
        {
            public string SessionKey => "preview:1";
            public bool IsOnline { get; set; } = true;
            public Action<PurchaseReply>? Pending;
            public int Buys;
            public string Next = "quoted";
            public int Price = 650;
            public void Refresh(Action<PurchaseReply> done)
            {
                if (Next == "loading") { Pending = done; return; }
                done(new PurchaseReply("quoted", new PurchaseQuote("v1", Price, 10000)));
            }
            public void Buy(PurchaseQuote quote, Action<PurchaseReply> done)
            {
                Buys++;
                if (Next == "reprice") { Price = 800; done(new PurchaseReply("price-changed", new PurchaseQuote("v2", Price, 10000))); }
                else done(new PurchaseReply(Next == "quoted" ? "purchased" : Next));
            }
            public void Check(Action<PurchaseReply> done) => Refresh(done);
        }
        public static IEnumerable<Fixture> PurchaseStates(ContentSet content)
        {
            foreach (var example in new[] { (250, "offline"), (251, "loading"), (252, "pending"), (253, "reprice"), (254, "failed") })
            {
                var frame = example;
                yield return new Fixture(frame.Item1, "purchase-" + frame.Item2, "In-game purchase: " + frame.Item2, (p, data) =>
                {
                    var app = Progressed(new DesignApp(data, content, new Silence(), false), content, 44); CloseAll(app); app.OpenStore();
                    var operation = new PurchaseFixtureOperation { Next = frame.Item2, IsOnline = frame.Item2 != "offline" };
                    var offer = PurchaseOffer.ForItem("Moonlight nightcap", 650, "hat.twig_nightcap");
                    app.ShowPurchase(offer, (q, well) => Kit.BoosterIcon(q, "shuffle", well.Inset(well.Width*.1f)), operation);
                    if (frame.Item2 == "pending" || frame.Item2 == "reprice" || frame.Item2 == "failed") app.BuyConfirmed();
                    Run(app, p, .6f);
                    Expect(app.IsOpen(Overlay.Purchase), "An unconfirmed purchase stays visible");
                    if (frame.Item2 == "offline" || frame.Item2 == "loading") Expect(operation.Buys == 0 && !app.Purchase.CanBuy, "Unavailable purchase cannot dispatch");
                    if (frame.Item2 == "pending") Expect(!app.Purchase.CanBuy && app.Purchase.CanCheck, "Unknown result exposes check, not buy");
                    if (frame.Item2 == "reprice") Expect(app.Purchase.Offer!.Petals == 800 && operation.Buys == 1, "New price is shown without a second dispatch");
                    Expect(Shows(p,PlaytestText.T("common.close")) || Shows(p,PlaytestText.T("common.cancel")), "Every purchase state can be dismissed");
                });
            }
            yield return new Fixture(255, "purchase-local-safety", "Local purchase: refusal, offline and changed price", (p, data) =>
            {
                var app = Progressed(new DesignApp(data, content, new Silence(), false), content, 44); CloseAll(app); app.OpenStore();
                app.Meta.Economy.Grant(10000, null);
                int price = 650, calls = 0; long balance = app.Meta.Economy.Petals;
                PurchaseOffer Current() => PurchaseOffer.ForItem("Nightcap", price, "hat.twig_nightcap");
                app.PurchaseNetworkAvailable = () => false;
                app.ConfirmPurchase(Current(), null, () => { calls++; return true; }); app.BuyConfirmed();
                Expect(calls == 0 && app.Meta.Economy.Petals == balance, "Airplane mode never invokes local spending"); app.Back();
                app.PurchaseNetworkAvailable = () => true;
                app.ConfirmPurchase(Current(), null, () => { calls++; return false; }, currentOffer: Current);
                price = 800; app.BuyConfirmed();
                Expect(calls == 0 && app.Purchase.Stage == PurchaseStage.Reprice && app.Purchase.Offer!.Petals == 800, "Live local price changes require a new tap");
                app.BuyConfirmed(); Expect(calls == 1 && app.Purchase.Stage == PurchaseStage.Failed, "Refused local callback is not a successful purchase");
                app.BuyConfirmed(); Expect(calls == 1, "Checking a refused result cannot invoke spending again");
                Run(app, p, .6f);
            });
        }

        /// <summary>The lotus iris (spec 005 FR-039): the splash opening on the first screen, and the win's Next to the next level.</summary>
        public static IEnumerable<Fixture> Loading(ContentSet content)
        {
            DesignApp App(string data, bool splash = false) => new DesignApp(data, content, new Silence(), splash);
            yield return new Fixture(44, "splash-opening", "Extra: the splash's lotus iris opening on Level 1 (spec 005 FR-039)", (p, data) =>
            {
                DesignApp done = App(Directory.CreateDirectory(Path.Combine(data, "done")).FullName, splash: true);
                Run(done, p, LotusIris.SplashFillFrom + LotusIris.SplashFillSeconds + LotusIris.SplashOpenSeconds + 0.2f);
                Expect(!done.SplashOpening && done.Screen == Design.Screen.Level, "the iris is gone once open");

                // The frame: half open.
                DesignApp app = App(data, splash: true);
                Run(app, p, LotusIris.SplashFillFrom + LotusIris.SplashFillSeconds + 0.3f);
                Expect(app.SplashOpening && app.Screen == Design.Screen.Level && app.Level!.Level == 1, "the first launch opens on Level 1 through the iris");
            });

            DesignApp Won(string data, SkiaPainter p)
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(11);
                app.LoadLevel(12);
                CloseDemo(app);
                Run(app, p, 0.1f);
                SolveResult win = new Solver.Solver().Solve(Session(content, 12), SolveOptions.Default);
                Play(app, p, win.Trace, win.Trace.Count);
                Settle(app, p);
                Run(app, p, 1.1f);
                Expect(app.Level!.Won && app.Meta.CurrentLevel == 13, "Level 12 won");
                app.Level.Next();
                Expect(app.Transitioning, "Next starts the lotus iris");
                return app;
            }

            yield return new Fixture(45, "next-closing", "Extra: the win's Next, the lotus iris closing (spec 005 FR-039)", (p, data) =>
            {
                DesignApp app = Won(data, p);
                Run(app, p, 0.3f);
                Expect(app.Level!.Level == 12, "the win stays under the closing iris");
            });

            // The closed and the opening iris need no win under them: Next's transition from Level 12's board.
            DesignApp Next(string data, SkiaPainter p)
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(12);
                app.LoadLevel(12);
                CloseDemo(app);
                Run(app, p, 0.1f);
                Expect(app.Meta.CurrentLevel == 13, "Level 13 is next");
                app.NextLevel();
                return app;
            }

            yield return new Fixture(46, "next-closed", "Extra: the lotus iris closed, \"Level 13\" (spec 005 FR-039)", (p, data) =>
            {
                DesignApp app = Next(data, p);
                Run(app, p, 1.05f);
                Expect(app.Level!.Level == 13, "the next level starts under the closed cover");
                Expect(Shows(p, PlaytestText.F("common.level", 13)), "the cover says Level 13");
                Expect(p.Slots.Contains(LotusIris.PetalShape), "the ring of petals");
            });

            yield return new Fixture(47, "next-opening", "Extra: the lotus iris opening on Level 13 (spec 005 FR-039)", (p, data) =>
            {
                DesignApp done = Next(Directory.CreateDirectory(Path.Combine(data, "done")).FullName, p);
                Run(done, p, LotusIris.TransitionSeconds + 0.1f);
                Expect(!done.Transitioning && done.Screen == Design.Screen.Level && done.Level!.Level == 13, "the iris is gone once open");

                // The frame: half open.
                DesignApp app = Next(data, p);
                Run(app, p, LotusIris.OpenAt + 0.32f);
                Expect(app.Transitioning && app.Level!.Level == 13, "the iris opens on Level 13");
            });
        }

        /// <summary>The preview's text entry: answers every question with the same text at once.</summary>
        private sealed class FixedPrompt : ITextPrompt
        {
            private readonly string _answer;

            public FixedPrompt(string answer) => _answer = answer;

            public void Ask(string title, string text, int maxLength, Action<string> done) => done(_answer);
        }

        /// <summary>
        /// The guided spotlights of the onboarding (spec 005 FR-035): Level 1's entry and forced first tap, the blocked entry
        /// (Level 2), a booster's free use and its "kept" step (Level 3), Return's slots (Level 6) and Bloom Burst's tiles
        /// (Level 9).
        /// </summary>
        public static IEnumerable<Fixture> Guides(ContentSet content)
        {
            DesignApp App(string data) => new DesignApp(data, content, new Silence(), false);

            yield return new Fixture(33, "guide-entry", "Guide: the Garden Entry (Level 1)", (p, data) =>
            {
                DesignApp app = App(data);
                app.LoadLevel(1);
                Run(app, p, 0.5f);
                Expect(app.Level!.Guide?.Kind == GuideKind.Entry && !app.Level.Guide.Forced, "Level 1 opens on the entry's spotlight");
                Expect(Shows(p, PlaytestText.T("demo.entry")) && Shows(p, PlaytestText.T("demo.tap_continue")), "the bubble says where Bloomlings come in, and to tap");
            });

            yield return new Fixture(34, "guide-first-tap", "Guide: the forced first tap (Level 1)", (p, data) =>
            {
                DesignApp app = App(data);
                app.LoadLevel(1);
                Run(app, p, 0.3f);
                LevelScreen level = app.Level!;
                Tap(p, new Box(p.Width * 0.5f, p.Height * 0.1f, p.Width * 0.5f + 1f, p.Height * 0.1f + 1f));
                Expect(level.Guide?.Kind == GuideKind.FirstTap && app.Meta.HasSeenDemo(GuideTour.EntryId), "a tap anywhere goes on to the first tap; the entry is seen");
                Run(app, p, 0.3f);
                string other = level.PodBoxes.Keys.First(id => id != level.GuidePod);
                Tap(p, level.PodBoxes[other]);
                Expect(level.Session.CommandLog.Count == 0, "another pod takes no tap");
                Run(app, p, 0.5f);
            });

            yield return new Fixture(35, "guide-blocked", "Guide: the blocked entry (Level 2)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(1);
                app.Meta.MarkDemoSeen(GuideTour.EntryId);
                app.Meta.MarkDemoSeen(GuideTour.FirstTapId);
                app.LoadLevel(2);
                Run(app, p, 0.5f);
                Expect(app.Level!.Guide?.Kind == GuideKind.Blocked, "Level 2 starts with a pod whose tiles are out of reach: the blocked spotlight");
                Expect(Shows(p, PlaytestText.T("demo.entry_blocked.1")), "the bubble says the tiles are in the way");
            });

            yield return new Fixture(36, "guide-kept", "Guide: the free use done, the charge kept (Level 3)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(2);
                app.LoadLevel(3);
                Run(app, p, 0.3f);
                LevelScreen level = app.Level!;
                int charges = app.Meta.Economy.Charges(BoosterKind.ExtraSlot);
                Tap(p, level.BoosterBoxes[BoosterKind.ExtraSlot]);
                Expect(level.Session.BoostersUsed == 1 && level.DemoBoosterUses == 1, "the lit booster's tap uses it");
                Expect(app.Meta.Economy.Charges(BoosterKind.ExtraSlot) == charges && charges == 1, "the demo's use is free: the unlock's charge stays");
                Expect(level.Guide?.Kind == GuideKind.BoosterKept && app.Meta.HasSeenDemo("booster.extra_slot"), "then the kept step; the demo is seen at the use");
                Run(app, p, 0.6f);
                Expect(p.Texts.Any(t => PlaytestText.T("demo.booster_kept").StartsWith(t.Text, StringComparison.Ordinal) && t.Text.Length > 8), "the bubble says the free one is still there");
            });

            yield return new Fixture(37, "guide-return", "Guide: Return's slot (Level 6, after the first tap)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(5);
                foreach (string id in new[] { "booster.extra_slot", "booster.shuffle", GuideTour.BlockedId })
                {
                    app.Meta.MarkDemoSeen(id);
                }

                app.LoadLevel(6);
                CloseCards(app);
                Run(app, p, 0.2f);
                LevelScreen level = app.Level!;
                Expect(level.Guide == null, "Return's demo waits: no pod waits yet");
                level.Tap(level.Session.View.Stack(0)[0]);
                Run(app, p, 6f);
                Expect(level.Guide?.Kind == GuideKind.Booster && level.Guide.Booster == BoosterKind.Return, "once a pod waits, Return's forced demo starts");
                Tap(p, level.BoosterBoxes[BoosterKind.Return]);
                Expect(level.Guide?.Kind == GuideKind.BoosterTarget && level.Targeting == Recovery.Return, "its tap lights the slot to send back");
                Run(app, p, 0.6f);
            });

            yield return new Fixture(38, "guide-bloom-burst", "Guide: Bloom Burst's tiles (Level 9)", (p, data) =>
            {
                DesignApp app = App(data);
                app.Meta.SkipTo(8);
                foreach (string id in new[] { "booster.extra_slot", "booster.shuffle", "booster.return", GuideTour.BlockedId })
                {
                    app.Meta.MarkDemoSeen(id);
                }

                app.LoadLevel(9);
                CloseCards(app);
                Run(app, p, 0.3f);
                LevelScreen level = app.Level!;
                Expect(level.Guide?.Booster == BoosterKind.BloomBurst, "Level 9 opens on Bloom Burst's forced demo");
                Tap(p, level.BoosterBoxes[BoosterKind.BloomBurst]);
                Expect(level.Guide?.Kind == GuideKind.BoosterTarget && level.Targeting == Recovery.BloomBurst, "its tap lights the board's tiles");
                Run(app, p, 0.6f);
            });
        }

        /// <summary>Closes the mechanic and variant cards of a level, keeping its guided spotlight.</summary>
        private static void CloseCards(DesignApp app)
        {
            while (app.Level?.Demo != null)
            {
                app.Level.CloseDemo();
            }
        }

        /// <summary>Draws frames for <paramref name="seconds"/>: animations advance as on a device; the last frame stays.</summary>
        /// <summary>Scripted service states only in this PNG renderer, never a simulated SDK in the game.</summary>
        public static IEnumerable<Fixture> Accounts(ContentSet content)
        {
            foreach (var example in new[] {
                (240, "account-settings", AccountSaveState.Device),
                (241, "account-device", AccountSaveState.Device),
                (242, "account-pending", AccountSaveState.Pending),
                (243, "account-conflict", AccountSaveState.Conflict),
                (244, "account-restore", AccountSaveState.Conflict),
                (245, "account-cloud", AccountSaveState.Cloud),
                (246, "account-unavailable", AccountSaveState.Unavailable),
                (247, "account-retry", AccountSaveState.Conflict) })
            {
                var frame = example;
                yield return new Fixture(frame.Item1, frame.Item2, "Account & progress: " + frame.Item3, (p, data) =>
                {
                    bool configured = frame.Item3 != AccountSaveState.Device && frame.Item3 != AccountSaveState.Unavailable;
                    var state = new AccountPanelSnapshot("preview:A:1", "one", "Rosie", 45, 1240,
                        configured ? "account.linked_google" : "account.panel.guest", frame.Item3,
                        CanSync: configured, CanReviewCloud: frame.Item3 == AccountSaveState.Conflict);
                    IAccountPanelGateway gateway = new DelegateAccountPanelGateway(() => state,
                        (action, __, done) => done(action == AccountPanelAction.ReviewCloud ? new AccountPanelReply("account.panel.restore_detail", new AccountCloudPreview("preview-token", "Rosie", 51, 700)) : new AccountPanelReply("account.panel.retry")));
                    var app = new DesignApp(data, content, new Silence(), false, frame.Item1 == 240 || frame.Item1 == 241 ? null : gateway);
                    app.Meta.SkipTo(44); app.Meta.Save.Profile.Name = "Rosie"; app.GoHome();
                    app.OpenOverlay(Overlay.Settings);
                    if (frame.Item1 == 240)
                    {
                        Run(app, p, .6f);
                        var card = ScreenLayout.Card(p.Width, p.Height, p.Insets, PopupDesign.SettingsContent);
                        // The new account row is a reachable native button, including on the short phone.
                        float top = card.Body.Top + p.U(12 + PopupDesign.SettingsRowsHeight + 20 + DesignTokens.Size.TouchMin + 12 + DesignTokens.Size.TouchMin + 24);
                        Tap(p, ScreenLayout.CardButton(card.Body, top, false, p.Scale));
                        Expect(app.IsOpen(Overlay.Account), "Settings opens the real account card");
                        app.Back();
                    }
                    else
                    {
                        app.OpenOverlay(Overlay.Account);
                        if (frame.Item1 == 244) app.Account.Run(AccountPanelAction.ReviewCloud);
                        if (frame.Item1 == 247) app.Account.Run(AccountPanelAction.Sync);
                    }
                    Run(app, p, .6f);
                    Expect(Shows(p, PlaytestText.T(frame.Item1 == 240 ? "account.panel.title" : frame.Item1 == 244 ? "account.panel.restore_title" : "account.panel.title")), "Account title is localized");
                });
            }
        }

        private sealed class PendingPurchaseTransport : IEconomyTransport
        {
            private readonly JObject _catalog, _state;
            private long _next = 1;
            public bool IsAvailable => true;
            public string? PlayerId => "alice";
            public long SessionVersion => 1;
            public PendingPurchaseTransport(JObject fixture)
            {
                _catalog=(JObject)fixture["catalog"]!.DeepClone();_state=(JObject)fixture["initialState"]!.DeepClone();
                _state["highestCompletedLevel"]=44;_state["milestones"]=new JArray(25);
            }
            public IEnumerator Read(string owner,long session,string installation,Action<EconomyReply> done)
            {
                done(new EconomyReply(new JObject { ["ok"]=true,["code"]="loaded",["consumed"]=false,["retryable"]=false,
                    ["state"]=_state.DeepClone(),["catalogVersion"]=_catalog["version"]!.DeepClone(),["catalog"]=_catalog.DeepClone(),
                    ["nextSequence"]=_next,["serverUtcDate"]="2026-10-23" }));yield break;
            }
            public IEnumerator Execute(string owner,long session,string operation,Action<EconomyReply> done)
            {
                JObject op=JObject.Parse(operation);
                if ((string?)op["kind"]!="cancel_purchase") { done(EconomyReply.Unavailable);yield break; }
                _next=2;_state["revision"]=1;
                done(new EconomyReply(new JObject { ["ok"]=true,["code"]="purchase-cancelled",["consumed"]=true,["retryable"]=false,
                    ["state"]=_state.DeepClone(),["nextSequence"]=_next,["catalogVersion"]=_catalog["version"]!.DeepClone() }));
            }
        }
        private static void CompleteRoutine(IEnumerator routine)
        {
            try { while(routine.MoveNext())if(routine.Current is IEnumerator child)CompleteRoutine(child); }
            finally { (routine as IDisposable)?.Dispose(); }
        }
        public static IEnumerable<Fixture> ServerEconomyStates(ContentSet content,string root)
        {
            foreach(int number in new[]{261,262,263})
                yield return new Fixture(number,number==261?"account-pending-purchase":number==262?"purchase-cancel-request":"purchase-cancelled","Server account pending purchase",(p,data)=>
                {
                    var saves=new SaveService(Path.Combine(data,SaveService.FolderName),new SystemClock());var save=saves.Load();
                    save.CloudOwnerId="alice";save.Progression.HighestCompletedLevel=44;save.Profile.Name="Rosie";saves.Save();
                    JObject fixture=JObject.Parse(File.ReadAllText(Path.Combine(root,"backend/tests/fixtures/economy-client.json")));
                    var transport=new PendingPurchaseTransport(fixture);
                    using var journal=new EconomyJournal(Path.Combine(data,"server-economy"),"alice",new EconomyCatalog((JObject)fixture["catalog"]!));
                    using var client=new OnlineEconomyClient(journal,transport);CompleteRoutine(client.Refresh());
                    using var app=new DesignApp(data,content,new Silence(),false,serverEconomy:client,activateServerEconomy:true);
                    Run(app,p,.6f);CompleteRoutine(client.Purchase(client.Catalog.QuoteItem("hat.twig_nightcap")!));
                    Expect(client.PendingPurchase!=null,"unavailable purchase is durable");
                    app.OpenOverlay(Overlay.Account);Run(app,p,.6f);
                    Expect(Shows(p,PlaytestText.T("account.panel.pending_purchase")),"account exposes pending purchase");
                    if(number>=262)
                    {
                        // Use the actual action after its presence was checked; the purchase card itself is native.
                        Tap(p,p.Texts.First(t=>t.Text==PlaytestText.T("account.panel.pending_purchase")).Box);Run(app,p,.6f);
                        Expect(Shows(p,PlaytestText.T("purchase.cancel_pending")),"safe cancellation action is visible");
                        Expect(app.Purchase.Stage==PurchaseStage.Pending,"opening does not silently replay the purchase");
                        if(number==263)
                        {
                            Tap(p,p.Texts.First(t=>t.Text==PlaytestText.T("purchase.cancel_pending")).Box);Run(app,p,.6f);
                            Expect(!app.IsOpen(Overlay.Purchase) && client.PendingCount==0,"native cancel button obtains the terminal cancellation ACK");
                            Expect(!client.Owns("hat.twig_nightcap") && client.Confirmed!.Petals==10000,"cancelling does not buy or spend");
                        }
                    }
                    int petals=app.Meta.Economy.Petals;app.Meta.Economy.AdjustPetalsForTesting(10000);app.ResetProfile();
                    Expect(app.Meta.Economy.Petals==petals && app.Meta.Economy.IsAuthoritative,"tester controls cannot switch to local money");
                });
        }

        public static IEnumerable<Fixture> Competition(ContentSet content)
        {
            yield return new Fixture(260,"leaderboard-rules","Leaderboard proposal in the game UI",(p,data)=>
            {
                var app = new DesignApp(data,content,new Silence(),false); app.Meta.SkipTo(44); app.GoHome(); app.OpenLeaderboard();
                Run(app,p,.6f);
                Tap(p,LeaderboardScreen.RulesButton(p));
                Expect(app.IsOpen(Overlay.CompetitionRules),"rules button opens the real native card");
                Run(app,p,.6f);
                Expect(Shows(p,PlaytestText.T("leaderboard.rules_title")),"localized ranking rules");
                Expect(!CompetitionRules.EnabledByDefault,"draft does not enable competitive submissions");
            });
        }

        private static void Run(DesignApp app, SkiaPainter p, float seconds)
        {
            int frames = Math.Max(1, (int)(seconds / Frame));
            for (int i = 0; i < frames; i++)
            {
                p.Now += Frame;
                p.BeginFrame();
                app.Draw(p, Frame);
            }
        }

        /// <summary>
        /// Draws frames until <paramref name="promoSeconds"/> after Home was last shown (<see cref="DesignApp.PromoSeconds"/>),
        /// 0.1 s apart (the app's longest step: Home's scenes and heroes follow the clock, so a long wait needs fewer frames).
        /// </summary>
        private static void RunTo(DesignApp app, SkiaPainter p, float promoSeconds)
        {
            const float step = 0.1f;
            do
            {
                p.Now += step;
                p.BeginFrame();
                app.Draw(p, step);
            }
            while (app.PromoSeconds < promoSeconds);
        }

        /// <summary>Plays the animation until it has shown the rules state (at most a minute of game time).</summary>
        private static void Settle(DesignApp app, SkiaPainter p)
        {
            for (int i = 0; i < 1800 && app.Level != null && !app.Level.Animator.Settled; i++)
            {
                p.Now += Frame;
                p.BeginFrame();
                app.Draw(p, Frame);
            }
        }

        /// <summary>The bottom menu as the app's screen draws it (its five places, the screen's place active).</summary>
        private static BottomNavRegions Nav(SkiaPainter p, DesignApp app) => HomeScreen.Nav(p, app.ActivePlace);

        /// <summary>Whether the last drawn frame shows <paramref name="text"/> as one of its texts.</summary>
        private static bool Shows(SkiaPainter p, string text) => p.Texts.Any(t => t.Text == text);

        /// <summary>The touch box of a place of the bottom menu as the app's screen draws it.</summary>
        private static Box NavTouch(SkiaPainter p, DesignApp app, NavPlace place)
        {
            BottomNavRegions nav = Nav(p, app);
            int index = nav.IndexOf(place);
            Expect(index >= 0, "the bottom menu shows " + place);
            return nav.Touch(index);
        }

        /// <summary>A tap in the middle of <paramref name="box"/> on the last drawn frame, as a finger would.</summary>
        private static void Tap(SkiaPainter p, Box box) => p.Dispatch(box.CenterX, box.CenterY);

        /// <summary>A finger going down at (x0, y0), moving in eight steps to (x1, y1) and lifting there, on the last drawn frame.</summary>
        private static void Drag(SkiaPainter p, float x0, float y0, float x1, float y1)
        {
            p.TouchDown(x0, y0);
            for (int i = 1; i <= 8; i++)
            {
                p.TouchMove(x0 + ((x1 - x0) * i / 8f), y0 + ((y1 - y0) * i / 8f));
            }

            p.TouchUp(x1, y1);
        }

        /// <summary>Fails the frame (the preview reports it) when a scripted interaction did not do what it should.</summary>
        private static void Expect(bool condition, string what)
        {
            if (!condition)
            {
                throw new InvalidOperationException("expected: " + what);
            }
        }

        private static void CloseAll(DesignApp app)
        {
            while (app.Overlays.Count > 0)
            {
                app.CloseOverlay();
            }
        }

        /// <summary>Closes a demo card and ends the guided spotlights, now and for later (their demos count as seen).</summary>
        private static void CloseDemo(DesignApp app)
        {
            if (app.Level?.Demo != null)
            {
                app.Level.CloseDemo();
            }

            foreach (string id in GuideTour.DemoIds)
            {
                app.Meta.MarkDemoSeen(id);
            }

            app.Level?.SkipGuide();
        }

        /// <summary>
        /// An early profile at Level <paramref name="highest"/>+1 with the pictures of the levels it won in its Collection, as
        /// a player has them (the skip collects none), on Home with no card open.
        /// </summary>
        private static DesignApp Early(DesignApp app, ContentSet content, int highest)
        {
            app.Meta.SkipTo(highest);
            for (int level = 1; level <= highest; level++)
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                {
                    app.Meta.Collection.Add(definition, level);
                }
            }

            app.GoHome();
            CloseAll(app);
            return app;
        }

        /// <summary>A profile at Level <paramref name="highest"/>+1 with some Petals and pictures in its Collection, on Home.</summary>
        // Art-only fixtures own their gear explicitly. Release purchase/unlock fixtures use fresh inventories.
        private static void OwnHeroGear(DesignApp app)
        {
            foreach(var item in app.Meta.Wardrobe.Catalog.Items)
                if(item.ForFamily.HasValue&&!item.Retired)app.Meta.Save.Cosmetics.Owned.Add(item.Id);
        }

        private static DesignApp Progressed(DesignApp app, ContentSet content, int highest)
        {
            app.Meta.SkipTo(highest);
            app.Meta.Economy.Grant(1240 - app.Meta.Economy.Petals, null);
            foreach (int level in new[] { 11, 12, 14, 16, 17, 18 })
            {
                if (content.TryGetLevel(app.Resolve(level), out LevelDefinition? definition) && definition != null)
                {
                    app.Meta.Collection.Add(definition, level);
                }
            }

            app.GoHome();
            return app;
        }

        private static LevelSession Session(ContentSet content, int level)
        {
            LevelDefinition definition = content.GetLevel(level);
            return LevelSession.Load(definition, content.GetPicture(definition.Picture), new SessionOptions(content.ContentVersion, content.ShuffleNodeBudget));
        }

        /// <summary>A level in play: a few moves of its solution, caught while Bloomlings walk.</summary>
        private static void Playing(DesignApp app, SkiaPainter p, ContentSet content, int level, int taps)
        {
            app.Meta.SkipTo(level - 1);
            app.Meta.Economy.Grant(2000, null);
            app.LoadLevel(level);
            CloseDemo(app);
            Run(app, p, 0.1f);
            SolveResult win = new Solver.Solver().Solve(Session(content, level), SolveOptions.Default);
            Play(app, p, win.Trace, taps);
            Run(app, p, 0.45f);
        }

        /// <summary>Plays commands as a player would: a tap, then a moment of animation.</summary>
        private static void Play(DesignApp app, SkiaPainter p, IReadOnlyList<Command> commands, int count)
        {
            for (int i = 0; i < count && i < commands.Count; i++)
            {
                LevelScreen level = app.Level!;
                switch (commands[i])
                {
                    case TapPod tap:
                        // As a player must (the owner, 2026-10-05): wait until a slot shows no pod for each pod the tap commits.
                        for (int wait = 0; wait < 600 && level.Animator.FreeOnScreen(level.Session.View) < level.Session.View.ConnectedGroup(tap.PodId).Count; wait++)
                        {
                            Run(app, p, 0.1f);
                        }

                        level.Tap(tap.PodId);
                        break;
                    case UseExtraSlot:
                        level.UseBooster(BoosterKind.ExtraSlot, commands[i], free: true);
                        break;
                    case UseShuffle:
                        level.UseBooster(BoosterKind.Shuffle, commands[i], free: true);
                        break;
                    case UseReturn:
                        level.UseBooster(BoosterKind.Return, commands[i], free: true);
                        break;
                    case UseBloomBurst:
                        level.UseBooster(BoosterKind.BloomBurst, commands[i], free: true);
                        break;
                }

                Run(app, p, i == count - 1 ? Frame : 0.35f);
            }
        }

        // ---- Component sheets (frames 12–14) ----

        private static void Sheet(SkiaPainter p, string title, out Box body)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            p.FillRound(safe.Inset(p.U(30f)), p.U(56f), C.SurfacePanel.WithAlpha(0.93f));
            p.Text(title, safe.CenterX, safe.Top + p.U(120f), T.TitleCaps, C.TextPrimary);
            body = new Box(safe.Left + p.U(70f), safe.Top + p.U(220f), safe.Right - p.U(70f), safe.Bottom - p.U(70f));
        }

        private static Box[] Grid(Box body, IPainter p, int count, int columns, float cellHeight)
        {
            var cells = new Box[count];
            float w = body.Width / columns;
            for (int i = 0; i < count; i++)
            {
                float x = body.Left + ((i % columns) * w);
                float y = body.Top + ((i / columns) * cellHeight);
                cells[i] = new Box(x, y, x + w, y + cellHeight);
            }

            return cells;
        }

        /// <summary>
        /// A component sheet in the reference look (frames 12–14, spec 005 §4.1, §6.1): the gameplay tray's parchment panel
        /// over the lawn from under the title to the bottom of the screen, and the title in <c>ink.brown</c>. The sheet's
        /// pieces take their sizes from the reference gameplay regions (<see cref="Reference"/>), so they look as in play.
        /// </summary>
        private static void TraySheet(SkiaPainter p, string title, out Box body)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            float w = safe.Width;
            Kit.WoodSign(p, Box.FromCenter(safe.CenterX, safe.Top + (0.08f * w), 0.56f * w, 0.115f * w), title, T.LevelPill, SignDecor.Ivy);
            var tray = new Box(0f, safe.Top + (0.17f * w), p.Width, p.Height);
            LevelScreen.TrayPanel(p, tray, 0.06f * w, w, Array.Empty<float>());
            body = new Box(safe.Left + (0.04f * w), tray.Top + (0.03f * w), safe.Right - (0.04f * w), safe.Bottom - (0.02f * w));
        }

        /// <summary>The reference gameplay regions of this frame's shape, for the sizes of decks, plates and booster boxes.</summary>
        private static ReferenceGameplayRegions Reference(IPainter p, int stacks = 4, int slots = 5) =>
            ScreenLayout.ReferenceGameplay(p.Width, p.Height, p.Insets, stacks, slots);

        /// <summary>A state's label on a component sheet: <c>ink.brown_soft</c> captions.</summary>
        private static void StateLabel(IPainter p, string label, float cx, float cy, float maxWidth) =>
            p.Text(label, cx, cy, T.Caption, C.InkBrownSoft, maxWidth);

        /// <summary>A separator line across a component sheet, as between the tray's rows.</summary>
        private static void SheetLine(IPainter p, Box body, float y) =>
            LevelScreen.Separator(p, Box.FromCenter(body.CenterX, y, body.Width, Math.Max(2f, body.Width * 0.0055f)));

        /// <summary>
        /// Frame 12: the pods of the tray's grid (spec 005 FR-021, the owner's rule of 2026-10-03) in every state at their
        /// size in play (exposed, pressed, locked, mystery; waiting under the exposed one, also as a hidden mystery and
        /// locked; "+N" more below; an emptied stack), and four columns as the tray shows them, one after another and never
        /// on each other, two exposed pods connected and a group across rows marked by rings.
        /// </summary>
        private static void PodStates(SkiaPainter p)
        {
            TraySheet(p, "Pod states", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            Box exposedSize = g.Pod(0, 0);
            Box waitingSize = g.Pod(0, 1);
            (VariantId? Variant, int Count, bool Locked) Pod(VariantId? variant, int count, bool locked = false) => (variant, count, locked);
            (string Label, VariantId? Variant, int Count, PodLook Look, bool Waiting, int More, bool Empty)[] states =
            {
                ("Exposed", VariantId.Leaf, 18, PodLook.Exposed, false, 0, false),
                ("Pressed", VariantId.Leaf, 18, PodLook.Pressed, false, 0, false),
                ("Locked", VariantId.Acorn, 20, PodLook.Locked, false, 0, false),
                ("Mystery", null, 14, PodLook.Exposed, false, 0, false),
                ("Waiting", VariantId.Flower, 9, PodLook.Exposed, true, 0, false),
                ("Waiting mystery", null, 7, PodLook.Exposed, true, 0, false),
                ("Waiting, locked", VariantId.Water, 4, PodLook.Locked, true, 0, false),
                ("More below (+4)", VariantId.Dew, 10, PodLook.Exposed, true, 4, false),
                ("Emptied stack", null, 0, PodLook.Exposed, false, 0, true),
            };

            float rowHeight = exposedSize.Height + (0.085f * g.W);
            Box[] cells = Grid(body, p, states.Length, 3, rowHeight);
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + (0.03f * g.W), cell.Width);
                Box size = states[i].Waiting ? waitingSize : exposedSize;
                Box box = Box.FromCenter(cell.CenterX, cell.Top + (0.065f * g.W) + (exposedSize.Height / 2f), size.Width, size.Height);
                if (states[i].Empty)
                {
                    PodPainter.EmptyColumn(p, box);
                    continue;
                }

                var chip = PodChip.In(box);
                Kit.Pod(p, chip, states[i].Variant, states[i].Count, states[i].Look, states[i].Waiting);
                if (states[i].More > 0)
                {
                    PodPainter.MoreBadge(p, chip, states[i].More);
                }
            }

            // Four columns as in play: the exposed pods on the top row, the next ones under them.
            float rowTop = cells[cells.Length - 1].Bottom + (0.01f * g.W);
            if (rowTop + (0.07f * g.W) + g.PodRow.Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, rowTop);
            StateLabel(p, "The tray in play: each stack a column, connected pods with their link badge", body.CenterX, rowTop + (0.035f * g.W), body.Width);
            float dy = rowTop + (0.07f * g.W) - g.PodRow.Top;
            var columns = new[]
            {
                new[] { Pod(VariantId.Leaf, 12), Pod(VariantId.Moss, 5), Pod(VariantId.Wood, 9), Pod(VariantId.Flower, 6) },
                new[] { Pod(VariantId.Flower, 8), Pod(VariantId.Water, 3), Pod(VariantId.Leaf, 6), Pod(VariantId.Dew, 4) },
                new[] { Pod(VariantId.Water, 14), Pod(null, 4), Pod(VariantId.Flower, 7), Pod(VariantId.Acorn, 5, locked: true) },
                new[] { Pod(VariantId.Acorn, 6), Pod(VariantId.VioletBud, 8), Pod(VariantId.Dew, 2) },
            };
            int[] totals = { 4, 7, 4, 3 };
            var frames = new Box[Math.Min(columns.Length, g.Columns.Count)][];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = PodPainter.DrawColumn(p, g, i, dy, columns[i], totals[i], PodLook.Exposed);
            }

            if (frames.Length >= 4)
            {
                // A pair side by side on the top row (the bar and both badges), and a pair apart: one member shown, its
                // partner under the "+N" of the second column (FR-043).
                PodPainter.Link(p, frames[1][0], frames[2][0]);
                PodPainter.LinkBadge(p, PodLinks.BadgeBox(frames[1][0]), PodLinks.Palette[0]);
                PodPainter.LinkBadge(p, PodLinks.BadgeBox(frames[2][0]), PodLinks.Palette[0]);
                PodPainter.LinkBadge(p, PodLinks.BadgeBox(frames[3][2]), PodLinks.Palette[1]);
                PodPainter.LinkBadge(p, PodLinks.HiddenMark(PodChip.In(g.Pod(1, g.PodRows - 1).Offset(0f, dy))), PodLinks.Palette[1]);
            }
        }

        /// <summary>The variants shown on the themes frame's board corners.</summary>
        private static readonly VariantId[] ThemeTiles = { VariantId.Leaf, VariantId.Flower, VariantId.Water, VariantId.Acorn, VariantId.Moss, VariantId.VioletBud, VariantId.Dew, VariantId.Wood };

        /// <summary>
        /// Every variant (spec 005 §3.1, spec 004): its candy tile in the board and sticker styles next to its 2D character
        /// in each mood (happy, asleep, worried, blank), on parchment over the lawn, and the four 3D heroes of the meta
        /// screens below, each on its stone pedestal.
        /// </summary>
        private static void Bloomlings(SkiaPainter p)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            Box sheet = safe.Inset(p.U(24f));
            Kit.CardFrame(p, sheet);
            Kit.WoodSign(p, Box.FromCenter(sheet.CenterX, sheet.Top + p.U(84f), p.U(470f), p.U(104f)), "Bloomlings", T.Title, SignDecor.Ivy);
            var body = new Box(sheet.Left + p.U(36f), sheet.Top + p.U(170f), sheet.Right - p.U(36f), sheet.Bottom - p.U(30f));

            VariantInfo[] variants = VariantCatalog.Default.All.ToArray();
            int columns = 2 + CharacterArt.Moods.Count;
            float w = body.Width / columns;
            float heroes = Math.Min(p.U(330f), body.Height * 0.22f);
            float row = Math.Min(p.U(126f), (body.Height - heroes - p.U(60f)) / variants.Length);
            string[] headers = { "board", "sticker", "happy", "asleep", "worried", "blank" };
            for (int c = 0; c < columns; c++)
            {
                p.Text(headers[c], body.Left + ((c + 0.5f) * w), body.Top + p.U(16f), T.Caption, C.InkBrownSoft, w);
            }

            for (int v = 0; v < variants.Length; v++)
            {
                float cy = body.Top + p.U(50f) + ((v + 0.5f) * row);
                float size = Math.Min(row * 0.86f, w * 0.8f);
                Kit.CandyTile(p, Box.FromCenter(body.Left + (0.5f * w), cy, size, size), variants[v].Id, TileStyle.Board);
                Kit.CandyTile(p, Box.FromCenter(body.Left + (1.5f * w), cy, size, size), variants[v].Id, TileStyle.Sticker);
                for (int m = 0; m < CharacterArt.Moods.Count; m++)
                {
                    Visuals.Character(p, Box.FromCenter(body.Left + ((m + 2.5f) * w), cy, size, size), variants[v].Id, CharacterArt.Moods[m]);
                }
            }

            // The four heroes, each on its stone pedestal, feet on the pedestal's top.
            float hw = body.Width / CharacterArt.Families.Count;
            for (int f = 0; f < CharacterArt.Families.Count; f++)
            {
                float cx = body.Left + ((f + 0.5f) * hw);
                var pedestal = Box.FromCenter(cx, body.Bottom - (heroes * 0.13f), hw * 0.78f, heroes * 0.26f);
                Kit.StonePedestal(p, pedestal);
                float top = HomeStage.PedestalTop(pedestal).CenterY;
                Visuals.Hero(p, HomeStage.Figure(cx, top, heroes * 0.86f), CharacterArt.Families[f], null);
            }
        }

        /// <summary>
        /// The reference look's kit on parchment (spec 005 T009), like the reference's "Target Variants" and "UI Elements"
        /// strips: the candy tiles in both styles, the stone border, the pedestal with rays and petals, the wooden
        /// signs, the buttons and their pressed state, the round buttons and pills, booster tiles, pods, slots and the jam
        /// choices.
        /// </summary>
        private static void KitSheet(SkiaPainter p)
        {
            p.BeginFrame();
            DesignApp.DrawBackdrop(p, BackdropScene.Gameplay, 1);
            Box safe = ScreenLayout.SafeArea(p.Width, p.Height, p.Insets);
            Box sheet = safe.Inset(p.U(20f));
            Kit.CardFrame(p, sheet);
            Box body = sheet.Inset(p.U(44f), p.U(34f));
            float[] heights = { 70f, 150f, 330f, 150f, 170f, 140f, 220f, 230f, 250f };
            float gap = Math.Max(0f, (body.Height - p.U(heights.Sum())) / (heights.Length - 1));
            var rows = new Box[heights.Length];
            float y = body.Top;
            for (int i = 0; i < heights.Length; i++)
            {
                rows[i] = new Box(body.Left, y, body.Right, y + p.U(heights[i]));
                y = rows[i].Bottom + gap;
            }

            p.Text("Reference look kit", rows[0].CenterX, rows[0].CenterY, T.Title, C.InkBrown, rows[0].Width, look: TextLook.Plain(C.InkBrown));

            // The eight launch variants as sticker tiles (pods, slots, the jam row), with their names.
            VariantInfo[] launch = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Launch).ToArray();
            Box[] stickers = ScreenLayout.Row(new Box(rows[1].Left, rows[1].Top, rows[1].Right, rows[1].Top + p.U(100f)), launch.Length, p.U(16f), p.U(100f), square: true);
            for (int i = 0; i < launch.Length; i++)
            {
                Kit.CandyTile(p, stickers[i], launch[i].Id, TileStyle.Sticker);
                p.Text(launch[i].Id.Key.Replace('_', ' '), stickers[i].CenterX, stickers[i].Bottom + p.U(26f), T.Caption, C.InkBrownSoft, stickers[i].Width + p.U(14f));
            }

            // The board style in a 4 × 2 grid inside the stone border, centered in its row (a Garden Entry has no picture).
            float cell = p.U(78f);
            float rim = cell * 0.48f;
            var grid = new Box(rows[2].Left + rim, rows[2].CenterY - cell, rows[2].Left + rim + (cell * 4f), rows[2].CenterY + cell);
            Kit.StoneBorder(p, grid, cell);
            for (int i = 0; i < launch.Length; i++)
            {
                float cx = grid.Left + ((i % 4) * cell);
                float cy = grid.Top + ((i / 4) * cell);
                Kit.CandyTile(p, new Box(cx, cy, cx + cell, cy + cell), launch[i].Id, TileStyle.Board);
            }

            // The expansion variants, and a hero on the stone pedestal in the win's light.
            VariantInfo[] expansion = VariantCatalog.Default.All.Where(v => v.Status == VariantStatus.Expansion).ToArray();
            float ex = grid.Right + rim + p.U(40f);
            for (int i = 0; i < expansion.Length; i++)
            {
                float sx = ex + ((i % 2) * p.U(100f));
                float sy = rows[2].Top + p.U(10f) + ((i / 2) * p.U(130f));
                var tile = new Box(sx, sy, sx + p.U(84f), sy + p.U(84f));
                Kit.CandyTile(p, tile, expansion[i].Id, TileStyle.Sticker);
                p.Text(expansion[i].Id.Key, tile.CenterX, tile.Bottom + p.U(22f), T.Caption, C.InkBrownSoft, tile.Width + p.U(14f));
            }

            var stage = new Box(ex + p.U(210f), rows[2].Top, rows[2].Right, rows[2].Bottom);
            var pedestal = Box.FromCenter(stage.CenterX, stage.Bottom - p.U(50f), Math.Min(stage.Width, p.U(230f)), p.U(100f));
            Kit.LightRays(p, pedestal.CenterX, pedestal.Top - p.U(70f), p.U(200f), p.Now);
            Box top = Kit.StonePedestal(p, pedestal);
            float hero = p.U(190f);
            Visuals.Hero(p, new Box(top.CenterX - (hero * 0.45f), top.CenterY - hero, top.CenterX + (hero * 0.45f), top.CenterY + (hero * 0.06f)), Family.Bloom, null);

            // The drawn Home stage's lotus fountain (ui.fountain), shown on Home only while the owner's picture is missing.
            Kit.LotusFountain(p, Box.FromCenter(pedestal.Left + (pedestal.Width * 0.08f), pedestal.Bottom - (pedestal.Height * 0.12f), pedestal.Width * 0.42f, pedestal.Width * 0.15f));
            Kit.FallingPetals(p, stage, p.Now);

            // Wooden signs: the gameplay level with ivy, the win title with flowers.
            Kit.WoodSign(p, new Box(rows[3].Left + p.U(50f), rows[3].CenterY - p.U(46f), rows[3].Left + p.U(320f), rows[3].CenterY + p.U(46f)), PlaytestText.F("common.level", 88), T.LevelPill, SignDecor.Ivy);
            Kit.WoodSign(p, new Box(rows[3].Right - p.U(530f), rows[3].CenterY - p.U(60f), rows[3].Right - p.U(30f), rows[3].CenterY + p.U(60f)), "Level complete!", T.Title, SignDecor.Flowers);

            // Buttons: Play in its wooden rim, the same pressed, the orange Next.
            Box[] buttons = Spread(rows[4], new[] { 360f, 290f, 250f }, new[] { 150f, 124f, 124f }, p);
            Kit.PrimaryButton(p, buttons[0], PlaytestText.T("common.play"), () => { }, T.ButtonLarge, decorate: true);
            p.Finger = (buttons[1].CenterX, buttons[1].CenterY);
            Kit.PrimaryButton(p, buttons[1], PlaytestText.T("common.play"), () => { });
            p.Finger = null;
            Kit.PrimaryButton(p, buttons[2], PlaytestText.T("common.next"), () => { }, set: GardenLook.Orange);

            // The icon buttons (rounded squares in their wood rims) and the speed pill.
            Box[] round = Spread(rows[5], new[] { 124f, 210f, 124f, 104f, 124f }, new[] { 124f, 112f, 124f, 104f, 124f }, p);
            Kit.RoundButton(p, round[0].CenterX, round[0].CenterY, round[0].Width, "ui.pause", () => { });
            Kit.SpeedPill(p, round[1], true, () => { });
            Kit.RoundButton(p, round[2].CenterX, round[2].CenterY, round[2].Width, "ui.settings", () => { });
            Kit.RoundButton(p, round[3].CenterX, round[3].CenterY, round[3].Width, "ui.close", () => { });
            Kit.RoundButton(p, round[4].CenterX, round[4].CenterY, round[4].Width, GardenLook.BackGlyph.ShapeId, () => { });

            // Booster tiles (charges, price, selected, disabled), the Petals pill, a cost pill and a count badge.
            Box[] tray = Spread(new Box(rows[6].Left, rows[6].Top + p.U(20f), rows[6].Right, rows[6].Top + p.U(180f)), new[] { 140f, 140f, 140f, 140f, 280f }, new[] { 146f, 146f, 146f, 146f, 160f }, p);
            Kit.BoosterTile(p, tray[0], "extra_slot", new BoosterTileState(3, 30, false, true, true), () => { });
            Kit.BoosterTile(p, tray[1], "shuffle", new BoosterTileState(0, 30, false, true, true), () => { });
            Kit.BoosterTile(p, tray[2], "return", new BoosterTileState(1, 50, true, true, true), () => { });
            Kit.BoosterTile(p, tray[3], "bloom_burst", new BoosterTileState(0, 60, false, true, false), null);
            Kit.PetalsPill(p, new Box(tray[4].Left, tray[4].Top, tray[4].Right - p.U(20f), tray[4].Top + p.U(76f)), 2450, () => { }, align: 0f);
            Kit.CostPill(p, new Box(tray[4].Left, tray[4].Bottom - p.U(60f), tray[4].Left + p.U(170f), tray[4].Bottom), Cost.Charges(2));
            Kit.CountBadge(p, tray[4].Right - p.U(40f), tray[4].Bottom - p.U(30f), p.U(56f), "3");

            // Pods of the tray's grid (the exposed one, one waiting under it) and Waiting Slots (working, empty, stuck).
            Box[] pods = Spread(new Box(rows[7].Left, rows[7].Top + p.U(24f), rows[7].Right, rows[7].Bottom), new[] { 206f, 206f, 132f, 132f, 132f }, new[] { 124f, 96f, 150f, 150f, 150f }, p);
            Kit.Pod(p, PodChip.In(pods[0]), VariantId.Leaf, 12, PodLook.Exposed, waiting: false);
            Kit.Pod(p, PodChip.In(pods[1]), VariantId.Flower, 8, PodLook.Exposed, waiting: true);
            Kit.SlotPlate(p, pods[2], SlotPlateState.Working, VariantId.Water, 3);
            Kit.SlotPlate(p, pods[3], SlotPlateState.Empty);
            Kit.SlotPlate(p, pods[4], SlotPlateState.Stuck, VariantId.Acorn, 4);

            // The jam's choices with their cost pills, and Restart.
            Box[] choices = Spread(rows[8], new[] { 300f, 300f, 270f }, new[] { 240f, 240f, 110f }, p);
            Kit.ChoiceButton(p, choices[0], GardenLook.Green, GardenLook.BoosterIcon("extra_slot"), PlaytestText.T("booster.extra_slot"), Cost.Petals(30), () => { });
            Kit.ChoiceButton(p, choices[1], GardenLook.Blue, GardenLook.BoosterIcon("return"), PlaytestText.T("booster.return"), Cost.Free, () => { });
            Kit.SecondaryButton(p, choices[2], PlaytestText.T("common.restart"), () => { }, "ui.restart");
        }

        /// <summary>Boxes of the given sizes (reference units) spread evenly across a row and centered in its height.</summary>
        private static Box[] Spread(Box row, float[] widths, float[] heights, IPainter p)
        {
            float total = p.U(widths.Sum());
            float gap = (row.Width - total) / (widths.Length + 1);
            var boxes = new Box[widths.Length];
            float x = row.Left + gap;
            for (int i = 0; i < widths.Length; i++)
            {
                boxes[i] = new Box(x, row.CenterY - (p.U(heights[i]) / 2f), x + p.U(widths[i]), row.CenterY + (p.U(heights[i]) / 2f));
                x += p.U(widths[i]) + gap;
            }

            return boxes;
        }

        /// <summary>
        /// Frame 13: every Waiting Slot state (spec 002 FR-013, spec 005 §3.7) on plates of their size in play (§6.1), and
        /// the slot row as it shows in play.
        /// </summary>
        private static void SlotStates(SkiaPainter p)
        {
            TraySheet(p, "Waiting slot states", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            Box plateSize = g.Slots[0];
            (string Label, SlotLook Look, bool Locked, bool Danger, bool Extra)[] states =
            {
                ("Empty", new SlotLook(), false, false, false),
                ("Working", new SlotLook { PodId = "a", Variant = VariantId.Leaf, Count = 12, InFlight = 2 }, false, false, false),
                ("Stuck", new SlotLook { PodId = "b", Variant = VariantId.Water, Count = 9 }, false, false, false),
                ("Locked", new SlotLook(), true, false, false),
                ("Danger (4/5)", new SlotLook(), false, true, false),
                ("Extra slot", new SlotLook(), false, false, true),
                ("Mystery", new SlotLook { PodId = "c", Count = 7, InFlight = 1 }, false, false, false),
                ("Extra slot, working", new SlotLook { PodId = "d", Variant = VariantId.Acorn, Count = 5, InFlight = 1 }, false, false, true),
                ("Return target", new SlotLook { PodId = "e", Variant = VariantId.Flower, Count = 4, InFlight = 1 }, false, false, false),
            };

            // Plates a little larger than in play, so each state reads on the sheet.
            float scale = 1.25f;
            float rowHeight = (plateSize.Height * scale) + (0.11f * g.W);
            Box[] cells = Grid(body, p, states.Length, 3, rowHeight);
            for (int i = 0; i < states.Length; i++)
            {
                Box cell = cells[i];
                var plate = Box.FromCenter(cell.CenterX, cell.Top + (0.08f * g.W) + (plateSize.Height * scale / 2f), plateSize.Width * scale, plateSize.Height * scale);
                if (states[i].Label == "Return target")
                {
                    SlotPainter.TargetGlow(p, plate);
                }

                SlotPainter.Slot(p, plate, states[i].Look, states[i].Locked, states[i].Danger, states[i].Extra, 10f);
                StateLabel(p, states[i].Label, cell.CenterX, cell.Top + (0.035f * g.W), cell.Width);
            }

            // The row as it shows in play: four working pods (one stuck) and the last free slot, in the regions' plates.
            float top = cells[cells.Length - 1].Bottom + (0.02f * g.W);
            if (top + (0.08f * g.W) + plateSize.Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, top);
            StateLabel(p, "The row in play", body.CenterX, top + (0.04f * g.W), body.Width);
            float dy = top + (0.08f * g.W) - g.Slots[0].Top;
            SlotLook[] looks =
            {
                new SlotLook { PodId = "r1", Variant = VariantId.Leaf, Count = 3, InFlight = 1 },
                new SlotLook { PodId = "r2", Variant = VariantId.Flower, Count = 2, InFlight = 1 },
                new SlotLook { PodId = "r3", Variant = VariantId.Water, Count = 1 },
                new SlotLook { PodId = "r4", Variant = VariantId.Acorn, Count = 4, InFlight = 2 },
                new SlotLook(),
            };
            for (int i = 0; i < looks.Length && i < g.Slots.Count; i++)
            {
                SlotPainter.Slot(p, g.Slots[i].Offset(0f, dy), looks[i], false, i == looks.Length - 1, false, 10f);
            }

            // With the sixth slot of Extra Slot, the row narrows its plates.
            ReferenceGameplayRegions six = Reference(p, slots: 6);
            float sixTop = top + (0.1f * g.W) + plateSize.Height;
            if (sixTop + (0.08f * g.W) + six.Slots[0].Height > body.Bottom)
            {
                return;
            }

            SheetLine(p, body, sixTop);
            StateLabel(p, "With the extra slot", body.CenterX, sixTop + (0.04f * g.W), body.Width);
            float dy6 = sixTop + (0.08f * g.W) - six.Slots[0].Top;
            for (int i = 0; i < six.Slots.Count; i++)
            {
                SlotLook look = i < looks.Length - 1 ? looks[i] : new SlotLook();
                SlotPainter.Slot(p, six.Slots[i].Offset(0f, dy6), look, false, false, i == six.Slots.Count - 1, 10f);
            }
        }

        /// <summary>
        /// Frame 14: the booster row of the tray (spec 002 FR-014, spec 005 §3.7, §6.1) as it unlocks, step by step, in the
        /// four cream boxes of their size in play, and every tile state (charges, price, selected, disabled).
        /// </summary>
        private static void BoosterBar(SkiaPainter p)
        {
            TraySheet(p, "Booster bar", out Box body);
            ReferenceGameplayRegions g = Reference(p);
            (string Label, int Shown)[] steps = { ("Level 1–2 (hidden)", 0), ("Level 3", 1), ("Level 4", 2), ("Level 6", 3), ("Level 9+", 4) };
            string[] ids = { "extra_slot", "shuffle", "return", "bloom_burst" };
            int[] charges = { 2, 2, 1, 0 };
            int[] prices = { RemoteConfigKeys.PriceExtraSlot.Default, RemoteConfigKeys.PriceShuffle.Default, RemoteConfigKeys.PriceReturn.Default, RemoteConfigKeys.PriceBloomBurst.Default };

            // The boxes at their size in play, unless the rows do not fit the sheet: then all of them a little smaller.
            float w = g.W;
            float label = 0.065f * w;
            float gap = 0.07f * w;
            float pill = 0.05f * w;
            float side = g.Boosters[0].Height;
            float needed = label + ((steps.Length - 1) * (label + side + gap)) + (label + side + pill);
            float k = Math.Min(1f, body.Height / needed);
            label *= k;
            gap *= k;
            side *= k;
            Box Place(int i, float cy) => Box.FromCenter(g.Boosters[i].CenterX, cy, g.Boosters[i].Width * k, side);

            float top = body.Top;
            for (int s = 0; s < steps.Length; s++)
            {
                if (s > 0)
                {
                    SheetLine(p, body, top - (gap / 2f));
                }

                p.TextLeft(steps[s].Label, body.Left, top + (label * 0.45f), T.Caption, C.InkBrownSoft);
                if (steps[s].Shown == 0)
                {
                    top += label + (gap / 2f);
                    continue;
                }

                float cy = top + label + (side / 2f);
                for (int i = 0; i < steps[s].Shown; i++)
                {
                    BoosterBarPainter.Tile(p, Place(i, cy), ids[i], new BoosterTileState(charges[i], prices[i], false, true, true), null);
                }

                top += label + side + gap;
            }

            // Every tile state of spec 003 FR-031: charges, price, selected (Return), disabled.
            var states = new (string Label, string Id, BoosterTileState State)[]
            {
                ("Charges", "extra_slot", new BoosterTileState(3, prices[0], false, true, true)),
                ("Price", "shuffle", new BoosterTileState(0, prices[1], false, true, true)),
                ("Selected", "return", new BoosterTileState(1, prices[2], true, true, true)),
                ("Disabled", "bloom_burst", new BoosterTileState(0, prices[3], false, true, false)),
            };
            SheetLine(p, body, top - (gap / 2f));
            float stateCy = top + label + (side / 2f);
            for (int i = 0; i < states.Length; i++)
            {
                Box place = Place(i, stateCy);
                StateLabel(p, states[i].Label, place.CenterX, top + (label * 0.45f), place.Width * 1.3f);
                BoosterBarPainter.Tile(p, place, states[i].Id, states[i].State, null);
            }
        }
    }
}
