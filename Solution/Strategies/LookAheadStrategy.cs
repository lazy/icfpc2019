namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Data;
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
                    // During walking we've found extension thingy - let's take it then!
                    if (state.ManipulatorExtensionCount > 0)
                    {
                        // TODO: try different extension! strategies
                        /*
                        var extensionDist = state.ManipConfig.Length / 2;
                        var sign = state.ManipConfig.Length % 2 == 0 ? 1 : -1;
                        var (dx, dy) = State.TurnManip(state.Dir, (1, extensionDist * sign));
                        yield return Next(new UseManipulatorExtension(dx, dy));
                        break;
                        */

                        var extensionDist = state.ManipConfig.Length - 2;
                        var sign = 1;
                        if (extensionDist > 4)
                        {
                            extensionDist -= 3;
                            sign = -1;
                        }

                        var (dx, dy) = State.TurnManip(state.Dir, (1, extensionDist * sign));
                        yield return Next(new UseManipulatorExtension(dx, dy));
                        break;
                    }

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

            // just call it to silence compiler
            TryBeamSearchImpl();

            yield break;

            Command? TryBeamSearch() => null; // TryBeamSearchImpl();

            Command? TryBeamSearchImpl()
            {
                var curWrappedCount = state.WrappedCellsCount;
                var beam = new StablePriorityQueue<WeightedState>(BeamSize + 1);
                var seenStates = new HashSet<int>();

                var startState = new WeightedState(state, null);
                beam.Enqueue(startState, startState.CalcPriority(state.X, state.Y));

                var bestState = (WeightedState?)null;

                // var numCollisions = 0
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
                                if (seenStates.Contains(nextState.Hash))
                                {
                                    // ++numCollisions;
                                    continue;
                                }

                                seenStates.Add(nextState.Hash);
                                if (beam.Count < BeamSize ||
                                    nextState.WrappedCellsCount > beam.First.State.WrappedCellsCount)
                                {
                                    var nextWeightedState = new WeightedState(nextState, (prevState, command));
                                    beam.Enqueue(nextWeightedState, nextWeightedState.CalcPriority(state.X, state.Y));

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

                // Console.WriteLine($"Collisions: {numCollisions}")
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
            }

            void Bfs()
            {
                ++generation;
                bfsQueue.Clear();
                bfsQueue.Enqueue((state.X, state.Y, state.Dir));
                bfsNodes[state.X, state.Y, state.Dir] = new BfsNode(generation, -1, 0);

                int? maxDepth = null;
                (int, int, int, int distFromCenter)? bestDest = null;

                while (bfsQueue.Count > 0)
                {
                    var (x, y, dir) = bfsQueue.Dequeue();
                    var depth = bfsNodes[x, y, dir].Depth;

                    Debug.Assert(bfsNodes[x, y, dir].Generation == generation, "oops");

                    if (maxDepth != null && depth > maxDepth.Value)
                    {
                        if (bestDest == null)
                        {
                            throw new InvalidOperationException();
                        }

                        var (destX, destY, destDir, distFromCenter) = bestDest.Value;
                        FindBackwardPath(destX, destY, destDir);
                        return;
                    }

                    // path found
                    if (state.UnwrappedVisible(x, y, dir))
                    {
                        if (maxDepth == null)
                        {
                            maxDepth = depth + 5;
                        }

                        var distFromCenter = state.MaxUnwrappedVisibleDistFromCenter(x, y, dir);
                        if (bestDest == null || distFromCenter > bestDest.Value.distFromCenter)
                        {
                            bestDest = (x, y, dir, distFromCenter);
                        }
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfsNodes[nx, ny, dir].Generation != generation)
                        {
                            bfsNodes[nx, ny, dir] = new BfsNode(generation, i, depth + 1);
                            bfsQueue.Enqueue((nx, ny, dir));
                        }
                    }

                    for (var i = 0; i < Turn.All.Length; ++i)
                    {
                        var ddir = Turn.All[i].Ddir;
                        var ndir = (dir + ddir) & 3;
                        if (bfsNodes[x, y, ndir].Generation != generation)
                        {
                            bfsNodes[x, y, ndir] = new BfsNode(generation, -ddir, depth + 1);
                            bfsQueue.Enqueue((x, y, ndir));
                        }
                    }
                }

                var (finalX, finalY, finalDir, _) =
                    bestDest ?? throw new InvalidOperationException("Couldn't find any path with BFS!");
                FindBackwardPath(finalX, finalY, finalDir);
            }

            void FindBackwardPath(int x, int y, int dir)
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
            }
        }

        private struct BfsNode
        {
            public BfsNode(int generation, int moveIdx, int depth)
            {
                this.Generation = generation;
                this.MoveIdx = moveIdx;
                this.Depth = depth;
            }

            public int Generation { get; }
            public int MoveIdx { get; }
            public int Depth { get; }
        }

        private class WeightedState : StablePriorityQueueNode
        {
            public WeightedState(State state, (WeightedState, Command)? prev)
            {
                this.State = state;
                this.Prev = prev;
                this.BestVisibleCount = Math.Max(
                    this.State.MaxUnwrappedVisibleDistFromCenter(this.State.X, this.State.Y, this.State.Dir),
                    this.Prev?.state?.BestVisibleCount ?? 0);
            }

            public State State { get; }
            public (WeightedState state, Command command)? Prev { get; }
            public int BestVisibleCount { get; }

            public float CalcPriority(int startX, int startY) =>
                (128 * this.BestVisibleCount) +
                (8 * this.State.WrappedCellsCount) +
                (0 * Math.Abs(this.State.X - startX)) +
                (0 * Math.Abs(this.State.Y - startY));
        }
    }
}
