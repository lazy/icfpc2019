namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;

    using Microsoft.VisualBasic;

    public class DumbLookAheadBfs : IStrategy
    {
        // Prefer moves facing same direction
        private static readonly Move[][] DirToMoves =
        {
            // Right
            new[]
            {
                Move.Right,
                Move.Up,
                Move.Down,
                Move.Left,
            },

            // Up
            new[]
            {
                Move.Up,
                Move.Right,
                Move.Left,
                Move.Down,
            },

            // Left
            new[]
            {
                Move.Left,
                Move.Down,
                Move.Up,
                Move.Right,
            },

            // Down
            new[]
            {
                Move.Down,
                Move.Left,
                Move.Right,
                Move.Up,
            }
        };

        private readonly int lookAheadSize;

        public DumbLookAheadBfs(int lookAheadSize)
        {
            this.lookAheadSize = lookAheadSize;
        }

        public string Name => $"{nameof(DumbLookAheadBfs)}({this.lookAheadSize})";

        public IEnumerable<Command[]> Solve(State state)
        {
            if (state.BotsCount > 1)
            {
                throw new Exception("This strategy works only with 1 bot");
            }

            foreach (var cmd in this.Solve1(state))
            {
                yield return new[] { cmd };
            }
        }

        public IEnumerable<Command> Solve1(State initState)
        {
            // To be used only in the root of the function
            var masterState = initState;
            var map = masterState.Map;

            // Used only by RunBfs() and FindSmallZones()
            var bfs = new BfsState(map);
            var distsFromCenter = map.DistsFromCenter;

            var globalZoneToVisit = map.CellsToVisit.ToHashSet();

            foreach (var visited in masterState.WrappedCells.Enumerate())
            {
                globalZoneToVisit.Remove(visited.Key);
            }

            foreach (var cmd in RunInZone(globalZoneToVisit))
            {
                yield return cmd;
            }

            IEnumerable<Command> RunInZone(HashSet<(int, int)> zone)
            {
                while (true)
                {
                    var firstPath = new List<Command>();

                    var noTurn = MeasureTurnProfit(masterState, zone, null, this.lookAheadSize, firstPath);

                    if (firstPath.Count == 0)
                    {
                        yield break;
                    }

                    if (this.lookAheadSize == 0 || firstPath.Count > 10)
                    {
                        var upTo = firstPath.Count - (this.lookAheadSize == 0 ? 0 : 10);
                        for (var i = 0; i < upTo; ++i)
                        {
                            masterState = masterState.Next(firstPath[i]) ?? throw new Exception("Impossible");
                            yield return firstPath[i];
                        }

                        continue;
                    }

                    var profits = new[]
                    {
                        noTurn,
                        MeasureTurnProfit(masterState, zone, Turn.Left, this.lookAheadSize - 1),
                        MeasureTurnProfit(masterState, zone, Turn.Right, this.lookAheadSize - 1),
                    };

                    var best = noTurn;
                    foreach (var p in profits)
                    {
                        if (p.profit > best.profit && p.profit > noTurn.profit + 1)
                        {
                            best = p;
                        }
                    }

                    if (best.command != null)
                    {
                        masterState = masterState.Next(best.command) ?? throw new Exception("Impossible");
                        yield return best.command;
                    }
                    else
                    {
                        throw new Exception("Impossible");
                    }
                }
            }

            int CalcProfit(List<(int, int)> touchedCells)
            {
                var bot = masterState.GetBot(0);
                var edgeCells = 0;
                foreach (var cell in touchedCells)
                {
                    var (x, y) = cell;
                    if (!IsFree(x + 1, y) || !IsFree(x - 1, y) || !IsFree(x, y + 1) || !IsFree(x, y - 1))
                    {
                        edgeCells += 1;
                    }
                }

                return touchedCells.Count + (10 * edgeCells);

                bool IsFree(int x, int y) =>
                    map.IsFree(x, y); // && !masterState.IsWrapped(x, y);
            }

            (Command? command, int profit) MeasureTurnProfit(
                State state,
                HashSet<(int, int)> zone,
                Command? firstCommand,
                int lookAhead,
                List<Command>? firstPath = null)
            {
                var touchedCells = new List<(int, int)>();

                if (firstCommand != null)
                {
                    var nextState = state.Next(touchedCells, firstCommand);
                    if (nextState == null)
                    {
                        return (null, 0);
                    }

                    state = nextState;
                }

                var afterFirstSize = touchedCells.Count;

                var firstBfsCommand = RepeatBfs(state, zone, 1 + lookAhead, touchedCells, firstPath);
                var bfsProfit = CalcProfit(touchedCells);

                touchedCells.RemoveRange(afterFirstSize, touchedCells.Count - afterFirstSize);

                if (lookAhead == 0)
                {
                    firstCommand ??= firstBfsCommand;
                    return (firstCommand, bfsProfit);
                }

                var possibleMoves = DirToMoves[state.GetBot(0).Dir];
                var bestMove = (command: (Command?)firstBfsCommand, profit: bfsProfit);
                foreach (var move in possibleMoves)
                {
                    if (move != firstBfsCommand)
                    {
                        TryMove(state, zone, move, lookAhead, touchedCells, null);
                        var profit = CalcProfit(touchedCells);
                        touchedCells.RemoveRange(afterFirstSize, touchedCells.Count - afterFirstSize);
                        if (profit > bestMove.profit)
                        {
                            bestMove = (move, profit);
                        }
                    }
                }

                firstCommand ??= bestMove.command;

                return (firstCommand, bestMove.profit);
            }

            void TryMove(
                State state,
                HashSet<(int, int)> zone,
                Command firstCommand,
                int lookAhead,
                List<(int, int)> touchedCells,
                List<Command>? firstPath = null)
            {
                var nextState = state.Next(touchedCells, firstCommand);
                if (nextState == null)
                {
                    return;
                }

                firstPath?.Add(firstCommand);
                RepeatBfs(nextState, zone, lookAhead, touchedCells, firstPath);
            }

            Command? RepeatBfs(State state, HashSet<(int, int)> zone, int times, List<(int, int)> touchedCells, List<Command>? firstPath)
            {
                Command? firstCmd = null;
                while (times > 0 && RunBfs(state, zone) && bfs.Path.Count > 0)
                {
                    for (var i = 0; i < bfs.Path.Count && times > 0; ++i, --times)
                    {
                        if (firstCmd == null)
                        {
                            firstCmd = bfs.Path[0];
                            firstPath?.AddRange(bfs.Path);
                        }

                        state = state.Next(touchedCells, bfs.Path[i]) ?? throw new Exception("Impossible");
                    }
                }

                return firstCmd;
            }

            bool RunBfs(State state, HashSet<(int, int)> zone)
            {
                var bot = state.GetBot(0);

                var possibleMoves = DirToMoves[bot.Dir];

                ++bfs.Generation;
                bfs.Nodes[bot.X, bot.Y, bot.Dir] = new BfsState.Node(bfs.Generation, -1, 0);
                bfs.Queue.Clear();
                bfs.Queue.Enqueue((bot.X, bot.Y, bot.Dir));

                while (bfs.Queue.Count > 0)
                {
                    var (x, y, dirUnused) = bfs.Queue.Dequeue();

                    if (state.UnwrappedCellsVisibleInZone(x, y, bot.Dir, zone))
                    {
                        bfs.FindBackwardPath(x, y, bot.Dir, bot, possibleMoves);
                        return true;
                    }

                    for (var moveIdx = 0; moveIdx < 4; ++moveIdx)
                    {
                        var move = possibleMoves[moveIdx];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfs.Nodes[nx, ny, bot.Dir].Generation != bfs.Generation)
                        {
                            bfs.Nodes[nx, ny, bot.Dir] = new BfsState.Node(bfs.Generation, moveIdx, 0);
                            bfs.Queue.Enqueue((nx, ny, bot.Dir));
                        }
                    }
                }

                return false;
            }
        }
    }
}
