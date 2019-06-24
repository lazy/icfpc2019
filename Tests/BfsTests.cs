namespace Icfpc2019.Tests
{
    using System;
    using System.IO;

    using Icfpc2019.Solution;
    using Icfpc2019.Solution.Strategies;

    using Xunit;
    using Xunit.Abstractions;

    public class BfsTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public BfsTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void RunDifferentBfs()
        {
            var map = MapParser.Parse(File.ReadAllText("../../../../Data/maps/prob-142.desc"), string.Empty);

            void Measure(IStrategy strat)
            {
                var solution = Emulator.MakeExtendedSolution(map, strat, string.Empty);
                File.WriteAllText($"../../../../Data/prob-142-{solution.StrategyName}.sol", solution.Commands);
                this.testOutputHelper.WriteLine($"{strat.Name}: {solution.TimeUnits}");
            }

            Measure(new DumbBfs());
            Measure(new DumbBfs(false));
            Measure(new DumbLookAheadBfs(0));
            Measure(new DumbLookAheadBfs(1));
            Measure(new DumbLookAheadBfs(2));
            Measure(new DumbLookAheadBfs(3));
            Measure(new DumbLookAheadBfs(10));
        }
    }
}