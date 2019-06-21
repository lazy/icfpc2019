namespace Icfpc2019.Solution
{
    using System;

    /// <summary>
    /// Helper class for storing (best) solutions. In addition to solution itself stores useful metadata
    /// for restoring which version of code has generated it
    /// </summary>
    public class ExtendedSolution
    {
        public ExtendedSolution(
            bool isSuccessful,
            int? score,
            string comment,
            string strategyName,
            string gitCommitId,
            string moves)
        {
            this.IsSuccessful = isSuccessful;
            this.Score = score;
            this.Comment = comment;
            this.StrategyName = strategyName;
            this.GitCommitId = gitCommitId;
            this.Moves = moves;
        }

        public bool IsSuccessful { get; }
        public int? Score { get; }
        public string? Comment { get; }
        public string StrategyName { get; }
        public string GitCommitId { get; }
        public string Moves { get; }

        public ExtendedSolution Load(string filename) =>
            throw new NotImplementedException();

        /// <summary>
        /// Saves solution to file. But only if this file doesn't exist or this solution is better than
        /// solution in that file.
        /// </summary>
        public void SaveIfBetter(string filename) =>
            throw new NotImplementedException();
    }
}
