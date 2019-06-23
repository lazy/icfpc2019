namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;

    public static class ManipulatorGrowthFactory
    {
        public static IEnumerable<ManipulatorGrowthStrategy> MakeStrategies()
        {
            yield return new ManipulatorGrowthStrategy(1, ManipulatorGrowthStrategy.Kind.SymLine);
            yield return new ManipulatorGrowthStrategy(-1, ManipulatorGrowthStrategy.Kind.SymLine);
            yield return new ManipulatorGrowthStrategy(1, ManipulatorGrowthStrategy.Kind.AssymLine);
            yield return new ManipulatorGrowthStrategy(-1, ManipulatorGrowthStrategy.Kind.AssymLine);
        }
    }

    public class ManipulatorGrowthStrategy : IStrategy
    {
        private readonly int initSign;
        private readonly Kind kind;

        public ManipulatorGrowthStrategy(int initSign, Kind kind)
        {
            this.initSign = initSign;
            this.kind = kind;
        }

        public enum Kind
        {
            SymLine,
            AssymLine,
            /*
            SymBox,
            AssymBox
            */
        }

        public string Name => $"${this.kind}{(this.initSign > 0 ? 'L' : 'R')}";

        public IEnumerable<Command[]> Solve(State state)
        {
            if (state.BotsCount > 1)
            {
                throw new Exception("This strategy works only with 1 bot");
            }

            yield return new[] { this.Grow(state) };
        }

        public Command Grow(State state)
        {
            var bot = state.GetBot(0);
            switch (this.kind)
            {
                case Kind.SymLine:
                {
                    var extensionDist = bot.ManipConfig.Length / 2;
                    var sign = this.initSign * (bot.ManipConfig.Length % 2 == 0 ? 1 : -1);
                    var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                    return new UseManipulatorExtension(dx, dy);
                }

                case Kind.AssymLine:
                {
                    var extensionDist = bot.ManipConfig.Length - 2;
                    var sign = this.initSign;
                    if (extensionDist > 4)
                    {
                        extensionDist -= 3;
                        sign *= -1;
                    }

                    var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                    return new UseManipulatorExtension(dx, dy);
                }

                default:
                    throw new Exception($"Unexpected kind: {this.kind}");
            }
        }
    }
}
