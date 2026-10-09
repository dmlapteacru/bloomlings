using System;

namespace Bloomlings.Playtest.Design
{
    /// <summary>
    /// A painter that makes a screen's pictures ahead of it (the APK's painter; the preview draws without one): the
    /// splash warms the first screen's pictures while its ring fills, and an idle Home the next likely screens' and cards'
    /// (<see cref="DesignApp"/>), so their first frame only uploads what is made and a screen seen for the first time opens
    /// without a stall. A <see cref="IPainter.Picture"/> is a pure function of its key and size, so made early it is the
    /// same picture. Engine-free.
    /// </summary>
    public interface IPictureWarmer
    {
        /// <summary>Whether this painter warms pictures (the APK's always; the preview's only for the harness's <c>--perf-warm</c>).</summary>
        bool WarmsPictures { get; }

        /// <summary>
        /// Runs <paramref name="draw"/> on this frame without showing it or keeping its touch areas, and makes on a worker
        /// thread every picture it asks for that is not made yet.
        /// </summary>
        void Warm(Action draw);

        /// <summary>The share of the pictures asked for by <see cref="Warm"/> that are made (1 when none waits).</summary>
        float WarmProgress { get; }
    }
}
