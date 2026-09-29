using System;

namespace Bloomlings.Core.Variants
{
    /// <summary>
    /// Exact gameplay matching type (FR-002, FR-003). Two pods match the same tiles only when their
    /// <see cref="VariantId"/> values are equal; the Bloomling family is never used for matching.
    /// </summary>
    public readonly struct VariantId : IEquatable<VariantId>, IComparable<VariantId>
    {
        public static readonly VariantId Leaf = new VariantId("leaf");
        public static readonly VariantId Moss = new VariantId("moss");
        public static readonly VariantId Flower = new VariantId("flower");
        public static readonly VariantId VioletBud = new VariantId("violet_bud");
        public static readonly VariantId Water = new VariantId("water");
        public static readonly VariantId Dew = new VariantId("dew");
        public static readonly VariantId Wood = new VariantId("wood");
        public static readonly VariantId Acorn = new VariantId("acorn");
        public static readonly VariantId Vine = new VariantId("vine");
        public static readonly VariantId Berry = new VariantId("berry");
        public static readonly VariantId Mist = new VariantId("mist");
        public static readonly VariantId Bark = new VariantId("bark");

        private readonly string? _key;

        /// <param name="key">Lowercase wire id, for example <c>violet_bud</c>.</param>
        public VariantId(string key)
        {
            if (!IsValidKey(key))
            {
                throw new ArgumentException($"Invalid variant id '{key}'. Use lowercase letters, digits and '_'.", nameof(key));
            }

            _key = key;
        }

        /// <summary>The lowercase wire id; empty for the default value.</summary>
        public string Key => _key ?? string.Empty;

        /// <summary>True for <c>default(VariantId)</c>, which never matches a real variant.</summary>
        public bool IsNone => _key == null;

        public static bool IsValidKey(string? key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (char c in key!)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok)
                {
                    return false;
                }
            }

            return true;
        }

        public bool Equals(VariantId other) => string.Equals(_key, other._key, StringComparison.Ordinal);

        public override bool Equals(object? obj) => obj is VariantId other && Equals(other);

        public override int GetHashCode() => _key == null ? 0 : StringComparer.Ordinal.GetHashCode(_key);

        public int CompareTo(VariantId other) => string.CompareOrdinal(_key, other._key);

        public override string ToString() => Key;

        public static bool operator ==(VariantId left, VariantId right) => left.Equals(right);

        public static bool operator !=(VariantId left, VariantId right) => !left.Equals(right);
    }
}
