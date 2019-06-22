namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;

    using System.Linq;

    public static class Emulator
    {
        public static ExtendedSolution MakeExtendedSolution(Map map, IStrategy strategy) =>
            MakeExtendedSolution(map, strategy.Name, strategy.Solve(new State(map)));

        public static ExtendedSolution MakeExtendedSolution(Map map, string strategyName, Command[][] moves)
        {
            try
            {
                var (isValid, timeUnits) = IsValidSolution(map, moves);
                return new ExtendedSolution(
                    isSuccessful: isValid,
                    timeUnits: timeUnits,
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
                    commands: string.Empty);
            }
        }

        public static (bool isValid, int? timeUnits) IsValidSolution(Map map, Command[][] commands)
        {
            State? state = new State(map);
            var timeUnits = 0;

            var commandsPerBot = commands.Select(cmd => cmd.AsEnumerable().GetEnumerator()).ToArray();

            while (true)
            {
                var stepCommands = new Command[state.BotsCount];
                var hasCommands = false;
                for (var i = 0; i < state.BotsCount; ++i)
                {
                    if (commandsPerBot[i].MoveNext())
                    {
                        stepCommands[i] = commandsPerBot[i].Current;
                        hasCommands = true;
                    }
                }

                if (!hasCommands)
                {
                    break;
                }

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