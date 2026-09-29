using System;
using System.Collections.Generic;
using Bloomlings.Core.Definitions;
using Bloomlings.Core.Variants;

namespace Bloomlings.Content.Json
{
    /// <summary>Two-way mapping between enum values and the lowercase wire names used in the JSON schemas.</summary>
    internal static class WireNames
    {
        public static readonly EnumNames<ColorGroup> ColorGroups = new EnumNames<ColorGroup>(
            (ColorGroup.Green, "green"),
            (ColorGroup.PinkPurple, "pink_purple"),
            (ColorGroup.BlueCyan, "blue_cyan"),
            (ColorGroup.BrownOrange, "brown_orange"),
            (ColorGroup.Lime, "lime"),
            (ColorGroup.Red, "red"),
            (ColorGroup.Indigo, "indigo"),
            (ColorGroup.Gold, "gold"));

        public static readonly EnumNames<Mirror> Mirrors = new EnumNames<Mirror>(
            (Mirror.None, "none"),
            (Mirror.Horizontal, "horizontal"));

        public static readonly EnumNames<EntrySide> EntrySides = new EnumNames<EntrySide>(
            (EntrySide.Bottom, "bottom"),
            (EntrySide.Left, "left"),
            (EntrySide.Right, "right"),
            (EntrySide.Top, "top"));

        public static readonly EnumNames<SpecialType> SpecialTypes = new EnumNames<SpecialType>(
            (SpecialType.Gate, "gate"),
            (SpecialType.Fountain, "fountain"),
            (SpecialType.Chest, "chest"),
            (SpecialType.Statue, "statue"),
            (SpecialType.Bridge, "bridge"));

        public static readonly EnumNames<SpecialConditionKind> ConditionKinds = new EnumNames<SpecialConditionKind>(
            (SpecialConditionKind.Key, "key"),
            (SpecialConditionKind.ClearCountAdjacent, "clear_count_adjacent"),
            (SpecialConditionKind.ClearRegion, "clear_region"));

        public static readonly EnumNames<SpecialEffectKind> EffectKinds = new EnumNames<SpecialEffectKind>(
            (SpecialEffectKind.OpenCells, "open_cells"),
            (SpecialEffectKind.RevealLayers, "reveal_layers"),
            (SpecialEffectKind.RemoveStones, "remove_stones"));

        public static readonly EnumNames<LockTargetKind> LockTargets = new EnumNames<LockTargetKind>(
            (LockTargetKind.Pod, "pod"),
            (LockTargetKind.Slot, "slot"),
            (LockTargetKind.Special, "special"));

        public static readonly EnumNames<DifficultyClass> DifficultyClasses = new EnumNames<DifficultyClass>(
            (DifficultyClass.Normal, "normal"),
            (DifficultyClass.Hard, "hard"),
            (DifficultyClass.SuperHard, "super_hard"));

        public static readonly EnumNames<FinishedLookMode> FinishedLookModes = new EnumNames<FinishedLookMode>(
            (FinishedLookMode.Auto, "auto"),
            (FinishedLookMode.Illustration, "illustration"));

        public static readonly EnumNames<ReviewStatus> ReviewStatuses = new EnumNames<ReviewStatus>(
            (ReviewStatus.Draft, "draft"),
            (ReviewStatus.Approved, "approved"),
            (ReviewStatus.Rejected, "rejected"));

        public static readonly EnumNames<PictureSourceKind> SourceKinds = new EnumNames<PictureSourceKind>(
            (PictureSourceKind.Hand, "hand"),
            (PictureSourceKind.Generated, "generated"),
            (PictureSourceKind.GeneratedEdited, "generated_edited"));
    }

    internal sealed class EnumNames<T>
        where T : struct, Enum
    {
        private readonly Dictionary<T, string> _toWire = new Dictionary<T, string>();
        private readonly Dictionary<string, T> _fromWire = new Dictionary<string, T>(StringComparer.Ordinal);

        public EnumNames(params (T Value, string Wire)[] pairs)
        {
            foreach ((T value, string wire) in pairs)
            {
                _toWire.Add(value, wire);
                _fromWire.Add(wire, value);
            }
        }

        public string ToWire(T value) => _toWire[value];

        public bool TryParse(string wire, out T value) => _fromWire.TryGetValue(wire, out value);

        public string Allowed => string.Join(", ", _fromWire.Keys);
    }
}
