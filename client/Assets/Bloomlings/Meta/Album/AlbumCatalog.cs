using System;
using System.Collections.Generic;
using Bloomlings.Content.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Meta.Album
{
    /// <summary>
    /// One object of a Scene Album scene (spec 007-scene-album): its id in the scene, its name's localization key, the
    /// picture code of the campaign level that opens it (<see cref="Picture"/>, the explicit binding of the owner's pack;
    /// never the title), the level the pack was drawn for (<see cref="LevelHint"/>, only a fallback where no round levels are
    /// published), its layer picture (<c>Album/{scene}/{object}</c>) and where that layer lies on the scene's canvas
    /// (<see cref="Rect"/>: x, y, width, height in the canvas's pixels; the layer is cropped to it). Objects are listed in
    /// the pack's layer order, back to front, which is also the order of the medallions.
    /// </summary>
    public sealed record AlbumObject(string Id, string TitleKey, string Picture, int LevelHint, string Layer, int X, int Y, int Width, int Height);

    /// <summary>
    /// A Scene Album scene (spec 007): its id, its name's localization key, its canvas (every layer shares it), its
    /// opaque background picture and its 3–5 objects in layer order.
    /// </summary>
    public sealed record AlbumScene(string Id, string TitleKey, int CanvasWidth, int CanvasHeight, string Background, IReadOnlyList<AlbumObject> Objects)
    {
        /// <summary>The object of that id, or null.</summary>
        public AlbumObject? Find(string id)
        {
            foreach (AlbumObject item in Objects)
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The Scene Album's scenes in album order (spec 007), read from <c>Meta/Album/Resources/AlbumCatalog.json</c>
    /// (<c>bloomlings-album-catalog.v1</c>, written by <c>tools/album/import_scene_pack.py</c> from the owner's layered pack).
    /// Each object is bound to a picture code; a picture appears at most once in the album. Engine-free.
    /// </summary>
    public sealed class AlbumCatalog
    {
        public const string Format = "bloomlings-album-catalog.v1";

        /// <summary>The resource's name (Unity <c>Resources.Load</c>; the playtest embeds it as <c>album/AlbumCatalog.json</c>).</summary>
        public const string ResourceName = "AlbumCatalog";

        /// <summary>The folder of the layer pictures (<c>Art/Album/Resources/Album/</c>).</summary>
        public const string PictureFolder = "Album";


        public AlbumCatalog(IReadOnlyList<AlbumScene> scenes)
        {
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }

        /// <summary>An album without scenes (a build without the resource).</summary>
        public static AlbumCatalog Empty { get; } = new AlbumCatalog(Array.Empty<AlbumScene>());

        public IReadOnlyList<AlbumScene> Scenes { get; }

        /// <summary>The scene of that id, or null.</summary>
        public AlbumScene? Find(string id)
        {
            foreach (AlbumScene scene in Scenes)
            {
                if (scene.Id == id)
                {
                    return scene;
                }
            }

            return null;
        }

        /// <summary>Whether <paramref name="code"/> is a picture code: <c>P</c> and four digits (<c>P0113</c>).</summary>
        public static bool IsPictureCode(string code)
        {
            if (code.Length != 5 || code[0] != 'P')
            {
                return false;
            }

            for (int i = 1; i < 5; i++)
            {
                if (code[i] < '0' || code[i] > '9')
                {
                    return false;
                }
            }

            return true;
        }

        /// <exception cref="ContentFormatException">The document is not a valid album catalog.</exception>
        public static AlbumCatalog Parse(string json)
        {
            JObject root = JsonDoc.ParseObject(json, "album");
            JsonDoc.AllowOnly(root, string.Empty, "format", "source", "locked", "scenes");
            string format = JsonDoc.String(JsonDoc.Required(root, string.Empty, "format"), "format");
            if (format != Format)
            {
                throw new ContentFormatException("format", $"expected {Format}, found {format}");
            }

            var scenes = new List<AlbumScene>();
            var sceneIds = new HashSet<string>(StringComparer.Ordinal);
            var pictures = new HashSet<string>(StringComparer.Ordinal);
            JArray list = JsonDoc.Array(JsonDoc.Required(root, string.Empty, "scenes"), "scenes");
            for (int s = 0; s < list.Count; s++)
            {
                string path = JsonDoc.Index("scenes", s);
                JObject scene = JsonDoc.Object(list[s], path);
                JsonDoc.AllowOnly(scene, path, "id", "title", "canvas", "background", "objects");
                string id = JsonDoc.Id(JsonDoc.Required(scene, path, "id"), JsonDoc.Join(path, "id"));
                if (!sceneIds.Add(id))
                {
                    throw new ContentFormatException(JsonDoc.Join(path, "id"), $"scene '{id}' appears twice");
                }

                JArray canvas = JsonDoc.Array(JsonDoc.Required(scene, path, "canvas"), JsonDoc.Join(path, "canvas"), 2, 2);
                int width = JsonDoc.Int(canvas[0], JsonDoc.Join(path, "canvas[0]"), min: 1);
                int height = JsonDoc.Int(canvas[1], JsonDoc.Join(path, "canvas[1]"), min: 1);
                JArray items = JsonDoc.Array(JsonDoc.Required(scene, path, "objects"), JsonDoc.Join(path, "objects"), 3, 5);
                var objects = new List<AlbumObject>();
                var objectIds = new HashSet<string>(StringComparer.Ordinal);
                for (int o = 0; o < items.Count; o++)
                {
                    string at = JsonDoc.Index(JsonDoc.Join(path, "objects"), o);
                    JObject item = JsonDoc.Object(items[o], at);
                    JsonDoc.AllowOnly(item, at, "id", "title", "picture", "levelHint", "layer", "rect");
                    string objectId = JsonDoc.Id(JsonDoc.Required(item, at, "id"), JsonDoc.Join(at, "id"));
                    if (!objectIds.Add(objectId))
                    {
                        throw new ContentFormatException(JsonDoc.Join(at, "id"), $"object '{objectId}' appears twice in '{id}'");
                    }

                    string picture = JsonDoc.String(JsonDoc.Required(item, at, "picture"), JsonDoc.Join(at, "picture"));
                    if (!IsPictureCode(picture) || !pictures.Add(picture))
                    {
                        throw new ContentFormatException(JsonDoc.Join(at, "picture"), $"'{picture}' is not a picture code or opens two objects");
                    }

                    JArray rect = JsonDoc.Array(JsonDoc.Required(item, at, "rect"), JsonDoc.Join(at, "rect"), 4, 4);
                    int x = JsonDoc.Int(rect[0], JsonDoc.Join(at, "rect[0]"), 0, width - 1);
                    int y = JsonDoc.Int(rect[1], JsonDoc.Join(at, "rect[1]"), 0, height - 1);
                    int w = JsonDoc.Int(rect[2], JsonDoc.Join(at, "rect[2]"), 1, width - x);
                    int h = JsonDoc.Int(rect[3], JsonDoc.Join(at, "rect[3]"), 1, height - y);
                    objects.Add(new AlbumObject(
                        objectId,
                        JsonDoc.String(JsonDoc.Required(item, at, "title"), JsonDoc.Join(at, "title")),
                        picture,
                        JsonDoc.Int(JsonDoc.Required(item, at, "levelHint"), JsonDoc.Join(at, "levelHint"), min: 1),
                        JsonDoc.String(JsonDoc.Required(item, at, "layer"), JsonDoc.Join(at, "layer")),
                        x,
                        y,
                        w,
                        h));
                }

                scenes.Add(new AlbumScene(
                    id,
                    JsonDoc.String(JsonDoc.Required(scene, path, "title"), JsonDoc.Join(path, "title")),
                    width,
                    height,
                    JsonDoc.String(JsonDoc.Required(scene, path, "background"), JsonDoc.Join(path, "background")),
                    objects));
            }

            return new AlbumCatalog(scenes);
        }
    }
}
