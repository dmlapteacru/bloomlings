using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Rounds
{
    /// <summary>The kind of a recorded round command: a pod tap or the choice of a Sprig bridge site.</summary>
    public enum RoundCommandType
    {
        Tap,
        Bridge,
    }

    /// <summary>
    /// One recorded command with its model time in microseconds (constitution III): a tap of pod <see cref="Index"/>, or
    /// the bridge choice <see cref="Index"/> (an index into the level's <c>hero.choices</c>).
    /// </summary>
    public sealed class RoundCommand : IEquatable<RoundCommand>
    {
        public RoundCommand(RoundCommandType type, int index, long atUs)
        {
            Type = type;
            Index = index;
            AtUs = atUs;
        }

        public RoundCommandType Type { get; }

        /// <summary>The pod index of a tap, or the choice index of a bridge.</summary>
        public int Index { get; }

        public long AtUs { get; }

        public bool Equals(RoundCommand? other) => other != null && other.Type == Type && other.Index == Index && other.AtUs == AtUs;

        public override bool Equals(object? obj) => Equals(obj as RoundCommand);

        public override int GetHashCode() => ((int)Type * 397) ^ (Index * 31) ^ AtUs.GetHashCode();

        public override string ToString() => Type == RoundCommandType.Tap ? $"tap pod {Index} at {AtUs} µs" : $"bridge {Index} at {AtUs} µs";
    }

    /// <summary>The saved solution: the reference's patient replay, each command when all prior work has settled.</summary>
    public sealed class RoundSolution
    {
        public RoundSolution(IReadOnlyList<RoundCommand> commands, long endUs)
        {
            Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            EndUs = endUs;
        }

        public IReadOnlyList<RoundCommand> Commands { get; }

        /// <summary>Always <c>Won</c>: a proof only records a winning solution.</summary>
        public string Outcome => "Won";

        public long EndUs { get; }
    }

    /// <summary>The random-test record of rules 17 §5 (spec 006 FR-031).</summary>
    public sealed class RoundRandomTest
    {
        /// <summary>The only policy a proof may name.</summary>
        public const string PolicyName = "17§5 extended: uniform over legal actions + wait before a state event";

        public RoundRandomTest(long seedBase, int runs, int wins, decimal share, decimal wilsonLow, decimal wilsonHigh, RoundClass classOfShare, bool? borderline = null)
        {
            SeedBase = seedBase;
            Runs = runs;
            Wins = wins;
            Share = share;
            WilsonLow = wilsonLow;
            WilsonHigh = wilsonHigh;
            ClassOfShare = classOfShare;
            Borderline = borderline;
        }

        public string Policy => PolicyName;

        public long SeedBase { get; }

        /// <summary>100, or 400 when the Wilson interval of 100 crossed a class border.</summary>
        public int Runs { get; }

        public int Wins { get; }

        public decimal Share { get; }

        public decimal WilsonLow { get; }

        public decimal WilsonHigh { get; }

        public RoundClass ClassOfShare { get; }

        public bool? Borderline { get; }
    }

    /// <summary>The audit result of a level (rules 17 §13 layer A, with the layer B notes).</summary>
    public sealed class RoundAudit
    {
        public const string Ok = "OK";

        public const string OkWithException = "OK with exception";

        public RoundAudit(string result, string report, IReadOnlyList<string>? notes = null)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Report = report ?? throw new ArgumentNullException(nameof(report));
            Notes = notes;
        }

        /// <summary><see cref="Ok"/> or <see cref="OkWithException"/>.</summary>
        public string Result { get; }

        /// <summary>The audit report's path, e.g. <c>content/rounds/audit-L0001-L0020.md</c>.</summary>
        public string Report { get; }

        public IReadOnlyList<string>? Notes { get; }
    }

    /// <summary>
    /// Why a round level is valid and how hard it is (spec 006 FR-022, data model RoundProof; wire format
    /// <c>round-proof.v1</c>). Times are model microseconds. <see cref="Checks"/> is kept as written by the reference's
    /// <c>measure.mjs</c> (opening, forced, hold, greedyLoses, hiddenMatters, bridgeKind, bridgeSingle, pressure).
    /// </summary>
    public sealed class RoundProof
    {
        /// <summary>The class checks a complete proof carries (data model RoundProof).</summary>
        public static readonly IReadOnlyList<string> CheckNames = new[]
        {
            "opening", "forced", "hold", "greedyLoses", "hiddenMatters", "bridgeKind", "bridgeSingle", "pressure",
        };

        private readonly JObject _checks;

        public RoundProof(int number, int definitionVersion, RoundSolution solution, RoundRandomTest randomTest, JObject checks, RoundAudit audit, RoundEngineVersion engine)
        {
            Number = number;
            DefinitionVersion = definitionVersion;
            Solution = solution ?? throw new ArgumentNullException(nameof(solution));
            RandomTest = randomTest ?? throw new ArgumentNullException(nameof(randomTest));
            _checks = (JObject)(checks ?? throw new ArgumentNullException(nameof(checks))).DeepClone();
            Audit = audit ?? throw new ArgumentNullException(nameof(audit));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public int Number { get; }

        public int DefinitionVersion { get; }

        public RoundSolution Solution { get; }

        public RoundRandomTest RandomTest { get; }

        /// <summary>A copy of the class checks object.</summary>
        public JObject Checks => (JObject)_checks.DeepClone();

        public RoundAudit Audit { get; }

        public RoundEngineVersion Engine { get; }

        /// <summary>Whether the checks object carries the named check.</summary>
        public bool HasCheck(string name) => _checks[name] != null;
    }
}
