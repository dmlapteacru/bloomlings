using System;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// What the full playtest's designed screens draw with (spec 002 research R3, contracts/painter.md).
    /// <list type="bullet">
    /// <item><description>The APK implements it over <c>Android.Graphics.Canvas</c> (<c>AndroidPainter</c>).</description></item>
    /// <item><description>The preview tool implements it over SkiaSharp (<c>SkiaPainter</c>), so the same screen code
    /// renders to PNG files and is checked without a device.</description></item>
    /// </list>
    /// Coordinates are surface pixels, y down. Colors are design tokens. Screens never touch Android or Skia types.
    /// </summary>
    public interface IPainter
    {
        float Width { get; }

        float Height { get; }

        /// <summary>Pixels per reference unit (<see cref="DesignTokens.ScaleFor"/>).</summary>
        float Scale { get; }

        /// <summary>The safe-area insets (status bar, navigation bar, cut-outs).</summary>
        Insets Insets { get; }

        void FillRect(Box box, Rgba color);

        void FillRound(Box box, float radius, Rgba color);

        /// <summary>A rounded rectangle with a vertical gradient.</summary>
        void FillRoundGradient(Box box, float radius, Rgba top, Rgba bottom);

        /// <summary>An outline; a positive <paramref name="dashOn"/> draws it dashed (the danger slot).</summary>
        void StrokeRound(Box box, float radius, float width, Rgba color, float dashOn = 0f, float dashOff = 0f);

        void FillCircle(float cx, float cy, float r, Rgba color);

        void StrokeCircle(float cx, float cy, float r, float width, Rgba color);

        /// <summary>A line with round caps.</summary>
        void Line(float x0, float y0, float x1, float y1, float width, Rgba color);

        /// <summary>A <see cref="ShapeLibrary"/> shape fitted into <paramref name="box"/> and tinted.</summary>
        void Shape(string id, Box box, Rgba color);

        /// <summary>A composite shape (a skin on a family body): its own cache key and distance function.</summary>
        void ShapeOf(string key, Func<float, float, float> sdf, Box box, Rgba color);

        /// <summary>
        /// Centered text in a board type style: bold, uppercase and outlined as the style says. It is
        /// <paramref name="sizeScale"/> times the style size, and shrinks to <paramref name="maxWidth"/> down to the
        /// style's minimum. With a <paramref name="look"/>, <paramref name="color"/> is ignored and the label is drawn
        /// with volume: shadow, extrusion, outline, then a gradient fill (spec 003 contracts/painter-text.md).
        /// </summary>
        void Text(string text, float cx, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null);

        /// <summary>Left-aligned text, vertically centered on <paramref name="cy"/>.</summary>
        void TextLeft(string text, float x, float cy, TypeStyle style, Rgba color, float maxWidth = 0f, float sizeScale = 1f, TextLook? look = null);

        /// <summary>The width of a text at the style's size times <paramref name="sizeScale"/>.</summary>
        float MeasureText(string text, TypeStyle style, float sizeScale = 1f);

        /// <summary>
        /// A generated character picture (spec 004 contracts/hosts.md), by its name in the art set (<c>2d/leaf-happy</c>,
        /// <c>3d/group</c>), fitted into <paramref name="box"/> with its aspect kept and centered. It follows the alpha
        /// and transform stacks. Pictures are decoded once and cached.
        /// </summary>
        void Sprite(string name, Box box);

        /// <summary>Whether the picture is embedded and decodes (else callers draw the spec 002 fallback, FR-021).</summary>
        bool HasSprite(string name);

        /// <summary>
        /// A cosmetic skin pattern (<see cref="ShapeLibrary.SkinPattern"/>) in <paramref name="tint"/>, drawn only where the
        /// picture <paramref name="name"/>, fitted as by <see cref="Sprite"/>, is opaque: the picture masks the pattern.
        /// </summary>
        void SpriteSkin(string name, Box box, string skinShape, Rgba tint);

        /// <summary>The garden backdrop of a theme over a box (<see cref="BackdropRaster"/>, cached by the painter).</summary>
        void Backdrop(Box box, BackdropColors colors, BackdropScene scene, string cacheKey);

        void PushClip(Box box);

        void PopClip();

        /// <summary>Multiplies the alpha of everything drawn until <see cref="PopAlpha"/>.</summary>
        void PushAlpha(float alpha);

        void PopAlpha();

        /// <summary>Translates by (dx, dy), then scales by <paramref name="scale"/> about (cx, cy), until <see cref="PopTransform"/>.</summary>
        void PushTransform(float dx, float dy, float scale, float cx, float cy);

        /// <summary>Scales by (<paramref name="sx"/>, <paramref name="sy"/>) about (cx, cy) until <see cref="PopTransform"/> (the press squash).</summary>
        void PushSquash(float sx, float sy, float cx, float cy);

        void PopTransform();

        /// <summary>A touch target; the topmost target under the finger gets the tap (FR-027: at least <c>size.touch_min</c>).</summary>
        void Hit(Box box, Action action);

        /// <summary>Whether a finger is down inside <paramref name="box"/> (the pressed look of buttons and pods).</summary>
        bool Pressed(Box box);

        /// <summary>Seconds since a finger lifted inside <paramref name="box"/>, or −1 (the spring-back, spec 003 FR-017).</summary>
        float Released(Box box);

        /// <summary>The host's clock in seconds (presses, breathing, glows).</summary>
        float Now { get; }

        /// <summary>Records that an asset slot is drawn procedurally here (the preview's inventory check; a no-op on a device).</summary>
        void Mark(string slotId);
    }

    /// <summary>The sounds and haptics the designed screens trigger (the APK plays <c>PlaytestSound</c>; the preview is silent).</summary>
    public interface ISoundOut
    {
        bool Enabled { get; set; }

        void Play(Bloomlings.Client.Services.Feedback.SoundCue cue);
    }
}
