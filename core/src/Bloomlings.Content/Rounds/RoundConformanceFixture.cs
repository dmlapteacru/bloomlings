using System;
using System.Collections.Generic;
using Bloomlings.Core.Rounds;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Content.Rounds
{
    /// <summary>One solution window of a fixture: its command and the SHA-256 of its events and of the state after it.</summary>
    public sealed class RoundFixtureWindow
    {
        public RoundFixtureWindow(RoundCommand command, string events, string signature)
        {
            Command = command;
            Events = events;
            Signature = signature;
        }

        public RoundCommand Command { get; }

        /// <summary><c>sha256:…</c> of the window's canonical event lines.</summary>
        public string Events { get; }

        /// <summary><c>sha256:…</c> of the extended state signature after the window.</summary>
        public string Signature { get; }
    }

    /// <summary>One recorded random-test game: its seed, how it ended, when, and the final state's signature hash.</summary>
    public sealed class RoundFixtureGame
    {
        public RoundFixtureGame(long seed, string outcome, long endUs, string finalSignature)
        {
            Seed = seed;
            Outcome = outcome;
            EndUs = endUs;
            FinalSignature = finalSignature;
        }

        public long Seed { get; }

        public string Outcome { get; }

        public long EndUs { get; }

        public string FinalSignature { get; }
    }

    /// <summary>One owner attempt from the pilot results, replayed by the reference with µs times.</summary>
    public sealed class RoundFixtureAttempt
    {
        public RoundFixtureAttempt(string source, string attempt, IReadOnlyList<RoundCommand> commands, string outcome, long endUs, string finalSignature)
        {
            Source = source;
            Attempt = attempt;
            Commands = commands;
            Outcome = outcome;
            EndUs = endUs;
            FinalSignature = finalSignature;
        }

        public string Source { get; }

        public string Attempt { get; }

        public IReadOnlyList<RoundCommand> Commands { get; }

        public string Outcome { get; }

        public long EndUs { get; }

        public string FinalSignature { get; }
    }

    /// <summary>
    /// A conformance fixture (<c>round-conformance.v1</c>, contracts/conformance-fixture.md), written only by the JS
    /// exporter from the reference engine. The format has no schema contract, so the reader requires the documented fields
    /// and keeps any further ones in <see cref="Document"/>.
    /// </summary>
    public sealed class RoundConformanceFixture
    {
        public const string Format = "round-conformance.v1";

        private readonly JObject _document;

        public RoundConformanceFixture(
            int number,
            int definitionVersion,
            RoundEngineVersion engine,
            IReadOnlyList<RoundFixtureWindow> windows,
            string solutionOutcome,
            long solutionEndUs,
            string solutionFinalSignature,
            string randomPolicy,
            long randomSeedBase,
            int randomRuns,
            IReadOnlyList<RoundFixtureGame> games,
            int randomWins,
            IReadOnlyList<RoundFixtureAttempt> owner,
            JObject document)
        {
            Number = number;
            DefinitionVersion = definitionVersion;
            Engine = engine;
            Windows = windows;
            SolutionOutcome = solutionOutcome;
            SolutionEndUs = solutionEndUs;
            SolutionFinalSignature = solutionFinalSignature;
            RandomPolicy = randomPolicy;
            RandomSeedBase = randomSeedBase;
            RandomRuns = randomRuns;
            Games = games;
            RandomWins = randomWins;
            Owner = owner;
            _document = (JObject)(document ?? throw new ArgumentNullException(nameof(document))).DeepClone();
        }

        public int Number { get; }

        public int DefinitionVersion { get; }

        public RoundEngineVersion Engine { get; }

        public IReadOnlyList<RoundFixtureWindow> Windows { get; }

        public string SolutionOutcome { get; }

        public long SolutionEndUs { get; }

        public string SolutionFinalSignature { get; }

        public string RandomPolicy { get; }

        public long RandomSeedBase { get; }

        public int RandomRuns { get; }

        public IReadOnlyList<RoundFixtureGame> Games { get; }

        public int RandomWins { get; }

        /// <summary>The owner's attempts; empty where the level has no pilot results.</summary>
        public IReadOnlyList<RoundFixtureAttempt> Owner { get; }

        /// <summary>A copy of the whole fixture document, as read.</summary>
        public JObject Document => (JObject)_document.DeepClone();
    }
}
