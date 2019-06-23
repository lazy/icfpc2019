namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class SequentialStrategy : IStrategy
    {
        private readonly IStrategy[] subStrategies;

        public SequentialStrategy(params IStrategy[] subStrategies)
        {
            this.subStrategies = subStrategies;
        }

        public string Name => "(" + string.Join("->", this.subStrategies.Select(strat => strat.Name)) + ")";

        public IEnumerable<Command[]> Solve(State state)
        {
            foreach (var strategy in this.subStrategies)
            {
                foreach (var botCommands in strategy.Solve(state))
                {
                    var nextState = state.Next(botCommands);
                    if (nextState == null)
                    {
                        throw new Exception("Some strat is broken");
                    }

                    yield return botCommands;
                }
            }
        }
    }
}
