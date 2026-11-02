using System;
using System.Collections.Generic;

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

        /// <summary>A copy of this shape between other ends (the same colours, thickness and parts).</summary>
        public LinkTubeShape WithEnds((float X, float Y) start, (float X, float Y) end) =>
            new LinkTubeShape(start, StartColor, end, EndColor, Thickness, StartCollar, EndCollar, Coupling);

        /// <summary>
        /// The box the shape covers on screen when it lies level or upright (a route's pieces): <see cref="Box"/>, turned a
        /// quarter when upright.
        /// </summary>
        public Box AxisBox => Math.Abs(Math.Abs(Degrees) - 90f) < 1f ? Box.FromCenter(Middle.X, Middle.Y, Thickness, Math.Max(1f, Length)) : Box;

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
    /// A routed link tube (<see cref="LinkTube.HiddenRoute"/>) to a partner the tray does not show: its level and upright
    /// <see cref="Pieces"/> in drawing order; <see cref="Connected"/> when it plugs into the partner's place in the layout,
    /// else it runs down out of the screen; <see cref="End"/> is where it plugs in or leaves the screen.
    /// </summary>
    public readonly struct LinkRoute
    {
        public LinkRoute(IReadOnlyList<LinkTubeShape> pieces, bool connected, (float X, float Y) end)
        {
            Pieces = pieces;
            Connected = connected;
            End = end;
        }

        public IReadOnlyList<LinkTubeShape> Pieces { get; }

        public bool Connected { get; }

        public (float X, float Y) End { get; }
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

        // ---- A partner deeper than the rows show (the owner's review of 2026-11-02, L54; RoundAnimator.LinkMarks) ----

        /// <summary>The routed tube's asset slot (<see cref="HiddenRoute"/>): a tube to a link partner the tray does not show.</summary>
        public const string MarkSlot = "pod.link.away";

        /// <summary>
        /// The tube to a link partner deeper than the rows show (the owner, 2026-11-02: "if there is a layout behind the screen,
        /// the link must really link them; if there is nothing there, the tube must still be there and go down"), never across a
        /// shown pod: out of the shown pod cube <paramref name="shown"/> at a tube's height on its side towards the partner, level
        /// into the gap beside it at <paramref name="gapX"/>, and down that gap;
        /// <list type="bullet">
        /// <item><description>when the partner's place in the layout <paramref name="place"/> (its row under the shown ones,
        /// <see cref="ReferenceGameplayRegions.Pod"/>) is on the screen (its tube height above <paramref name="bottom"/>, the
        /// screen's bottom edge): down to that height and level into the place's side (<see cref="LinkRoute.Connected"/>);</description></item>
        /// <item><description>else down to <paramref name="under"/> (under the shown rows), level under the rows to the gap beside
        /// the partner's column at <paramref name="partnerGapX"/> and down out of the screen.</description></item>
        /// </list>
        /// Several routes in one gap lie a lane apart (<paramref name="lane"/> of <paramref name="lanes"/>). Its pieces have round
        /// ends that overlap at the corners and a collar only where they plug into a pod or a place, all in the shown pod's
        /// <paramref name="color"/>; draw them before the pods, like a tube.
        /// </summary>
        public static LinkRoute HiddenRoute(Box shown, Box place, float gapX, float partnerGapX, float under, float bottom, Rgba color, int lane = 0, int lanes = 1)
        {
            float side = Math.Min(shown.Width, shown.Height);
            float thickness = Math.Max(1f, side * CubeThicknessShare);
            bool toRight = place.CenterX > shown.CenterX;
            float dir = toRight ? 1f : -1f;
            float offset = (lane - ((Math.Max(1, lanes) - 1) / 2f)) * thickness * 0.95f;
            float x = gapX + offset;
            float round = thickness * 0.36f;
            (float X, float Y) start = Anchor(shown, toRight, CubeFrontShare);
            float placeY = FrontY(place, CubeFrontShare);
            var pieces = new List<LinkTubeShape>(4)
            {
                Level(start, x + (dir * round), color, thickness, startCollar: true, endCollar: false),
            };

            if (placeY - (thickness / 2f) < bottom)
            {
                (float X, float Y) end = Anchor(place, !toRight, CubeFrontShare);
                pieces.Add(new LinkTubeShape((x, start.Y - round), color, (x, placeY + round), color, thickness, false, false, false));
                pieces.Add(Level((x - (dir * round), placeY), end.X, color, thickness, startCollar: false, endCollar: true));
                return new LinkRoute(pieces, true, end);
            }

            float y = under - (dir * offset);
            float x2 = partnerGapX + offset;
            float off = bottom + thickness;
            if (Math.Abs(x2 - x) < 0.5f)
            {
                pieces.Add(new LinkTubeShape((x, start.Y - round), color, (x, off), color, thickness, false, false, false));
                return new LinkRoute(pieces, false, (x, off));
            }

            pieces.Add(new LinkTubeShape((x, start.Y - round), color, (x, y + round), color, thickness, false, false, false));
            pieces.Add(Level((x - (dir * round), y), x2 + (dir * round), color, thickness, startCollar: false, endCollar: false));
            pieces.Add(new LinkTubeShape((x2, y - round), color, (x2, off), color, thickness, false, false, false));
            return new LinkRoute(pieces, false, (x2, off));
        }

        /// <summary>
        /// The middle of the gap between column <paramref name="column"/> and its neighbour on the right (<paramref name="toRight"/>)
        /// or on the left, in <paramref name="regions"/>' columns.
        /// </summary>
        public static float GapX(ReferenceGameplayRegions regions, int column, bool toRight)
        {
            Box here = regions.Columns[column];
            int next = column + (toRight ? 1 : -1);
            if (next < 0 || next >= regions.Columns.Count)
            {
                return toRight ? here.Right + (here.Width * RoundScreenLook.ColumnGap / 2f) : here.Left - (here.Width * RoundScreenLook.ColumnGap / 2f);
            }

            Box there = regions.Columns[next];
            return toRight ? (here.Right + there.Left) / 2f : (there.Right + here.Left) / 2f;
        }

        /// <summary>
        /// Where column <paramref name="column"/>'s row <paramref name="row"/> stands as a pod cube, also under the shown rows: the
        /// layout's next rows one pitch after another (<see cref="ReferenceGameplayRegions.Pod"/>).
        /// </summary>
        public static Box PlaceOf(ReferenceGameplayRegions regions, int column, int row) => PodChip.Cube(regions.Pod(column, row)).Frame;

        /// <summary>
        /// The routed tube of link mark <paramref name="mark"/> (<see cref="RoundAnimator.LinkMarks"/>) from the shown pod's
        /// cube <paramref name="shown"/> in <paramref name="regions"/>: its bottom the screen's (<see cref="ReferenceGameplayRegions.Tray"/>),
        /// its run under the rows at the first hidden row's tube height, kept under the shown rows and on the screen where it can be.
        /// </summary>
        public static LinkRoute HiddenRoute(ReferenceGameplayRegions regions, RoundLinkMark mark, Box shown)
        {
            float bottom = regions.Tray.Bottom;
            float thickness = Math.Min(shown.Width, shown.Height) * CubeThicknessShare;
            float lastBottom = PlaceOf(regions, mark.Column, regions.PodRows - 1).Bottom;
            float firstHidden = FrontY(PlaceOf(regions, mark.Column, regions.PodRows), CubeFrontShare);
            float under = Math.Max(lastBottom + thickness, Math.Min(firstHidden, bottom - thickness));
            return HiddenRoute(shown, PlaceOf(regions, mark.Column, mark.Row), GapX(regions, mark.ShownColumn, mark.ToRight), GapX(regions, mark.Column, !mark.ToRight),
                under, bottom, RoundLook.ColorOf(mark.Material), mark.Lane, mark.Lanes);
        }

        private static LinkTubeShape Level((float X, float Y) from, float toX, Rgba color, float thickness, bool startCollar, bool endCollar)
        {
            // The picture's start is its left end.
            bool forward = toX >= from.X;
            var to = (toX, from.Y);
            return forward
                ? new LinkTubeShape(from, color, to, color, thickness, startCollar, endCollar, false)
                : new LinkTubeShape(to, color, from, color, thickness, endCollar, startCollar, false);
        }
    }
}
