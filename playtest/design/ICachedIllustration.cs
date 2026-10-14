using System;
using Bloomlings.Client.UI.Design;

namespace Bloomlings.Playtest.Design
{
    /// <summary>Caches a noninteractive illustration drawn at full alpha without a parent transform. The draw must have no side effects.</summary>
    public interface ICachedIllustration
    {
        void Illustration(string key, Box bounds, Action draw);
    }
}
