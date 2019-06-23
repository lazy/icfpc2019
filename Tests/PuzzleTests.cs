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
            puzzle14.EnsureMapIsValid(map14);
        }
    }
}