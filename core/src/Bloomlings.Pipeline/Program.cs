using System;

namespace Bloomlings.Pipeline
{
    /// <summary>
    /// Entry point of the content pipeline CLI. Commands are specified in
    /// specs/001-core-game-mvp/contracts/pipeline-cli.md and implemented in US3 (T081).
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            Console.Error.WriteLine("bloomlings-pipeline: no commands are implemented yet (see contracts/pipeline-cli.md).");
            return 2; // usage error, per the CLI contract
        }
    }
}
