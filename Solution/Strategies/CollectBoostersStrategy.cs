namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class CollectBoostersFactory
    {
        private static int[] boostersCount = { 0, 1, 2, 3, 4, 8, 10000 };

        public static IEnumerable<IStrategy> MakeStrategies(ManipulatorGrowthStrategy manipStrategy)
        {
            foreach (var boostersCount in boostersCount)
            {
                yield return new CollectBoostersStrategy(boostersCount, manipStrategy);
            }
        }

        public static int PrevCount(int sz)
        {
            var prev = -1;
            foreach (var s in boostersCount)
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

    // Collects extension boosters. Doesn't solve whole map
    public class CollectBoostersStrategy : IStrategy
    {
        private readonly int boostersCount;
        private readonly ManipulatorGrowthStrategy manipStrategy;

        public CollectBoostersStrategy(int boostersCount, ManipulatorGrowthStrategy manipStrategy)
        {
            this.boostersCount = boostersCount;
            this.manipStrategy = manipStrategy;
        }

        public string Name => $"CollectBoosters{this.boostersCount}_{this.manipStrategy.Name}";

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

            // No point in running if there's another strategy that will collect everything
            if (CollectBoostersFactory.PrevCount(this.boostersCount) >= map.NumManipulatorExtensions)
            {
                throw new SkipStrategyException();
            }

            var remainingBoostersCount = Math.Min(state.ManipulatoExtensionsOnTheFloorCount, this.boostersCount);

            Command Next(Command cmd)
            {
                state = state.Next(cmd) ?? throw new InvalidOperationException("Generated invalid move!");

                if (cmd is UseManipulatorExtension)
                {
                    --remainingBoostersCount;
                }

                return cmd;
            }

            while (remainingBoostersCount > 0)
            {
                foreach (var cmd in FindManipulatorExtension())
                {
                    yield return Next(cmd);
                }

                while (state.HaveManipulatorExtensions())
                {
                    yield return Next(this.manipStrategy.Grow(state));
                }
            }

            IEnumerable<Command> FindManipulatorExtension()
            {
                var bot = state.GetBot(0);

                ++bfs.Generation;
                bfs.Queue.Clear();
                bfs.Queue.Enqueue((bot.X, bot.Y, bot.Dir));
                bfs.Nodes[bot.X, bot.Y, bot.Dir] = new BfsState.Node(bfs.Generation, -1, 0);
                while (bfs.Queue.Count > 0)
                {
                    var (x, y, dir) = bfs.Queue.Dequeue();
                    Debug.Assert(bfs.Nodes[x, y, dir].Generation == bfs.Generation, "oops");

                    if (map[x, y] == Map.Cell.ManipulatorExtension && !state.IsPickedUp(x, y))
                    {
                        bfs.FindBackwardPath(x, y, dir, bot);
                        return bfs.Path;
                    }

                    for (var i = 0; i < Move.All.Length; ++i)
                    {
                        var move = Move.All[i];
                        var nx = x + move.Dx;
                        var ny = y + move.Dy;
                        if (map.IsFree(nx, ny) && bfs.Nodes[nx, ny, dir].Generation != bfs.Generation)
                        {
                            bfs.Nodes[nx, ny, dir] = new BfsState.Node(bfs.Generation, i, 0);
                            bfs.Queue.Enqueue((nx, ny, dir));
                        }
                    }
                }

                throw new InvalidOperationException();
            }
        }
    }
}
