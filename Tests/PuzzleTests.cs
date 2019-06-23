namespace Icfpc2019.Tests
{
    using System;
    using System.IO;

    using Icfpc2019.Solution;

    using Xunit;

    public class PuzzleTests
    {
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
    }
}