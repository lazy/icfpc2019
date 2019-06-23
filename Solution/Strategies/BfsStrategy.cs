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

        public string Name =>
            nameof(BfsStrategy) + "(" + string.Join(
                ",",
                this.manipStrategy.Name,
                this.recalcDistsFromCenterCount,
                this.bfsExtraDepth,
                this.removeTurns ? "RT" : "KT",
                this.numVisCoeff) + ")";

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

                foreach (var cmd in bfs.Path)
                {
                    // During walking we've found extension thingy - let's take it then!
                    if (state.HaveManipulatorExtensions())
                    {
                        yield return Next(this.manipStrategy.Grow(state));
                        break;
                    }

                    yield return Next(cmd);
                }
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

                    if (bfs.Nodes[x, y, dir].Generation != bfs.Generation)
                    {
                        throw new Exception("oops");
                    }

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
    }
}
