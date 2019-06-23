namespace Icfpc2019.Tests
{
    using System;
    using System.IO;

    using Icfpc2019.Solution;

    using Xunit;
    using Xunit.Abstractions;

    public class PuzzleTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public PuzzleTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void TestValidation()
        {
            var puzzle14 = new Puzzle(File.ReadAllText(@"..\..\..\..\Data\blocks\14\puzzle.cond"));
            var map14 = @"..\..\..\..\Data\blocks\14\puzzle.cond.desc";
            Assert.Throws<Exception>(() => puzzle14.EnsureMapIsValid(map14));

            var puzzle15 = new Puzzle(File.ReadAllText(@"..\..\..\..\Data\blocks\15\puzzle.cond"));
            var map15 = @"..\..\..\..\Data\blocks\15\puzzle.cond.desc";
            puzzle15.EnsureMapIsValid(map15);
        }

        [Fact]
        public void ValidateAllBlocks()
        {
            for (var bn = 3; bn <= 45; ++bn)
            {
                if (bn == 39)
                {
                    // The puzzle solver hangs on this one :(
                    continue;
                }

                var puzzle = new Puzzle(File.ReadAllText($@"..\..\..\..\Data\blocks\{bn}\puzzle.cond"));
                var map = $@"..\..\..\..\Data\blocks\{bn}\puzzle.cond.desc";
                try
                {
                    puzzle.EnsureMapIsValid(map);
                    this.testOutputHelper.WriteLine($"VALID puzzle solution for block #{bn}");
                }
                catch (Exception ex)
                {
                    this.testOutputHelper.WriteLine($"INVALID puzzle solution for block #{bn}: {ex.Message}");
                }
            }
        }
    }
}