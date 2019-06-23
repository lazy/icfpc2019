namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Priority_Queue;

    public static class LookAheadFactory
    {
        private static int[] forcedManipulatorExtensionsCount = { 0, 1, 2, 3, 4, 8, 10000 };
        public static IEnumerable<IStrategy> MakeStrategies()
        {
            foreach (var recalcsTime in new[] { 1, 10 })
            {
                foreach (var sym in new[] { true, false })
                {
                    foreach (var growthSign in new[] { -1, 1 })
                    {
                        if (sym && growthSign < 0)
                        {
                            continue;
                        }

                        foreach (var forcedManipulatorExtensionsCount in forcedManipulatorExtensionsCount)
                        {
                            foreach (var extraDepth in new[] { 1, 2, 3, 4, 5, })
                            {
                                foreach (var removeTurns in new[] { true, false })
                                {
                                    foreach (var numVisCoeff in new[] { 0, 1 })
                                    {
                                        yield return new LookAheadStrategy(
                                            sym,
                                            growthSign,
                                            recalcsTime,
                                            forcedManipulatorExtensionsCount,
                                            extraDepth,
                                            removeTurns,
                                            numVisCoeff);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static int PrevSize(int sz)
        {
            var prev = -1;
            foreach (var s in forcedManipulatorExtensionsCount)
            {
                if (s >= sz)
                {
                    return prev;
                }

                prev = s;
            }

            throw new InvalidOperationException();
        }
    }

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

        private readonly bool symmetricGrowth;
        private readonly int initSignGrowth;
        private readonly int recalcDistsFromLeafsCount;
        private readonly int forcedManipulatorExtensionsCount;
        private readonly int bfsExtraDepth;
        private readonly bool removeTurns;
        private readonly int numVisCoeff;

        public LookAheadStrategy(
            bool symmetricGrowth,
            int initSignGrowth,
            int recalcDistsFromLeafsCount,
            int forcedManipulatorExtensionsCount,
            int bfsExtraDepth,
            bool removeTurns,
            int numVisCoeff)
        {
            this.symmetricGrowth = symmetricGrowth;
            this.initSignGrowth = initSignGrowth;
            this.recalcDistsFromLeafsCount = recalcDistsFromLeafsCount;
            this.forcedManipulatorExtensionsCount = forcedManipulatorExtensionsCount;
            this.bfsExtraDepth = bfsExtraDepth;
            this.removeTurns = removeTurns;
            this.numVisCoeff = numVisCoeff;
        }

        public string Name => string.Join(
            "_",
            nameof(LookAheadStrategy),
            this.symmetricGrowth ? "Sym" : "Assym",
            this.initSignGrowth > 0 ? "L" : "R",
            this.recalcDistsFromLeafsCount,
            this.forcedManipulatorExtensionsCount,
            this.bfsExtraDepth,
            this.removeTurns ? "RT" : "KT",
            this.numVisCoeff);

        public Command[][] Solve(State state) =>
            new[] { this.Solve1(state).ToArray() };

        public IEnumerable<Command> Solve1(State state)
        {
            var map = state.Map;

            // No point in running if there's another strategy that will collect everything
            if (LookAheadFactory.PrevSize(this.forcedManipulatorExtensionsCount) >= map.NumManipulatorExtensions)
            {
                yield break;
            }

            var forcedExtensionsLeft = Math.Min(map.NumManipulatorExtensions, this.forcedManipulatorExtensionsCount);

            var generation = 0;

            // reuse to reduce memory consumption
            var bfsNodes = new BfsNode[map.Width, map.Height, 4];
            var bfsQueue = new Queue<(int, int, int)>();
            var bfsPath = new List<Command>();

            var distsFromLeafs = map.DistsFromLeafs;
            var distsFromLeafsTimer = 0;
            var distsFromLeafsTimerResetPeriod = Math.Max(10, 1 + (map.CellsToVisit.Count() / this.recalcDistsFromLeafsCount));

            // for debugging purposes
            // var history = new List<(Command, State)>();
            Command Next(Command cmd)
            {
                var newState = state.Next(cmd) ?? throw new InvalidOperationException("Generated invalid move!");
                var wrappedDiff = newState.WrappedCellsCount - state.WrappedCellsCount;
                state = newState;

                distsFromLeafsTimer += wrappedDiff;
                if (distsFromLeafsTimer >= distsFromLeafsTimerResetPeriod)
                {
                    distsFromLeafs = new DistsFromLeafs(state);
                    distsFromLeafsTimer = 0;
                }

                if (cmd is UseManipulatorExtension)
                {
                    --forcedExtensionsLeft;
                }

                // history.Add((cmd, state));
                return cmd;
            }

            while (forcedExtensionsLeft > 0)
            {
                foreach (var cmd in FindManipulatorExtension())
                {
                    yield return Next(cmd);
                }

                while (HaveManipulatorExtensions())
                {
                    yield return Next(ExtendManipulator());
                }
            }

            while (state.WrappedCellsCount != map.CellsToVisit.Count)
            {
                Bfs(distsFromLeafs);

                if (HaveManipulatorExtensions())
                {
                    yield return Next(ExtendManipulator());
                    continue;
                }

                for (var i = 0; i < bfsPath.Count; ++i)
                {
                    // During walking we've found extension thingy - let's take it then!
                    if (HaveManipulatorExtensions())
                    {
                        yield return Next(ExtendManipulator());
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

            bool HaveManipulatorExtensions()
            {
                var bot = state.GetBot(0);
                return
                    state.ManipulatorExtensionCount > 0 ||
                    (map[bot.X, bot.Y] == Map.Cell.ManipulatorExtension && !state.IsPickedUp(bot.X, bot.Y));
            }

            Command ExtendManipulator()
            {
                var bot = state.GetBot(0);
                if (this.symmetricGrowth)
                {
                    var extensionDist = bot.ManipConfig.Length / 2;
                    var sign = this.initSignGrowth * (bot.ManipConfig.Length % 2 == 0 ? 1 : -1);
                    var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                    return new UseManipulatorExtension(dx, dy);
                }
                else
                {
                    var extensionDist = bot.ManipConfig.Length - 2;
                    var sign = this.initSignGrowth;
                    if (extensionDist > 4)
                    {
                        extensionDist -= 3;
                        sign *= -1;
                    }

                    var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                    return new UseManipulatorExtension(dx, dy);
                }
            }

            Command? TryBeamSearch() => null; // TryBeamSearchImpl();

            Command? TryBeamSearchImpl()
            {
                var bot = state.GetBot(0);
                var curWrappedCount = state.WrappedCellsCount;
                var beam = new StablePriorityQueue<WeightedState>(BeamSize + 1);
                var seenStates = new HashSet<int>();

                var startState = new WeightedState(state, null, distsFromLeafs);
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
                                    var nextWeightedState = new WeightedState(nextState, (prevState, command), distsFromLeafs);
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

            IEnumerable<Command> FindManipulatorExtension()
            {
                var bot = state.GetBot(0);

                ++generation;
                bfsQueue.Clear();
                bfsQueue.Enqueue((bot.X, bot.Y, bot.Dir));
                bfsNodes[bot.X, bot.Y, bot.Dir] = new BfsNode(generation, -1, 0);
                while (bfsQueue.Count > 0)
                {
                    var (x, y, dir) = bfsQueue.Dequeue();
                    Debug.Assert(bfsNodes[x, y, dir].Generation == generation, "oops");

                    if (map[x, y] == Map.Cell.ManipulatorExtension && !state.IsPickedUp(x, y))
                    {
                        FindBackwardPath(x, y, dir);
                        return bfsPath;
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfsNodes[nx, ny, dir].Generation != generation)
                        {
                            bfsNodes[nx, ny, dir] = new BfsNode(generation, i, 0);
                            bfsQueue.Enqueue((nx, ny, dir));
                        }
                    }
                }

                throw new InvalidOperationException();
            }

            void Bfs(DistsFromLeafs distsFromLeafs)
            {
                var bot = state.GetBot(0);

                ++generation;
                bfsQueue.Clear();
                bfsQueue.Enqueue((bot.X, bot.Y, bot.Dir));
                bfsNodes[bot.X, bot.Y, bot.Dir] = new BfsNode(generation, -1, 0);

                int? maxDepth = null;
                (int, int, int, bool isExtensionBooster, int numVis, int distFromLeafs)? bestDest = null;

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

                        var (destX, destY, destDir, destIsExtensionBooster, destNumVis, bestDistFromLeafs) = bestDest.Value;
                        FindBackwardPath(destX, destY, destDir);
                        return;
                    }

                    // path found, but search a bit more for deeper fruits
                    var isExtensionBooster = map[x, y] == Map.Cell.ManipulatorExtension && !state.IsPickedUp(x, y);
                    var (numVis, distFromLeafs) = state.MinUnwrappedVisibleDistFromLeafs(x, y, dir, distsFromLeafs);
                    if (numVis > 0 || isExtensionBooster)
                    {
                        if (maxDepth == null)
                        {
                            maxDepth = depth + this.bfsExtraDepth;
                        }

                        if (bestDest == null ||
                            Quality(isExtensionBooster, numVis, distFromLeafs) > Quality(bestDest.Value.isExtensionBooster, bestDest.Value.numVis, bestDest.Value.distFromLeafs))
                        {
                            bestDest = (x, y, dir, isExtensionBooster, numVis, distFromLeafs);
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

                var (finalX, finalY, finalDir, p1, p2, p3) =
                    bestDest ?? throw new InvalidOperationException("Couldn't find any path with BFS!");
                FindBackwardPath(finalX, finalY, finalDir);

                int Quality(bool isBooster, int numVis, int distFromLeafs1) =>
                    (isBooster ? 10000 : 0) + (this.numVisCoeff * numVis) - (10 * distFromLeafs1);
            }

            void FindBackwardPath(int x, int y, int dir)
            {
                var bot = state.GetBot(0);

                bfsPath.Clear();
                var xx = x;
                var yy = y;
                while ((x, y, dir) != (bot.X, bot.Y, bot.Dir))
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
                if (this.removeTurns && bfsPath.Count > 1)
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
            public WeightedState(State state, (WeightedState, Command)? prev, DistsFromLeafs distsFromLeafs)
            {
                var bot = state.GetBot(0);
                this.State = state;
                this.Prev = prev;
                this.BestVisibleCount = Math.Max(
                    this.State.MinUnwrappedVisibleDistFromLeafs(bot.X, bot.Y, bot.Dir, distsFromLeafs).maxDist,
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
