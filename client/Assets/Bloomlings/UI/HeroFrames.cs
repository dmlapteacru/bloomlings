using System.Collections.Generic;
using Bloomlings.Client.UI.Design;
using Bloomlings.Core.Variants;
using UnityEngine;

namespace Bloomlings.Client.UI
{
    /// <summary>
    /// The owner's animated heroes in Unity (spec 005 FR-028, <see cref="HeroMotion"/>; slots
    /// <c>char.hero3d.motion.{family}</c>): the flat frames <c>tools/heroanim</c> pre-renders into
    /// <c>Art/Heroes/Resources/HeroMotion/</c> (constitution VII: pictures only, no 3D model in the game), one
    /// <see cref="HeroFrameSet"/> per family. A frame is loaded the first time it shows (a sprite when the importer made one,
    /// else its texture as a sprite) and kept while a view holds its family (<see cref="HeroFrameSet.Hold"/>); when the last
    /// view lets go (its screen hides or closes), the family's textures are unloaded. Home holds four families, the win and
    /// the milestone one, every other screen none, so at most the families on screen stay loaded, as compressed textures,
    /// never all 576 frames decoded.
    /// </summary>
    public static class HeroFrames
    {
        private static readonly Dictionary<Family, HeroFrameSet> Sets = new Dictionary<Family, HeroFrameSet>();

        /// <summary>The frames of a family (its crops and head points copied from the kit once).</summary>
        public static HeroFrameSet Of(Family family)
        {
            if (!Sets.TryGetValue(family, out HeroFrameSet? set))
            {
                set = new HeroFrameSet(family);
                Sets[family] = set;
            }

            return set;
        }

        /// <summary>Whether a family's frames can show: both clips baked and its seam picture (the idle's first frame) present.</summary>
        public static bool Has(Family family) => Of(family).Present;
    }

    /// <summary>
    /// One family's frames (<see cref="HeroFrames.Of"/>): the kit's crops and head points per clip and frame, cached so a
    /// view looks one up every display frame without allocating, and the frame pictures, loaded on first use.
    /// </summary>
    public sealed class HeroFrameSet
    {
        private readonly HeroFrame[] _idle;
        private readonly HeroFrame[] _react;
        private readonly HeroFrame[] _win;
        private readonly Sprite?[] _sprites;
        private readonly bool[] _tried;
        private readonly bool[] _created;
        private int _holders;

        internal HeroFrameSet(Family family)
        {
            Family = family;
            bool baked = HeroMotion.Has(family);
            _idle = Frames(family, MotionClip.Idle, baked);
            _react = Frames(family, MotionClip.React, baked);
            _win = Frames(family, MotionClip.Win, baked);
            int count = _idle.Length + _react.Length + _win.Length;
            _sprites = new Sprite?[count];
            _tried = new bool[count];
            _created = new bool[count];
        }

        public Family Family { get; }

        /// <summary>Whether both clips were baked (<see cref="HeroMotion.Has"/>).</summary>
        public bool Baked => _idle.Length > 0 && _react.Length > 0;

        /// <summary>Whether the frames can show: baked, and the seam picture is there.</summary>
        public bool Present => Baked && Sprite(MotionClip.Idle, 0) != null;

        /// <summary>The number of frames of a clip (0 for a win's celebration the family does not have).</summary>
        public int Count(MotionClip clip) => FramesOf(clip).Length;

        /// <summary>A frame's crop and head points; the index wraps (the kit's <see cref="HeroMotion.Frame"/>, cached).</summary>
        public HeroFrame Frame(MotionClip clip, int index)
        {
            HeroFrame[] frames = FramesOf(clip);
            return frames[Wrap(index, frames.Length)];
        }

        /// <summary>A frame's place among the family's frames (the idle's, then the reaction's, then the win's): its picture's key.</summary>
        public int Slot(MotionClip clip, int index) =>
            (clip == MotionClip.Idle ? 0 : clip == MotionClip.React ? _idle.Length : _idle.Length + _react.Length) + Wrap(index, Count(clip));

        private HeroFrame[] FramesOf(MotionClip clip) => clip == MotionClip.Idle ? _idle : clip == MotionClip.React ? _react : _win;

        /// <summary>A frame's picture, loaded the first time it is asked for; null while it is missing.</summary>
        public Sprite? Sprite(MotionClip clip, int index)
        {
            int count = Count(clip);
            if (count == 0)
            {
                return null;
            }

            int slot = Slot(clip, index);
            if (!_tried[slot])
            {
                _tried[slot] = true;
                (_sprites[slot], _created[slot]) = Load(Frame(clip, index).Name);
            }

            return _sprites[slot];
        }

        /// <summary>
        /// A view starts (<paramref name="on"/>) or stops showing this family. When the last view stops, the loaded
        /// textures are unloaded (a later frame loads again).
        /// </summary>
        public void Hold(bool on)
        {
            if (on)
            {
                _holders++;
                return;
            }

            _holders = Mathf.Max(0, _holders - 1);
            if (_holders == 0)
            {
                Unload();
            }
        }

        private void Unload()
        {
            for (int i = 0; i < _sprites.Length; i++)
            {
                Sprite? sprite = _sprites[i];
                if (sprite != null)
                {
                    Texture2D texture = sprite.texture;
                    if (_created[i])
                    {
                        Object.Destroy(sprite);
                    }

                    if (texture != null)
                    {
                        Resources.UnloadAsset(texture);
                    }
                }

                _sprites[i] = null;
                _tried[i] = false;
                _created[i] = false;
            }
        }

        /// <summary>A frame picture by name: the importer's sprite, else its texture as a sprite (then destroyed on unload).</summary>
        private static (Sprite? Sprite, bool Created) Load(string name)
        {
            string path = HeroMotion.Folder + "/" + name;
            Sprite? sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return (sprite, false);
            }

            Texture2D? texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return (null, false);
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, Vector4.zero);
            sprite.name = name;
            return (sprite, true);
        }

        private static HeroFrame[] Frames(Family family, MotionClip clip, bool baked)
        {
            int count = baked ? HeroMotion.FrameCount(family, clip) : 0;
            var frames = new HeroFrame[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = HeroMotion.Frame(family, clip, i);
            }

            return frames;
        }

        private static int Wrap(int index, int count) => count <= 0 ? 0 : ((index % count) + count) % count;
    }
}
