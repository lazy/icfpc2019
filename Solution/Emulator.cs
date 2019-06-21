namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;

    using System.Linq;

    public static class Emulator
    {
        public static ExtendedSolution MakeExtendedSolution(Map map, IStrategy strategy) =>
            MakeExtendedSolution(map, strategy.Name, strategy.Solve(map));

        public static ExtendedSolution MakeExtendedSolution(Map map, string strategyName, IEnumerable<Move> movesEnumerable)
        {
            try
            {
                var moves = movesEnumerable.ToArray();
                var isValid = IsValidSolution(map, moves);
                return new ExtendedSolution(
                    isSuccessful: isValid,
                    comment: isValid ? "valid" : "invalid",
                    strategyName: strategyName,
                    gitCommitId: GitInfo.GitCommit,
                    moves: moves);
            }
            catch (Exception ex)
            {
                return new ExtendedSolution(
                    isSuccessful: false,
                    timeUnits: null,
                    comment: ex.Message.Replace("\n", "\\n"),
                    strategyName: strategyName,
                    gitCommitId: GitInfo.GitCommit,
                    moves: string.Empty);
            }
        }

        public static bool IsValidSolution(Map map, IReadOnlyList<Move> moves)
        {
            State? state = new State(map);
            var movesCount = 0;

            foreach (var move in moves)
            {
                state = state.Next(move);
                if (state == null)
                {
                    return false;
                }

                ++movesCount;
            }

            return state.WrappedCellsCount == map.CellsToVisit.Count();
        }

        private static class GitInfo
        {
            public static string GitCommit => "TODO";
        }
    }
}