namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    public class LookAheadStrategy : IStrategy
    {
        private const int BeamSize = 64;
        private const int BeamSearchDepth = 10;

        public IEnumerable<Command> Solve(Map map)
        {
            var state = new State(map);
            var generation = 0;

            // reuse to reduce memory consumption
            var bfsNodes = new BfsNode[map.Width, map.Height];
            var bfsQueue = new Queue<(int, int)>();
            var bfsPath = new List<int>(); // <- indices of moves

            // for debugging purposes
            // var history = new List<(Command, State)>();
            Command Next(Command cmd)
            {
                var newState = state.Next(cmd);
                state = newState ?? throw new InvalidOperationException("Generated invalid move!");

                // history.Add((cmd, state));
                return cmd;
            }

            while (state.WrappedCellsCount != map.CellsToVisit.Count)
            {
                Bfs();

                for (var i = 0; i < bfsPath.Count; ++i)
                {
                    var bfsCommand = Move.All[bfsPath[i]];
                    if (i >= bfsPath.Count - 10)
                    {
                        // If close to bfs finish - try looking for optimal solution with beam search
                        var command = TryBeamSearch();
                        if (command != null && !command.Equals(bfsCommand))
                        {
                            // found a command that is different from bfs path, it invalidates bfs
                            yield return Next(command);
                            break;
                        }
                    }

                    // just go according to bfs
                    yield return Next(bfsCommand);
                }
            }

            yield break;

            Command? TryBeamSearch()
            {
                return null;
            }

            // returns dist to
            void Bfs()
            {
                ++generation;
                bfsQueue.Clear();
                bfsQueue.Enqueue((state.X, state.Y));
                bfsNodes[state.X, state.Y] = new BfsNode(generation, -1);
                while (bfsQueue.Count > 0)
                {
                    var (x, y) = bfsQueue.Dequeue();

                    Debug.Assert(bfsNodes[x, y].Generation == generation, "oops");

                    // path found
                    if (map.IsFree(x, y) && !state.IsWrapped(x, y))
                    {
                        bfsPath.Clear();
                        while ((x, y) != (state.X, state.Y))
                        {
                            Debug.Assert(bfsNodes[x, y].Generation == generation, "oops");

                            var dir = bfsNodes[x, y].Direction;
                            bfsPath.Add(dir);
                            var move = Move.All[dir];
                            x -= move.Dx;
                            y -= move.Dy;
                        }

                        bfsPath.Reverse();
                        return;
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfsNodes[nx, ny].Generation != generation)
                        {
                            bfsNodes[nx, ny] = new BfsNode(generation, i);
                            bfsQueue.Enqueue((nx, ny));
                        }
                    }
                }

                throw new InvalidOperationException("Couldn't find any path with BFS!");
            }
        }

        private struct BfsNode
        {
            public BfsNode(int generation, int direction)
            {
                this.Generation = generation;
                this.Direction = direction;
            }

            public int Generation { get; }
            public int Direction { get; }
        }
    }
}
