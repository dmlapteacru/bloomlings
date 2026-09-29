using System;
using System.Globalization;
using Bloomlings.Core.Variants;

namespace Bloomlings.Core.Simulation
{
    /// <summary>
    /// Stable text form of commands for command logs, golden files and support: <c>tap:&lt;podId&gt;</c>,
    /// <c>extra_slot</c>, <c>shuffle</c>, <c>return:&lt;slot&gt;</c>, <c>burst:&lt;variant&gt;</c> and <c>restart</c>.
    /// </summary>
    public static class CommandText
    {
        public static string Format(Command command) => command switch
        {
            TapPod tap => "tap:" + tap.PodId,
            UseExtraSlot _ => "extra_slot",
            UseShuffle _ => "shuffle",
            UseReturn r => "return:" + r.SlotIndex.ToString(CultureInfo.InvariantCulture),
            UseBloomBurst b => "burst:" + b.Variant.Key,
            Restart _ => "restart",
            _ => throw new NotSupportedException($"No text form for {command.GetType().Name}."),
        };

        public static Command Parse(string text)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            int colon = text.IndexOf(':');
            string verb = colon < 0 ? text : text.Substring(0, colon);
            string? arg = colon < 0 ? null : text.Substring(colon + 1);
            switch (verb)
            {
                case "tap" when !string.IsNullOrEmpty(arg):
                    return new TapPod(arg!);
                case "extra_slot" when arg == null:
                    return new UseExtraSlot();
                case "shuffle" when arg == null:
                    return new UseShuffle();
                case "return" when arg != null && int.TryParse(arg, NumberStyles.None, CultureInfo.InvariantCulture, out int slot):
                    return new UseReturn(slot);
                case "burst" when VariantId.IsValidKey(arg):
                    return new UseBloomBurst(new VariantId(arg!));
                case "restart" when arg == null:
                    return new Restart();
                default:
                    throw new FormatException($"'{text}' is not a command.");
            }
        }
    }
}
