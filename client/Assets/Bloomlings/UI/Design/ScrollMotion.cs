using System;
using System.Collections.Generic;

namespace Bloomlings.Client.UI.Design
{
    /// <summary>Recent finger velocity and a bounded, frame-rate-independent glide after release. Coordinates are pixels.</summary>
    public sealed class ScrollMotion
    {
        private readonly List<(float Y, float At)> _samples = new List<(float, float)>();
        private float _velocity;
        public bool Moving => Math.Abs(_velocity) > 1f;

        public void Press(float y, float now)
        {
            Stop();
            _samples.Clear();
            Record(y, now);
        }

        public void Record(float y, float now)
        {
            _samples.Add((y, now));
            while (_samples.Count > 2 && _samples[1].At < now - .12f) _samples.RemoveAt(0);
        }

        public void Release(float y, float now, float maximumSpeed)
        {
            Record(y, now);
            var first = _samples[0];
            float dt = now - first.At;
            _velocity = dt > .008f && now - _samples[Math.Max(0, _samples.Count - 2)].At < .12f
                ? Math.Max(-maximumSpeed, Math.Min(maximumSpeed, (y - first.Y) / dt)) * .82f : 0f;
            _samples.Clear();
        }

        public float Advance(float seconds, float stopSpeed)
        {
            if (!Moving || seconds <= 0f) return 0f;
            float decay = (float)Math.Exp(-4.2f * Math.Min(seconds, .1f));
            float delta = _velocity * (1f - decay) / 4.2f;
            _velocity *= decay;
            if (Math.Abs(_velocity) < stopSpeed) Stop();
            return delta;
        }

        public void Stop() => _velocity = 0f;
    }
}
