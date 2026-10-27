using System.Collections.Generic;
using Bloomlings.Content.Rounds;
using Bloomlings.Core.Rounds;

namespace Bloomlings.Pipeline.Rounds
{
    /// <summary>
    /// The conformance replay of one level (spec 006 FR-030–FR-033, contracts/conformance-fixture.md): the solution
    /// windows, every random game and every owner attempt replayed in <c>Bloomlings.Core.Rounds</c> and compared with the
    /// reference's fixture. Returns one message per difference, in the contract's failure report format
    /// (<c>L0005 solution window 7 (tap pod 11 at 26962200 µs): events differ</c> …), or none when the level conforms.
    /// </summary>
    public interface IRoundConformance
    {
        IReadOnlyList<string> Check(RoundLevel level, RoundProof proof, RoundConformanceFixture fixture);
    }

    /// <summary>
    /// The definition rules of a round level (T008 <c>RoundLevelValidator</c>: the port of <c>rules.cjs validate</c> plus
    /// the data model's rules). Returns one message per broken rule, an exception already applied for the rule it names.
    /// </summary>
    public interface IRoundDefinitionRules
    {
        IReadOnlyList<string> Check(RoundLevel level);
    }

    /// <summary>
    /// Stands in until the replay engine (<c>RoundTimeline</c>, <c>RoundEvents</c>, <c>RoundRandomPolicy</c>, T032–T035)
    /// is merged: every level fails, so nothing is published unproven.
    /// </summary>
    public sealed class UnwiredRoundConformance : IRoundConformance
    {
        public IReadOnlyList<string> Check(RoundLevel level, RoundProof proof, RoundConformanceFixture fixture) =>
            new[] { $"L{level.Number:0000} conformance not wired: the replay engine (RoundTimeline, RoundEvents, RoundRandomPolicy) is not plugged into RoundSeams.Conformance yet" };
    }

    /// <summary>Stands in until <c>RoundLevelValidator</c> (T008) is merged: every level fails, so nothing unchecked passes.</summary>
    public sealed class UnwiredRoundDefinitionRules : IRoundDefinitionRules
    {
        public IReadOnlyList<string> Check(RoundLevel level) =>
            new[] { $"L{level.Number:0000} definition rules not wired: RoundLevelValidator (T008) is not plugged into RoundSeams.DefinitionRules yet" };
    }

    /// <summary>
    /// The pluggable parts of <c>rounds validate</c>, <c>import</c> and <c>publish</c>.
    /// TODO(after merging T008 and T032–T035): set the defaults to the real implementations, e.g.
    /// <c>DefinitionRules = new RoundLevelValidatorRules()</c> (wrapping <c>RoundLevelValidator</c>) and
    /// <c>Conformance = new RoundReplayConformance()</c> (wrapping <c>RoundTimeline</c> / <c>RoundEvents</c> /
    /// <c>RoundRandomPolicy</c>).
    /// </summary>
    public static class RoundSeams
    {
        public static IRoundConformance Conformance { get; set; } = new UnwiredRoundConformance();

        public static IRoundDefinitionRules DefinitionRules { get; set; } = new UnwiredRoundDefinitionRules();
    }
}
