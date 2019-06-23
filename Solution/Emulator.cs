namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;

    using System.Linq;

    public static class Emulator
    {
        public static ExtendedSolution MakeExtendedSolution(Map map, IStrategy strategy)
        {
            try
            {
                return MakeExtendedSolution(map, strategy.Name, strategy.Solve(new State(map)));
            }
            catch (Exception ex)
            {
                return new ExtendedSolution(
                    isSuccessful: false,
                    timeUnits: null,
                    comment: ex.Message.Replace("\n", "\\n"),
                    strategyName: strategy.Name,
                    gitCommitId: GitInfo.GitCommit,
                    commands: string.Empty);
            }
        }

        public static ExtendedSolution MakeExtendedSolution(Map map, string strategyName, IEnumerable<Command[]> commands)
        {
            var (isValid, timeUnits) = IsValidSolution(map, commands);
            return new ExtendedSolution(
                isSuccessful: isValid,
                timeUnits: timeUnits,
                comment: isValid ? "valid" : "invalid",
                strategyName: strategyName,
                gitCommitId: GitInfo.GitCommit,
                commands: commands);
        }

        public static (bool isValid, int? timeUnits) IsValidSolution(Map map, IEnumerable<Command[]> commands)
        {
            State? state = new State(map);
            var timeUnits = 0;

            var buf = new List<Command[]>();

            foreach (var stepCommands in commands)
            {
                buf.Add(stepCommands);
                state = state.Next(stepCommands);
                if (state == null)
                {
                    return (false, null);
                }

                ++timeUnits;
            }

            var isValid = state.WrappedCellsCount == map.CellsToVisit.Count();
            return (isValid, isValid ? (int?)timeUnits : null);
        }

        private static class GitInfo
        {
            public static string GitCommit => "TODO";
        }
    }
}