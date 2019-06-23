namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;

    public class DumbLookAheadBfs : IStrategy
    {
        // Prefer moves facing same direction
        private static Move[][] dirToMoves =
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

        public IEnumerable<Command> Solve1(State state)
        {
            var map = state.Map;
            var bfs = new BfsState(map);

            while (state.WrappedCellsCount != map.CellsToVisit.Count)
            {
                if (RunBfs(state) && bfs.Path.Count > 0)
                {
                    foreach (var cmd in bfs.Path)
                    {
                        state = state.Next(cmd) ?? throw new Exception("Impossible");
                        yield return cmd;
                    }
                }
                else
                {
                    throw new Exception("Impossible");
                }
            }

            bool RunBfs(State from)
            {
                var bot = from.GetBot(0);

                var possibleMoves = dirToMoves[bot.Dir];
                ++bfs.Generation;
                bfs.Nodes[bot.X, bot.Y, bot.Dir] = new BfsState.Node(bfs.Generation, -1, 0);
                bfs.Queue.Clear();
                bfs.Queue.Enqueue((bot.X, bot.Y, bot.Dir));

                while (bfs.Queue.Count > 0)
                {
                    var (x, y, dirUnused) = bfs.Queue.Dequeue();

                    if (from.UnwrappedCellsVisible(x, y, bot.Dir))
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
