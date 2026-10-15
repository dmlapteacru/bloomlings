namespace Bloomlings.AssetPack;

/// <summary>Shipping sizes by use, relative to the 1024 px UI quality setting. Masters are never changed.</summary>
public static class PhoneTextureProfile
{
    public static int MaxEdge(string logical, int uiEdge)
    {
        int edge = logical switch
        {
            // Atlas cells must remain large enough after cropping. Animated indexed frames are never resized.
            _ when logical.StartsWith("Assets/heromotion/", StringComparison.Ordinal) => int.MaxValue,
            "Assets/navigation/plus-v2.png" => 256,
            "Assets/navigation/wood-selected-v1.png" => 640,
            "Assets/shop/profile-leaf-v1.png" => 512,
            "Assets/shop/profile-gold-v1.png" => 768,
            "Assets/shop/noads-basket-v2.png" => 640,
            _ when logical.StartsWith("Assets/navigation/", StringComparison.Ordinal)
                || logical.StartsWith("Assets/shop/", StringComparison.Ordinal)
                || logical.StartsWith("Assets/gameplay/", StringComparison.Ordinal) => 1024,
            _ when logical.StartsWith("Assets/avatars/", StringComparison.Ordinal)
                || logical.StartsWith("Assets/badges/", StringComparison.Ordinal) => 256,
            _ when logical.StartsWith("Assets/icons/", StringComparison.Ordinal)
                || logical.StartsWith("Assets/decor/", StringComparison.Ordinal) => 384,
            _ when logical.StartsWith("Assets/characters/", StringComparison.Ordinal) => logical.EndsWith("/group.png", StringComparison.Ordinal) ? 1024 : 512,
            _ when logical.StartsWith("Assets/brand/", StringComparison.Ordinal) => 768,
            // Full-screen gardens retain substantially more resolution than icons. Home's sharp courtyard stays intact.
            "Assets/backgrounds/home.jpg" => 1846,
            _ when logical.StartsWith("Assets/backgrounds/", StringComparison.Ordinal) => 1600,
            _ => int.MaxValue,
        };
        return edge == int.MaxValue ? edge : Math.Max(1, (int)Math.Round(edge * (uiEdge / 1024.0)));
    }

    public static (int Width, int Height) Size(string logical, int width, int height, int uiEdge)
    {
        double scale = Math.Min(1, MaxEdge(logical, uiEdge) / (double)Math.Max(width, height));
        return (Math.Max(1, (int)Math.Round(width * scale)), Math.Max(1, (int)Math.Round(height * scale)));
    }
}
