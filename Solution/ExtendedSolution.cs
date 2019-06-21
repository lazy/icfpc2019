namespace Icfpc2019.Solution
{
    /// <summary>
    /// Helper class for storing (best) solutions. In addition to solution itself stores useful metadata
    /// for restoring which version of code has generated it
    /// </summary>
    public class ExtendedSolution
    {
        public ExtendedSolution(int score, string strategyName, string gitCommitId, string moves)
        {
            this.Score = score;
            this.StrategyName = strategyName;
            this.GitCommitId = gitCommitId;
            this.Moves = moves;
        }

        public int Score { get; }
        public string StrategyName { get; }
        public string GitCommitId { get; }
        public string Moves { get; }
    }
}
