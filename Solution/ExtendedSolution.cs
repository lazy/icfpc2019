namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Helper class for storing (best) solutions. In addition to solution itself stores useful metadata
    /// for restoring which version of code has generated it
    /// </summary>
    public class ExtendedSolution
    {
        public ExtendedSolution(
            bool isSuccessful,
            string comment,
            string strategyName,
            string gitCommitId,
            IReadOnlyList<Move> moves)
            : this(
                isSuccessful,
                isSuccessful ? moves.Count() : (int?)null,
                comment,
                strategyName,
                gitCommitId,
                MovesSerializer.Serialize(moves))
        {
        }

        public ExtendedSolution(
            bool isSuccessful,
            int? timeUnits,
            string comment,
            string strategyName,
            string gitCommitId,
            string moves)
        {
            this.IsSuccessful = isSuccessful;
            this.TimeUnits = timeUnits;
            this.Comment = comment;
            this.StrategyName = strategyName;
            this.GitCommitId = gitCommitId;
            this.Moves = moves;
        }

        public string? Comment { get; }
        public string GitCommitId { get; }

        public bool IsSuccessful { get; }
        public string Moves { get; }
        public string StrategyName { get; }
        public int? TimeUnits { get; }

        public static ExtendedSolution Load(string filename)
        {
            using var stream = new StreamReader(filename);
            return new ExtendedSolution(
                isSuccessful: bool.Parse(stream.ReadLine()),
                timeUnits: int.TryParse(stream.ReadLine(), out var timeUnits) ? (int?)timeUnits : (int?)null,
                comment: stream.ReadLine(),
                strategyName: stream.ReadLine(),
                gitCommitId: stream.ReadLine(),
                moves: stream.ReadLine());
        }

        public bool IsBetterThan(ExtendedSolution that) =>
            this.IsSuccessful &&
            (!that.IsSuccessful || (this.TimeUnits ?? 0) > (that.TimeUnits ?? 0));

        /// <summary>
        /// Saves solution to file. But only if this file doesn't exist or this solution is better than
        /// solution in that file.
        /// </summary>
        public void SaveIfBetter(string filename)
        {
            if (!File.Exists(filename) ||
                this.IsBetterThan(Load(filename)))
            {
                this.Save(filename);
            }
        }

        private void Save(string filename)
        {
            using var stream = new StreamWriter(filename);
            stream.WriteLine(this.IsSuccessful);
            stream.WriteLine(this.TimeUnits);
            stream.WriteLine(this.Comment);
            stream.WriteLine(this.StrategyName);
            stream.WriteLine(this.GitCommitId);
            stream.WriteLine(this.Moves);
        }
    }
}