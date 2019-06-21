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
            var cellsToVisit = new HashSet<(int, int)>(map.CellsToVisit);
            var pickedUpBoosterCoords = new HashSet<(int, int)>();
            var drilledCells = new HashSet<(int, int)>();

            var x = map.StartX;
            var y = map.StartY;

            var manipConfig = new[]
            {
                (0, 0),
                (1, -1),
                (1, 0),
                (1, 1),
            };

            var dir = 0;

            var manipulatorExtensionCount = 0;
            var fastWheelsCount = 0;
            var drillsCount = 0;
            var mysteriousPointsCount = 0;
            var teleportsCount = 0;

            var remainingSpeedBoostedMoves = 0;
            var remainingDrillMoves = 0;

            MarkVisited();

            foreach (var move in moves)
            {
                switch (move)
                {
                    case Move.MoveUp:
                    case Move.MoveDown:
                    case Move.MoveLeft:
                    case Move.MoveRight:
                        if (!DoMove(move))
                        {
                            return false;
                        }

                        if (remainingSpeedBoostedMoves > 0)
                        {
                            MarkVisited();

                            // It is allowed to fail
                            DoMove(move);
                        }

                        break;
                    case Move.TurnLeft:
                        dir = (dir + 1) & 3;
                        break;
                    case Move.TurnRight:
                        dir = (dir + 3) & 3;
                        break;
                    case Move.UseManipulatorExtension:
                        throw new NotImplementedException();
                    case Move.UseFastWheels:
                        if (fastWheelsCount <= 0)
                        {
                            return false;
                        }

                        --fastWheelsCount;
                        remainingSpeedBoostedMoves = 50;
                        break;
                    case Move.UseDrill:
                        if (drillsCount <= 0)
                        {
                            return false;
                        }

                        --drillsCount;
                        remainingDrillMoves = 30;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(move), move.ToString());
                }

                --remainingSpeedBoostedMoves;
                --remainingDrillMoves;
                MarkVisited();
            }

            return cellsToVisit.Count == 0;

            bool DoMove(Move move)
            {
                var (dx, dy) = GetDelta(move);
                var (newX, newY) = (x + dx, y + dy);
                if (!map.IsFree(newX, newY) &&
                    !drilledCells.Contains((newX, newY)) &&
                    !(remainingDrillMoves > 0 && map[newX, newY] == Map.Cell.Obstacle))
                {
                    return false;
                }

                x += dx;
                y += dy;

                switch (map[x, y])
                {
                    case Map.Cell.Empty:
                        break;
                    case Map.Cell.Obstacle:
                        if (remainingDrillMoves <= 0)
                        {
                            throw new InvalidOperationException();
                        }

                        if (!drilledCells.Contains((x, y)))
                        {
                            drilledCells.Add((x, y));
                        }

                        break;
                    case Map.Cell.FastWheels:
                        CollectBooster(ref fastWheelsCount);
                        break;
                    case Map.Cell.Drill:
                        CollectBooster(ref drillsCount);
                        break;
                    case Map.Cell.ManipulatorExtension:
                        CollectBooster(ref manipulatorExtensionCount);
                        break;
                    case Map.Cell.MysteriousPoint:
                        CollectBooster(ref mysteriousPointsCount);
                        break;
                    case Map.Cell.Teleport:
                        CollectBooster(ref teleportsCount);
                        break;
                    default:
                        throw new InvalidOperationException($"Unexpected cell: {map[x, y]}");
                }

                void CollectBooster(ref int counter)
                {
                    if (!pickedUpBoosterCoords.Contains((x, y)))
                    {
                        ++counter;
                        pickedUpBoosterCoords.Add((x, y));
                    }
                }

                return true;
            }

            (int, int) GetDelta(Move move) =>
                move switch
                    {
                    Move.MoveUp => (0, 1),
                    Move.MoveDown => (0, -1),
                    Move.MoveLeft => (-1, 0),
                    Move.MoveRight => (1, 0),
                    _ => throw new ArgumentOutOfRangeException(nameof(move)),
                    };

            void MarkVisited()
            {
                foreach (var delta in manipConfig)
                {
                    var (dx, dy) = delta;

                    (dx, dy) = dir switch
                        {
                        0 => (dx, dy),
                        1 => (-dy, dx),
                        2 => (-dx, -dy),
                        3 => (dy, -dx),
                        _ => throw new ArgumentOutOfRangeException(nameof(dir)),
                        };

                    // TODO: check visibility
                    var manipCoord = (x + dx, y + dy);
                    if (cellsToVisit.Contains(manipCoord))
                    {
                        cellsToVisit.Remove(manipCoord);
                    }
                }
            }
        }

        private static class GitInfo
        {
            public static string GitCommit => "TODO";
        }
    }
}