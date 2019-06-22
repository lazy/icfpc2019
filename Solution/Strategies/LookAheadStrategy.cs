namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Priority_Queue;

    public class LookAheadStrategy : IStrategy
    {
        private const int BeamSize = 64;
        private const int BeamSearchDepth = 10;

        private static readonly Command[] BeamSearchCommands =
        {
            Move.Up,
            Move.Down,
            Move.Left,
            Move.Right,
            Turn.Left,
            Turn.Right,

            // TODO: fix BFS to support it
            // UseFastWheels.Instance,
        };

        public IEnumerable<Command> Solve(Map map)
        {
            var state = new State(map);
            var generation = 0;

            // reuse to reduce memory consumption
            var bfsNodes = new BfsNode[map.Width, map.Height, 4];
            var bfsQueue = new Queue<(int, int, int)>();
            var bfsPath = new List<Command>();

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
                    var bfsCommand = bfsPath[i];

                    if (i >= bfsPath.Count - 10)
                    {
                        // If close to bfs finish - try looking for optimal solution with beam search
                        var command = TryBeamSearch();
                        if (command != null && !command.Equals(bfsCommand))
                        {
                            // found a command that is different from bfs path, it invalidates bfs
                            // Console.WriteLine($"    BS: {command}, bfs[{i}/{bfsPath.Count}]: {bfsCommand}");
                            yield return Next(command);
                            break;
                        }
                    }

                    // just go according to bfs
                    // Console.WriteLine($"        bfs[{i}/{bfsPath.Count}]: {bfsCommand}");
                    yield return Next(bfsCommand);
                }
            }

            yield break;

            Command? TryBeamSearch()
            {
                var curWrappedCount = state.WrappedCellsCount;
                var beam = new FastPriorityQueue<WeightedState>(BeamSize + 1);

                beam.Enqueue(new WeightedState(state, null), CalcPriority(state));

                var bestState = (WeightedState?)null;

                for (var depth = 0; depth < BeamSearchDepth; ++depth)
                {
                    var prevBeam = beam.ToArray();
                    beam.Clear();

                    foreach (var prevState in prevBeam)
                    {
                        foreach (var command in BeamSearchCommands)
                        {
                            var nextState = prevState.State.Next(command);
                            if (nextState != null)
                            {
                                if (beam.Count < BeamSize ||
                                    nextState.WrappedCellsCount > beam.First.State.WrappedCellsCount)
                                {
                                    var nextWeightedState = new WeightedState(nextState, (prevState, command));
                                    beam.Enqueue(nextWeightedState, CalcPriority(nextState));

                                    // remove worst states
                                    if (beam.Count > BeamSize)
                                    {
                                        beam.Dequeue();
                                    }

                                    // update the global best state if possible
                                    if (nextState.WrappedCellsCount > curWrappedCount &&
                                        (bestState == null || nextState.WrappedCellsCount > bestState.State.WrappedCellsCount))
                                    {
                                        bestState = nextWeightedState;
                                    }
                                }
                            }
                        }
                    }
                }

                if (bestState == null)
                {
                    return null;
                }

                // Scroll back until first command is found
                var firstCommand = (Command?)null;
                var cur = bestState;
                while (cur.Prev != null)
                {
                    var (prev, command) = cur.Prev.Value;
                    firstCommand = command;
                    cur = prev;
                }

                return firstCommand;

                float CalcPriority(State newState) =>
                    newState.WrappedCellsCount + (0.01f * (Math.Abs(newState.X - state.X) + Math.Abs(newState.Y - state.Y)));
            }

            void Bfs()
            {
                ++generation;
                bfsQueue.Clear();
                bfsQueue.Enqueue((state.X, state.Y, state.Dir));
                bfsNodes[state.X, state.Y, state.Dir] = new BfsNode(generation, -1);
                while (bfsQueue.Count > 0)
                {
                    var (x, y, dir) = bfsQueue.Dequeue();

                    Debug.Assert(bfsNodes[x, y, dir].Generation == generation, "oops");

                    // path found
                    if (state.UnwrappedVisible(x, y, dir))
                    {
                        bfsPath.Clear();
                        var xx = x;
                        var yy = y;
                        while ((x, y, dir) != (state.X, state.Y, state.Dir))
                        {
                            Debug.Assert(bfsNodes[x, y, dir].Generation == generation, "oops");

                            var moveIdx = bfsNodes[x, y, dir].MoveIdx;
                            if (moveIdx >= 0)
                            {
                                var move = Move.All[moveIdx];
                                bfsPath.Add(move);
                                x -= move.Dx;
                                y -= move.Dy;
                            }
                            else
                            {
                                var ddir = -moveIdx;
                                dir = (4 + dir - ddir) & 3;
                                bfsPath.Add(ddir == 1 ? Turn.Left : Turn.Right);
                            }
                        }

                        bfsPath.Reverse();

                        // if there's more than one command, then filter out all turns
                        if (bfsPath.Count > 1)
                        {
                            var writeIdx = 0;
                            for (var i = 0; i < bfsPath.Count; ++i)
                            {
                                if (!(bfsPath[i] is Turn))
                                {
                                    bfsPath[writeIdx++] = bfsPath[i];
                                }
                            }

                            // degenerate case: 180 degree turn
                            if (writeIdx != 0)
                            {
                                bfsPath.RemoveRange(writeIdx, bfsPath.Count - writeIdx);
                            }
                        }

                        return;
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfsNodes[nx, ny, dir].Generation != generation)
                        {
                            bfsNodes[nx, ny, dir] = new BfsNode(generation, i);
                            bfsQueue.Enqueue((nx, ny, dir));
                        }
                    }

                    for (var i = 0; i < Turn.All.Length; ++i)
                    {
                        var ddir = Turn.All[i].Ddir;
                        var ndir = (dir + ddir) & 3;
                        if (bfsNodes[x, y, ndir].Generation != generation)
                        {
                            bfsNodes[x, y, ndir] = new BfsNode(generation, -ddir);
                            bfsQueue.Enqueue((x, y, ndir));
                        }
                    }
                }

                throw new InvalidOperationException("Couldn't find any path with BFS!");
            }
        }

        private struct BfsNode
        {
            public BfsNode(int generation, int moveIdx)
            {
                this.Generation = generation;
                this.MoveIdx = moveIdx;
            }

            public int Generation { get; }
            public int MoveIdx { get; }
        }

        private class WeightedState : FastPriorityQueueNode
        {
            public WeightedState(State state, (WeightedState, Command)? prev)
            {
                this.State = state;
                this.Prev = prev;
            }

            public State State { get; }
            public (WeightedState state, Command command)? Prev { get; }
        }
    }
}
