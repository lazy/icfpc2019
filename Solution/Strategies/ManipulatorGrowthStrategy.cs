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
            yield return new ManipulatorGrowthStrategy(1, ManipulatorGrowthStrategy.Kind.AsymLine);
            yield return new ManipulatorGrowthStrategy(-1, ManipulatorGrowthStrategy.Kind.AsymLine);

            yield return new ManipulatorGrowthStrategy(1, ManipulatorGrowthStrategy.Kind.SymBox);
            yield return new ManipulatorGrowthStrategy(-1, ManipulatorGrowthStrategy.Kind.SymBox);
            yield return new ManipulatorGrowthStrategy(1, ManipulatorGrowthStrategy.Kind.AsymBox);
            yield return new ManipulatorGrowthStrategy(-1, ManipulatorGrowthStrategy.Kind.AsymBox);
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
            AsymLine,
            SymBox,
            AsymBox,
        }

        public string Name => $"{this.kind}{(this.initSign > 0 ? 'L' : 'R')}";

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
                    return MakeSymLineCmd(bot.ManipConfig.Length);
                case Kind.AsymLine:
                    return MakeAsymLineCmd(bot.ManipConfig.Length);
                case Kind.SymBox:
                    return bot.ManipConfig.Length < 9
                        ? MakeBoxCmd(bot.ManipConfig.Length)
                        : MakeSymLineCmd(bot.ManipConfig.Length - 5);
                case Kind.AsymBox:
                    return bot.ManipConfig.Length < 9
                        ? MakeBoxCmd(bot.ManipConfig.Length)
                        : MakeAsymLineCmd(bot.ManipConfig.Length - 5);
                default:
                    throw new Exception($"Unexpected kind: {this.kind}");
            }

            UseManipulatorExtension MakeSymLineCmd(int curManipSize)
            {
                var extensionDist = curManipSize / 2;
                var sign = this.initSign * (curManipSize % 2 == 0 ? 1 : -1);
                var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                return new UseManipulatorExtension(dx, dy);
            }

            UseManipulatorExtension MakeAsymLineCmd(int curManipSize)
            {
                var extensionDist = curManipSize - 2;
                var sign = this.initSign;
                if (extensionDist > 4)
                {
                    extensionDist -= 3;
                    sign *= -1;
                }

                var (dx, dy) = State.TurnManip(bot.Dir, (1, extensionDist * sign));
                return new UseManipulatorExtension(dx, dy);
            }

            UseManipulatorExtension MakeBoxCmd(int curManipSize)
            {
                var (dx, dy) = State.TurnManip(
                    bot.Dir,
                    curManipSize switch {
                        4 => (0, this.initSign),
                        5 => (0, -this.initSign),
                        6 => (-1, this.initSign),
                        7 => (-1, -this.initSign),
                        8 => (-1, 0),
                        _ => throw new Exception("impossible"),
                        });

                return new UseManipulatorExtension(dx, dy);
            }
        }
    }
}
