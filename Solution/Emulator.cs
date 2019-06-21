﻿namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Emulator
    {
        public static ExtendedSolution MakeExtendedSolution(Map map, IStrategy strategy)
        {
            try
            {
                var moves = strategy.Solve(map).ToList();
                var isValid = IsValidSolution(map, moves);
                return new ExtendedSolution(
                    isSuccessful: isValid,
                    comment: isValid ? "valid" : "invalid",
                    strategyName: strategy.Name,
                    gitCommitId: GitInfo.GitCommit,
                    moves: moves);
            }
            catch (Exception ex)
            {
                return new ExtendedSolution(
                    isSuccessful: false,
                    timeUnits: null,
                    comment: ex.Message.Replace("\n", "\\n"),
                    strategyName: strategy.Name,
                    gitCommitId: GitInfo.GitCommit,
                    moves: string.Empty);
            }
        }

        public static bool IsValidSolution(Map map, List<Move> moves)
        {
            foreach (var move in moves)
            {
            }

            return false;
        }

        private static class GitInfo
        {
            public static string GitCommit => "TODO";
        }
    }
}
