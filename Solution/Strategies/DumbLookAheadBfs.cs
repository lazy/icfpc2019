namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Data;

    using Microsoft.VisualBasic;

    public class DumbLookAheadBfs : IStrategy
    {
        // Prefer moves facing same direction
        private static readonly Move[][] DirToMoves =
        {
            // Left,
            new[]
            {
                Move.Left,
                Move.Down,
                Move.Up,
                Move.Right,
            },

            // Up
            new[]
            {
                Move.Up,
                Move.Right,
                Move.Left,
                Move.Down,
            },

            // Right
            new[]
            {
                Move.Right,
                Move.Up,
                Move.Down,
                Move.Left,
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

            // Used only by RunBfs()
            var bfs = new BfsState(map);

            while (masterState.WrappedCellsCount != map.CellsToVisit.Count)
            {
                var firstPath = new List<Command>();

                var noTurn = MeasureProfit(masterState, null, this.lookAheadSize, firstPath);
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
                    MeasureProfit(masterState, Turn.Left, this.lookAheadSize - 1),
                    MeasureProfit(masterState, Turn.Right, this.lookAheadSize - 1),
                };

                var best = noTurn;
                foreach (var p in profits)
                {
                    if (p.profit > best.profit)
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

            (Command? command, int profit) MeasureProfit(
                State state, Command? firstCommand, int lookAhead, List<Command>? firstPath = null)
            {
                if (firstCommand != null)
                {
                    var nextState = state.Next(firstCommand);
                    if (nextState == null)
                    {
                        return (null, state.WrappedCellsCount);
                    }

                    state = nextState;
                }

                var (firstBfsCommand, profit) = RepeatBfs(state, 1 + lookAhead, firstPath);
                firstCommand ??= firstBfsCommand;

                return (firstCommand, profit);
            }

            (Command? command, int profit) RepeatBfs(State state, int times, List<Command>? firstPath)
            {
                Command? firstCmd = null;
                while (times > 0 && RunBfs(state) && bfs.Path.Count > 0)
                {
                    for (var i = 0; i < bfs.Path.Count && times > 0; ++i, --times)
                    {
                        if (firstCmd == null)
                        {
                            firstCmd = bfs.Path[0];
                            firstPath?.AddRange(bfs.Path);
                        }

                        state = state.Next(bfs.Path[i]) ?? throw new Exception("Impossible");
                    }
                }

                return (firstCmd, state.WrappedCellsCount);
            }

            bool RunBfs(State state)
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

                    if (state.UnwrappedCellsVisible(x, y, bot.Dir))
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
