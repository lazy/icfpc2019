namespace Icfpc2019.Solution
{
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
            int? timeUnits,
            string comment,
            string strategyName,
            string gitCommitId,
            IEnumerable<Command[]> commands,
            string boosterPack)
            : this(
                isSuccessful,
                timeUnits,
                comment,
                strategyName,
                gitCommitId,
                CommandsSerializer.Serialize(commands),
                boosterPack)
        {
        }

        public ExtendedSolution(
            bool isSuccessful,
            int? timeUnits,
            string comment,
            string strategyName,
            string gitCommitId,
            string commands,
            string boosterPack)
        {
            this.IsSuccessful = isSuccessful;
            this.TimeUnits = timeUnits;
            this.Comment = comment;
            this.StrategyName = strategyName;
            this.GitCommitId = gitCommitId;
            this.Commands = commands;
            this.BoosterPack = boosterPack;
        }

        public string? Comment { get; }
        public string GitCommitId { get; }

        public bool IsSuccessful { get; }
        public string Commands { get; }
        public string StrategyName { get; }
        public int? TimeUnits { get; }
        public string BoosterPack { get; }

        public static ExtendedSolution Load(string filename)
        {
            using var stream = new StreamReader(filename);
            return new ExtendedSolution(
                isSuccessful: bool.Parse(stream.ReadLine()),
                timeUnits: int.TryParse(stream.ReadLine(), out var timeUnits) ? (int?)timeUnits : (int?)null,
                comment: stream.ReadLine(),
                strategyName: stream.ReadLine(),
                gitCommitId: stream.ReadLine(),
                commands: stream.ReadLine(),
                boosterPack: stream.ReadLine() ?? string.Empty);
        }

        public bool IsBetterThan(ExtendedSolution that) =>
            this.IsSuccessful &&
            (!that.IsSuccessful || (this.TimeUnits ?? int.MaxValue) <= (that.TimeUnits ?? int.MaxValue));

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
            stream.WriteLine(this.Commands);
            stream.WriteLine(this.BoosterPack);
        }
    }
}