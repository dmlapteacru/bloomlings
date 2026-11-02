using System;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>
    /// One link tube (<see cref="LinkTube"/>): a straight hose from <see cref="Start"/> (the left pod's front, in
    /// <see cref="StartColor"/>) to <see cref="End"/> (the right pod's front, in <see cref="EndColor"/>), <see cref="Thickness"/>
    /// thick. Hosts draw <see cref="UiRaster.LinkTube"/> upright in <see cref="Box"/>, turned clockwise by
    /// <see cref="Degrees"/> about its middle; the colours meet at <see cref="Middle"/>. Screen pixels, y down.
    /// </summary>
    public readonly struct LinkTubeShape
    {
        public LinkTubeShape((float X, float Y) start, Rgba startColor, (float X, float Y) end, Rgba endColor, float thickness,
            bool startCollar = true, bool endCollar = true, bool coupling = true)
        {
            Start = start;
            StartColor = startColor;
            End = end;
            EndColor = endColor;
            Thickness = thickness;
            StartCollar = startCollar;
            EndCollar = endCollar;
            Coupling = coupling;
        }

        /// <summary>Whether the picture has its collar at <see cref="Start"/> (a plug into a pod or a place); a route's inner piece has none.</summary>
        public bool StartCollar { get; }

        /// <summary>Whether the picture has its collar at <see cref="End"/>.</summary>
        public bool EndCollar { get; }

        /// <summary>Whether the picture has the cream coupling ring in its middle (a whole tube); a route's pieces have none.</summary>
        public bool Coupling { get; }

        /// <summary>The left end, where the tube plugs into the first pod's front.</summary>
        public (float X, float Y) Start { get; }

        /// <summary>The colour of the half nearer <see cref="Start"/>.</summary>
        public Rgba StartColor { get; }

        /// <summary>The right end, where the tube plugs into the second pod's front.</summary>
        public (float X, float Y) End { get; }

        /// <summary>The colour of the half nearer <see cref="End"/>.</summary>
        public Rgba EndColor { get; }

        /// <summary>The picture's thickness, the collars included.</summary>
        public float Thickness { get; }

        public float Length => (float)Math.Sqrt(((End.X - Start.X) * (End.X - Start.X)) + ((End.Y - Start.Y) * (End.Y - Start.Y)));

        /// <summary>Where the two halves meet (the coupling ring).</summary>
        public (float X, float Y) Middle => ((Start.X + End.X) / 2f, (Start.Y + End.Y) / 2f);

        /// <summary>The clockwise turn of the picture from horizontal (y down), between -90° and 90°.</summary>
        public float Degrees => (float)(Math.Atan2(End.Y - Start.Y, End.X - Start.X) * 180.0 / Math.PI);

        /// <summary>The picture's box before it is turned: <see cref="Length"/> × <see cref="Thickness"/> round <see cref="Middle"/>.</summary>
        public Box Box => Box.FromCenter(Middle.X, Middle.Y, Math.Max(1f, Length), Thickness);

        /// <summary>The point at <paramref name="t"/> (0 at <see cref="Start"/>, 1 at <see cref="End"/>) along the tube.</summary>
        public (float X, float Y) At(float t) => (Start.X + ((End.X - Start.X) * t), Start.Y + ((End.Y - Start.Y) * t));

        /// <summary>The tube's colour at <paramref name="t"/>: the half nearer each pod is that pod's colour (FR-043).</summary>
        public Rgba ColorAt(float t) => t < 0.5f ? StartColor : EndColor;

        /// <summary>The picture's cache key (its colours; the size comes with the box).</summary>
        public string PictureKey => LinkTube.Slot + "/" + StartColor.Hex + "/" + EndColor.Hex
            + (StartCollar && EndCollar && Coupling ? string.Empty : "/" + (StartCollar ? "s" : "-") + (EndCollar ? "e" : "-") + (Coupling ? "c" : "-"));

        /// <summary>The tube's picture at <paramref name="width"/> × <paramref name="height"/> pixels (<see cref="UiRaster.LinkTube"/> with its parts).</summary>
        public byte[] Picture(int width, int height) => UiRaster.LinkTube(width, height, StartColor, EndColor, StartCollar, EndCollar, Coupling);
    }

    /// <summary>
    /// The tube that joins two linked pods of a round level (spec 006 FR-043, contracts/presentation.md: "a tube attached to
    /// the front faces of the two linked pods, in the columns and in the places. Its half nearer each pod is that pod's
    /// colour. It is not drawn above or on top of the pods"). Presentation only.
    /// <list type="bullet">
    /// <item><description>Each end plugs into its pod's side facing the partner, low on the pod's front
    /// (<see cref="FrontShare"/> of its height above its bottom, <see cref="Anchor"/>), tucked <see cref="TuckShare"/> under
    /// its edge.</description></item>
    /// <item><description>The tube runs straight between the ends, slanted when the pods stand at different depths
    /// (<see cref="LinkTubeShape.Degrees"/>); pods in the same column (never in valid data) join along their right
    /// sides.</description></item>
    /// <item><description>Hosts draw every tube before the pods and places, so it passes behind any pod between the two and
    /// never lies over a pod.</description></item>
    /// </list>
    /// The same geometry serves the columns (<see cref="BetweenPods"/>, the pods' frames) and the places
    /// (<see cref="Between"/>, the plates). Engine-free; its picture is <see cref="UiRaster.LinkTube"/> (<c>pod.link.tube</c>).
    /// </summary>
    public static class LinkTube
    {
        /// <summary>The tube's asset slot.</summary>
        public const string Slot = "pod.link.tube";

        /// <summary>
        /// The tube picture's thickness (the collars included), as a share of the shorter pod's height (the owner, 2026-10-31:
        /// "twice as thick"; it was 0.2).
        /// </summary>
        public const float ThicknessShare = 0.4f;

        /// <summary>How high on the pod's front the tube plugs in, from its bottom, as a share of its height.</summary>
        public const float FrontShare = 0.2f;

        /// <summary>How far each end reaches under its pod's edge, as a share of the pod's height.</summary>
        public const float TuckShare = 0.03f;

        /// <summary>
        /// How high on a pod cube's front face the tube plugs in, from the cube's bottom, as a share of its side (FR-064): the
        /// middle of the cube's front face under its top (<see cref="UiRaster.CubeSide"/>, inside its margin).
        /// </summary>
        public const float CubeFrontShare = UiRaster.CubeMargin + (UiRaster.CubeSide * 0.5f);

        /// <summary>
        /// The tube's thickness between pod cubes, as a share of the smaller cube's side (the owner, 2026-10-31: "twice as
        /// thick"; it was 0.15, about the front face's height).
        /// </summary>
        public const float CubeThicknessShare = 0.3f;

        /// <summary>The height on <paramref name="body"/> (a pod's frame or a place's plate) where the tube plugs in.</summary>
        public static float FrontY(Box body) => FrontY(body, FrontShare);

        /// <summary>The height <paramref name="share"/> of <paramref name="body"/>'s height above its bottom.</summary>
        public static float FrontY(Box body, float share) => body.Bottom - (body.Height * share);

        /// <summary>Where the tube plugs into <paramref name="body"/>: on its right side or its left one, at <see cref="FrontY(Box)"/>, tucked in.</summary>
        public static (float X, float Y) Anchor(Box body, bool rightSide) => Anchor(body, rightSide, FrontShare);

        /// <summary>Where the tube plugs into <paramref name="body"/>, <paramref name="front"/> of its height above its bottom.</summary>
        public static (float X, float Y) Anchor(Box body, bool rightSide, float front)
        {
            float tuck = body.Height * TuckShare;
            return (rightSide ? body.Right - tuck : body.Left + tuck, FrontY(body, front));
        }

        /// <summary>
        /// The tube between two linked pod cubes (spec 006 FR-064: a round level's pods, and the cubes in the places), plugged
        /// into the middle of each cube's front face (<see cref="CubeFrontShare"/>), <see cref="CubeThicknessShare"/> of a cube thick.
        /// </summary>
        public static LinkTubeShape BetweenCubes(Box a, Rgba colorA, Box b, Rgba colorB) =>
            Between(a, colorA, b, colorB, CubeFrontShare, CubeThicknessShare);

        /// <summary>
        /// The tube between two linked bodies (pods' frames, or places' plates) <paramref name="a"/> in
        /// <paramref name="colorA"/> and <paramref name="b"/> in <paramref name="colorB"/>, from the side of each that faces
        /// the other; its <see cref="LinkTubeShape.Start"/> is the left one.
        /// </summary>
        public static LinkTubeShape Between(Box a, Rgba colorA, Box b, Rgba colorB) => Between(a, colorA, b, colorB, FrontShare, ThicknessShare);

        private static LinkTubeShape Between(Box a, Rgba colorA, Box b, Rgba colorB, float front, float thicknessShare)
        {
            float thickness = Math.Max(1f, Math.Min(a.Height, b.Height) * thicknessShare);
            bool overlap = a.Left < b.Right && b.Left < a.Right;
            if (overlap)
            {
                // One column (not in valid data): both ends on the right sides, the upper body first.
                bool aFirst = a.Top <= b.Top;
                Box first = aFirst ? a : b;
                Box second = aFirst ? b : a;
                return new LinkTubeShape(Anchor(first, true, front), aFirst ? colorA : colorB, Anchor(second, true, front), aFirst ? colorB : colorA, thickness);
            }

            bool aLeft = a.CenterX <= b.CenterX;
            Box leftBody = aLeft ? a : b;
            Box rightBody = aLeft ? b : a;
            return new LinkTubeShape(Anchor(leftBody, true, front), aLeft ? colorA : colorB, Anchor(rightBody, false, front), aLeft ? colorB : colorA, thickness);
        }

        /// <summary>The tube between two linked pods in the columns, attached to their frames (<see cref="PodChip.Frame"/>).</summary>
        public static LinkTubeShape BetweenPods(PodChip a, Rgba colorA, PodChip b, Rgba colorB) => Between(a.Frame, colorA, b.Frame, colorB);

        // ---- A partner deeper than the rows show (the owner's reviews of 2026-11-02, L54; RoundAnimator.LinkMarks) ----

        /// <summary>The hidden partner's tube's asset slot (<see cref="HiddenTube"/>).</summary>
        public const string MarkSlot = "pod.link.away";

        /// <summary>
        /// The tube to a link partner deeper than the rows show (the owner, 2026-11-02: "a straight tube, just under the other
        /// pods; not behind the phone, to the edge of the bottom row of pods"): one straight tube from the shown pod cube
        /// <paramref name="shown"/>, plugged in at a tube's height on its side towards the partner (its collar there), to the
        /// middle of the bottom edge of the partner column's last shown cube <paramref name="last"/>, where it ends open and
        /// round (no collar, no coupling ring); slanted when the columns differ, drawn under the pods like a tube (a pod in its
        /// way covers it), never further down. In the shown pod's <paramref name="color"/>, as thick as a tube between cubes.
        /// </summary>
        public static LinkTubeShape HiddenTube(Box shown, Box last, Rgba color)
        {
            bool toRight = last.CenterX > shown.CenterX;
            (float X, float Y) start = Anchor(shown, toRight, CubeFrontShare);
            (float X, float Y) end = (last.CenterX, last.Bottom);
            float thickness = Math.Max(1f, Math.Min(shown.Width, shown.Height) * CubeThicknessShare);

            // The picture's start is its left end: the collar goes to the shown pod's end.
            return toRight
                ? new LinkTubeShape(start, color, end, color, thickness, true, false, false)
                : new LinkTubeShape(end, color, start, color, thickness, false, true, false);
        }

        /// <summary>The cube of column <paramref name="column"/>'s row <paramref name="row"/> in <paramref name="regions"/> (<see cref="ReferenceGameplayRegions.Pod"/>).</summary>
        public static Box CubeOf(ReferenceGameplayRegions regions, int column, int row) => PodChip.Cube(regions.Pod(column, row)).Frame;

        /// <summary>
        /// The tube of link mark <paramref name="mark"/> (<see cref="RoundAnimator.LinkMarks"/>) from the shown pod's cube
        /// <paramref name="shown"/> to the bottom edge of the partner column's last shown row in <paramref name="regions"/>.
        /// </summary>
        public static LinkTubeShape HiddenTube(ReferenceGameplayRegions regions, RoundLinkMark mark, Box shown) =>
            HiddenTube(shown, CubeOf(regions, mark.Column, regions.PodRows - 1), RoundLook.ColorOf(mark.Material));

        /// <summary>The open end of a hidden partner's tube (<see cref="HiddenTube"/>): where it meets the partner column's bottom edge.</summary>
        public static (float X, float Y) OpenEnd(LinkTubeShape tube) => tube.StartCollar ? tube.End : tube.Start;
    }
}
