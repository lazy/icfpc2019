namespace Icfpc2019.Solution
{
    using System.Collections.Generic;

    public interface IStrategy
    {
        string Name { get; }

        IEnumerable<Move> Solve(Map map);
    }
}
