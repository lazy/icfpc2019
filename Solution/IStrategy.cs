namespace Icfpc2019.Solution
{
    using System.Collections.Generic;

    public interface IStrategy
    {
        string Name => this.GetType().Name;

        Command[][] Solve(State state);
    }
}