using System;

namespace Bloomlings.ArtGen
{
    /// <summary>A small 3D vector for the hero raymarcher.</summary>
    public readonly struct V3
    {
        public readonly double X;
        public readonly double Y;
        public readonly double Z;

        public V3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static V3 operator +(V3 a, V3 b) => new V3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static V3 operator -(V3 a, V3 b) => new V3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static V3 operator -(V3 a) => new V3(-a.X, -a.Y, -a.Z);

        public static V3 operator *(V3 a, double s) => new V3(a.X * s, a.Y * s, a.Z * s);

        public static V3 operator *(V3 a, V3 b) => new V3(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

        public static V3 operator /(V3 a, V3 b) => new V3(a.X / b.X, a.Y / b.Y, a.Z / b.Z);

        public double Length => Math.Sqrt((X * X) + (Y * Y) + (Z * Z));

        public V3 Normalized => this * (1.0 / Length);

        public static double Dot(V3 a, V3 b) => (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);

        public static V3 Cross(V3 a, V3 b) => new V3((a.Y * b.Z) - (a.Z * b.Y), (a.Z * b.X) - (a.X * b.Z), (a.X * b.Y) - (a.Y * b.X));

        public static V3 Mix(V3 a, V3 b, double t) => a + ((b - a) * t);

        public V3 WithXY((double X, double Y) xy) => new V3(xy.X, xy.Y, Z);

        public V3 WithYZ((double Y, double Z) yz) => new V3(X, yz.Y, yz.Z);

        public V3 WithXZ((double X, double Z) xz) => new V3(xz.X, Y, xz.Z);
    }

    /// <summary>A fixed 2D rotation, as GLSL <c>rot(a) * v</c> with <c>mat2(c, -s, s, c)</c>, with its sine and cosine computed once.</summary>
    public readonly struct Rotation
    {
        public readonly double C;
        public readonly double S;

        public Rotation(double angle)
        {
            C = Math.Cos(angle);
            S = Math.Sin(angle);
        }

        public (double X, double Y) Apply(double x, double y) => ((C * x) + (S * y), (-S * x) + (C * y));
    }

    /// <summary>Signed distances, noise and helpers of the hero raymarcher (the GLSL concept's library, ported).</summary>
    public static class Sdf
    {
        public static double Clamp(double v, double lo, double hi) => v < lo ? lo : v > hi ? hi : v;

        public static double Mix(double a, double b, double t) => a + ((b - a) * t);

        public static double Fract(double v) => v - Math.Floor(v);

        public static double Smoothstep(double e0, double e1, double x)
        {
            double t = Clamp((x - e0) / (e1 - e0), 0, 1);
            return t * t * (3 - (2 * t));
        }

        /// <summary>Rotates a 2D pair as GLSL <c>rot(a) * v</c> with <c>mat2(c, -s, s, c)</c> does.</summary>
        public static (double X, double Y) Rot(double a, double x, double y)
        {
            double c = Math.Cos(a);
            double s = Math.Sin(a);
            return ((c * x) + (s * y), (-s * x) + (c * y));
        }

        public static double Hash(V3 p)
        {
            double x = Fract((p.X * 0.3183099) + 0.1) * 17;
            double y = Fract((p.Y * 0.3183099) + 0.1) * 17;
            double z = Fract((p.Z * 0.3183099) + 0.1) * 17;
            return Fract(x * y * z * (x + y + z));
        }

        public static double Noise(V3 p)
        {
            double ix = Math.Floor(p.X);
            double iy = Math.Floor(p.Y);
            double iz = Math.Floor(p.Z);
            double fx = p.X - ix;
            double fy = p.Y - iy;
            double fz = p.Z - iz;
            fx = fx * fx * (3 - (2 * fx));
            fy = fy * fy * (3 - (2 * fy));
            fz = fz * fz * (3 - (2 * fz));
            double H(double dx, double dy, double dz) => Hash(new V3(ix + dx, iy + dy, iz + dz));
            return Mix(
                Mix(Mix(H(0, 0, 0), H(1, 0, 0), fx), Mix(H(0, 1, 0), H(1, 1, 0), fx), fy),
                Mix(Mix(H(0, 0, 1), H(1, 0, 1), fx), Mix(H(0, 1, 1), H(1, 1, 1), fx), fy),
                fz);
        }

        public static double Fbm(V3 p)
        {
            double a = 0.5;
            double s = 0;
            for (int i = 0; i < 4; i++)
            {
                s += a * Noise(p);
                p = p * 2.03;
                a *= 0.5;
            }

            return s;
        }

        public static double Smin(double a, double b, double k)
        {
            double h = Clamp(0.5 + (0.5 * (b - a) / k), 0, 1);
            return Mix(b, a, h) - (k * h * (1 - h));
        }

        public static double Ellipsoid(V3 p, V3 r)
        {
            double k0 = (p / r).Length;
            double k1 = (p / (r * r)).Length;
            return k0 * (k0 - 1) / Math.Max(k1, 1e-5);
        }

        public static double Capsule(V3 p, V3 a, V3 b, double r)
        {
            V3 pa = p - a;
            V3 ba = b - a;
            double h = Clamp(V3.Dot(pa, ba) / V3.Dot(ba, ba), 0, 1);
            return (pa - (ba * h)).Length - r;
        }

        public static double RoundCone(V3 p, double r1, double r2, double h)
        {
            double b = (r1 - r2) / h;
            double a = Math.Sqrt(1 - (b * b));
            double qx = Math.Sqrt((p.X * p.X) + (p.Z * p.Z));
            double qy = p.Y;
            double k = (qx * -b) + (qy * a);
            if (k < 0)
            {
                return Math.Sqrt((qx * qx) + (qy * qy)) - r1;
            }

            if (k > a * h)
            {
                return Math.Sqrt((qx * qx) + ((qy - h) * (qy - h))) - r2;
            }

            return (qx * a) + (qy * b) - r1;
        }

        public static double RoundCylinder(V3 p, double r, double h, double rr)
        {
            double dx = Math.Sqrt((p.X * p.X) + (p.Z * p.Z)) - r + rr;
            double dy = Math.Abs(p.Y) - h + rr;
            double outside = Math.Sqrt((Math.Max(dx, 0) * Math.Max(dx, 0)) + (Math.Max(dy, 0) * Math.Max(dy, 0)));
            return Math.Min(Math.Max(dx, dy), 0) + outside - rr;
        }

        /// <summary>A thin leaf: a lens in its plane (y along, x across), curled by <paramref name="curl"/>.</summary>
        public static double Leaf(V3 q, double len, double wid, double curl, double th)
        {
            double u = q.Y;
            double v = q.X;
            double w = q.Z - (curl * v * v) - (0.25 * curl * u * u);
            double t = Clamp(u / len, 0, 1);
            double half = wid * Math.Sin(t * Math.PI) * (1 - (0.25 * t));
            double edge = Math.Abs(v) - half;
            double ends = Math.Max(-u, u - len);
            return Math.Max(Math.Max(edge, ends) * 0.7, Math.Abs(w) - th);
        }

        /// <summary>sRGB (gamma 2.2) to linear, per channel.</summary>
        public static V3 Linear(double r, double g, double b) => new V3(Math.Pow(r, 2.2), Math.Pow(g, 2.2), Math.Pow(b, 2.2));
    }
}
