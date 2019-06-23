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

        public Command[][] Solve(State state)
        {
            var result = new List<List<Command>>();

            foreach (var strategy in this.subStrategies)
            {
                foreach (var botCommands in strategy.Solve(state))
                {
                    if (botCommands.Length == 0)
                    {
                        // Some strategy decided that it aint gonna work
                        return new Command[][] { };
                    }

                    if (botCommands.Length < result.Count)
                    {
                        throw new Exception("Can't produce commands than bots exist currently");
                    }

                    var nextState = state.Next(botCommands);
                    if (nextState == null)
                    {
                        throw new Exception("Some strat is broken");
                    }

                    for (var i = 0; i < botCommands.Length; ++i)
                    {
                        if (i >= result.Count)
                        {
                            result.Add(new List<Command>());
                        }

                        result[i].Add(botCommands[i]);
                    }
                }
            }

            return result.Select(bc => bc.ToArray()).ToArray();
        }
    }
}
