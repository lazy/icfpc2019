namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Emulator
    {
        public static bool IsValidSolution(Map map, IReadOnlyList<Move> moves)
        {
            foreach (var move in moves)
            {
            }

            return false;
        }

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

            var cellsToVisit = FindCellsToVisit(map);
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

            var dirx = 1;
            var diry = 0;

            var manipulatorExtensionCount = 0;
            var fastWheelsCount = 0;
            var drillsCount = 0;

            var remainingSpeedBoostedMoves = 0;
            var remainingDrillMoves = 0;

            MarkVisited();

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
                        (dirx, diry) = (-diry, dirx);
                        break;
                    case Move.TurnRight:
                        (dirx, diry) = (diry, -dirx);
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
            return cellsToVisit.Count == 0;

            bool DoMove(Move move)
            {
                var (dx, dy) = GetDelta(move);
                var (newX, newY) = (x + dx, y + dy);
                if (!map.CanMoveTo(newX, newY) &&
                    !drilledCells.Contains((newX, newY)) &&
                    !(remainingDrillMoves > 0 && map[newX, newY] == Map.Cell.Obstacle))
                {
                    return false;
                }
                x += dx;
                y += dy;
                MarkVisited();

                switch (map[x, y])
                {
                    case Map.Cell.Empty:
                        break;
                    case Map.Cell.Obstacle:
                        if (remainingDrillMoves <= 0)
                        {
                            throw new InvalidOperationException();
                        }

                        if (drilledCells.Contains((x, y)))
                        {
                            drilledCells.Add((x, y));
                        }

                        break;
                    case Map.Cell.FastWheels:
                        if (!pickedUpBoosterCoords.Contains((x, y)))
                        {
                            ++fastWheelsCount;
                            pickedUpBoosterCoords.Add((x, y));
                        }

                        break;

                    case Map.Cell.Drill:
                        if (!pickedUpBoosterCoords.Contains((x, y)))
                        {
                            ++drillsCount;
                            pickedUpBoosterCoords.Add((x, y));
                        }

                        break;

                    case Map.Cell.ManipulatorExtension:
                        if (!pickedUpBoosterCoords.Contains((x, y)))
                        {
                            ++manipulatorExtensionCount;
                            pickedUpBoosterCoords.Add((x, y));
                        }

                        break;

                    default:
                        throw new InvalidOperationException($"Unexpected cell: {map[x, y]}");
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

                    // turn
                    dx = (dx * dirx) + (dy * diry);
                    dy = (dy * dirx) + (dx * diry);

                    // TODO: check visibility
                    var manipCoord = (x + dx, y + dy);
                    if (cellsToVisit.Contains(manipCoord))
                    {
                        cellsToVisit.Remove(manipCoord);
                    }
                }
            }
        }

        private static HashSet<(int, int)> FindCellsToVisit(Map map)
        {
            var result = new HashSet<(int, int)>();
            var queue = new Queue<(int, int)>();
            queue.Enqueue((map.StartX, map.StartY));

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                var (x, y) = point;
                if (!result.Contains(point))
                {
                    result.Add(point);
                    Add(-1, 0);
                    Add(1, 0);
                    Add(0, -1);
                    Add(0, 1);

                    void Add(int dx, int dy)
                    {
                        if (map.CanMoveTo(x + dx, y + dy))
                        {
                            queue.Enqueue((x + dx, y + dy));
                        }
                    }
                }
            }

            return result;
        }

        private static class GitInfo
        {
            public static string GitCommit => "TODO";
        }
    }
}