namespace Icfpc2019.Solution
{
    using System.Collections.Generic;

    using Icfpc2019.Solution.Strategies;

    public static class StrategyFactory
    {
        public static IEnumerable<IStrategy> GenerateStrategies()
        {
            yield return new DumbBfs();

            var manipStrategies = ManipulatorGrowthFactory.MakeStrategies();
            foreach (var manipStrategy in manipStrategies)
            {
                foreach (var collect in CollectBoostersFactory.MakeStrategies(manipStrategy))
                {
                    foreach (var bfs in BfsFactory.MakeStrategies(manipStrategy))
                    {
                        yield return new SequentialStrategy(collect, bfs);
                    }
                }
            }
        }
    }
}