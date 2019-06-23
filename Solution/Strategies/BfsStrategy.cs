namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Priority_Queue;

    public static class BfsFactory
    {
        public static IEnumerable<IStrategy> MakeStrategies(ManipulatorGrowthStrategy manipStrategy)
        {
            foreach (var recalcsTime in new[] { 1, 10 })
            {
                foreach (var extraDepth in new[] { 1, 2, 3, 4, 5, })
                {
                    foreach (var removeTurns in new[] { true, false })
                    {
                        foreach (var numVisCoeff in new[] { 0, 1 })
                        {
                            yield return new BfsStrategy(
                                manipStrategy,
                                recalcsTime,
                                extraDepth,
                                removeTurns,
                                numVisCoeff);
                        }
                    }
                }
            }
        }
    }

    public class BfsStrategy : IStrategy
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

        private readonly ManipulatorGrowthStrategy manipStrategy;
        private readonly int recalcDistsFromCenterCount;
        private readonly int bfsExtraDepth;
        private readonly bool removeTurns;
        private readonly int numVisCoeff;

        public BfsStrategy(
            ManipulatorGrowthStrategy manipStrategy,
            int recalcDistsFromCenterCount,
            int bfsExtraDepth,
            bool removeTurns,
            int numVisCoeff)
        {
            this.manipStrategy = manipStrategy;
            this.recalcDistsFromCenterCount = recalcDistsFromCenterCount;
            this.bfsExtraDepth = bfsExtraDepth;
            this.removeTurns = removeTurns;
            this.numVisCoeff = numVisCoeff;
        }

        public string Name => string.Join(
            "_",
            nameof(BfsStrategy),
            this.manipStrategy.Name,
            this.recalcDistsFromCenterCount,
            this.bfsExtraDepth,
            this.removeTurns ? "RT" : "KT",
            this.numVisCoeff);

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

            var distsFromCenter = map.DistsFromCenter;
            var distsFromCenterTimer = 0;
            var distsFromCenterTimerResetPeriod = Math.Max(10, 1 + (map.CellsToVisit.Count() / this.recalcDistsFromCenterCount));

            var bfs = new BfsState(state.Map);

            // for debugging purposes
            // var history = new List<(Command, State)>();
            Command Next(Command cmd)
            {
                var newState = state.Next(cmd) ?? throw new InvalidOperationException("Generated invalid move!");
                var wrappedDiff = newState.WrappedCellsCount - state.WrappedCellsCount;
                state = newState;

                distsFromCenterTimer += wrappedDiff;
                if (distsFromCenterTimer >= distsFromCenterTimerResetPeriod)
                {
                    distsFromCenter = new DistsFromCenter(state);
                    distsFromCenterTimer = 0;
                }

                // history.Add((cmd, state));
                return cmd;
            }

            while (state.WrappedCellsCount != map.CellsToVisit.Count)
            {
                Bfs(distsFromCenter);

                if (state.HaveManipulatorExtensions())
                {
                    yield return Next(this.manipStrategy.Grow(state));
                    continue;
                }

                for (var i = 0; i < bfs.Path.Count; ++i)
                {
                    // During walking we've found extension thingy - let's take it then!
                    if (state.HaveManipulatorExtensions())
                    {
                        yield return Next(this.manipStrategy.Grow(state));
                        break;
                    }

                    var bfsCommand = bfs.Path[i];

                    if (i >= bfs.Path.Count - 10)
                    {
                        // If close to bfs finish - try looking for optimal solution with beam search
                        var command = TryBeamSearch();
                        if (command != null && !command.Equals(bfsCommand))
                        {
                            // found a command that is different from bfs path, it invalidates bfs
                            // Console.WriteLine($"    BS: {command}, bfs[{i}/{bfs.Path.Count}]: {bfsCommand}");
                            yield return Next(command);
                            break;
                        }
                    }

                    // just go according to bfs
                    // Console.WriteLine($"        bfs[{i}/{bfs.Path.Count}]: {bfsCommand}");
                    yield return Next(bfsCommand);
                }
            }

            // just call it to silence compiler
            TryBeamSearchImpl();

            yield break;

            Command? TryBeamSearch() => null; // TryBeamSearchImpl();

            Command? TryBeamSearchImpl()
            {
                var bot = state.GetBot(0);
                var curWrappedCount = state.WrappedCellsCount;
                var beam = new StablePriorityQueue<WeightedState>(BeamSize + 1);
                var seenStates = new HashSet<int>();

                var startState = new WeightedState(state, null, distsFromCenter);
                beam.Enqueue(startState, startState.CalcPriority(bot.X, bot.Y));

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
                                    var nextWeightedState = new WeightedState(nextState, (prevState, command), distsFromCenter);
                                    beam.Enqueue(nextWeightedState, nextWeightedState.CalcPriority(bot.X, bot.Y));

                                    // remove worst states
                                    if (beam.Count > BeamSize)
                                    {
                                        beam.Dequeue();
                                    }

                                    // update the global best state if possible
                                    if (nextState.WrappedCellsCount >= curWrappedCount &&
                                        (bestState == null || nextWeightedState.CalcPriority(bot.X, bot.Y) > bestState.CalcPriority(bot.X, bot.Y)))
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

            void Bfs(DistsFromCenter distsFromCenter)
            {
                var bot = state.GetBot(0);

                ++bfs.Generation;
                bfs.Queue.Clear();
                bfs.Queue.Enqueue((bot.X, bot.Y, bot.Dir));
                bfs.Nodes[bot.X, bot.Y, bot.Dir] = new BfsState.Node(bfs.Generation, -1, 0);

                int? maxDepth = null;
                (int, int, int, bool isExtensionBooster, int numVis, int distFromCenter)? bestDest = null;

                while (bfs.Queue.Count > 0)
                {
                    var (x, y, dir) = bfs.Queue.Dequeue();
                    var depth = bfs.Nodes[x, y, dir].Depth;

                    Debug.Assert(bfs.Nodes[x, y, dir].Generation == bfs.Generation, "oops");

                    if (maxDepth != null && depth > maxDepth.Value)
                    {
                        if (bestDest == null)
                        {
                            throw new InvalidOperationException();
                        }

                        var (destX, destY, destDir, destIsExtensionBooster, destNumVis, bestDistFromCenter) = bestDest.Value;
                        FindBackwardPath(destX, destY, destDir);
                        return;
                    }

                    // path found, but search a bit more for deeper fruits
                    var isExtensionBooster = map[x, y] == Map.Cell.ManipulatorExtension && !state.IsPickedUp(x, y);
                    var (numVis, distFromCenter) = state.MaxUnwrappedVisibleDistFromCenter(x, y, dir, distsFromCenter);
                    if (numVis > 0 || isExtensionBooster)
                    {
                        if (maxDepth == null)
                        {
                            maxDepth = depth + this.bfsExtraDepth;
                        }

                        if (bestDest == null ||
                            Quality(isExtensionBooster, numVis, distFromCenter) > Quality(bestDest.Value.isExtensionBooster, bestDest.Value.numVis, bestDest.Value.distFromCenter))
                        {
                            bestDest = (x, y, dir, isExtensionBooster, numVis, distFromCenter);
                        }
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfs.Nodes[nx, ny, dir].Generation != bfs.Generation)
                        {
                            bfs.Nodes[nx, ny, dir] = new BfsState.Node(bfs.Generation, i, depth + 1);
                            bfs.Queue.Enqueue((nx, ny, dir));
                        }
                    }

                    for (var i = 0; i < Turn.All.Length; ++i)
                    {
                        var ddir = Turn.All[i].Ddir;
                        var ndir = (dir + ddir) & 3;
                        if (bfs.Nodes[x, y, ndir].Generation != bfs.Generation)
                        {
                            bfs.Nodes[x, y, ndir] = new BfsState.Node(bfs.Generation, -ddir, depth + 1);
                            bfs.Queue.Enqueue((x, y, ndir));
                        }
                    }
                }

                var (finalX, finalY, finalDir, p1, p2, p3) =
                    bestDest ?? throw new InvalidOperationException("Couldn't find any path with BFS!");
                FindBackwardPath(finalX, finalY, finalDir);

                int Quality(bool isBooster, int numVis, int distFromCenter1) =>
                    (isBooster ? 10000 : 0) + (this.numVisCoeff * numVis) + (10 * distFromCenter1);
            }

            void FindBackwardPath(int x, int y, int dir)
            {
                bfs.FindBackwardPath(x, y, dir, state.GetBot(0));

                // if there's more than one command, then filter out all turns
                if (this.removeTurns && bfs.Path.Count > 1)
                {
                    var writeIdx = 0;
                    for (var i = 0; i < bfs.Path.Count; ++i)
                    {
                        if (!(bfs.Path[i] is Turn))
                        {
                            bfs.Path[writeIdx++] = bfs.Path[i];
                        }
                    }

                    // degenerate case: 180 degree turn
                    if (writeIdx != 0)
                    {
                        bfs.Path.RemoveRange(writeIdx, bfs.Path.Count - writeIdx);
                    }
                }
            }
        }

        private class WeightedState : StablePriorityQueueNode
        {
            public WeightedState(State state, (WeightedState, Command)? prev, DistsFromCenter distsFromCenter)
            {
                var bot = state.GetBot(0);
                this.State = state;
                this.Prev = prev;
                this.BestVisibleCount = Math.Max(
                    this.State.MaxUnwrappedVisibleDistFromCenter(bot.X, bot.Y, bot.Dir, distsFromCenter).maxDist,
                    this.Prev?.state?.BestVisibleCount ?? 0);
            }

            public State State { get; }
            public (WeightedState state, Command command)? Prev { get; }
            public int BestVisibleCount { get; }

            public float CalcPriority(int startX, int startY) =>
                (128 * this.BestVisibleCount) +
                (8 * this.State.WrappedCellsCount) +
                (0 * Math.Abs(this.State.GetBot(0).X - startX)) +
                (0 * Math.Abs(this.State.GetBot(0).Y - startY)) +
                ((0.01f * this.State.Hash) / int.MaxValue);
        }
    }
}
