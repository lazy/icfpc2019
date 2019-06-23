namespace Icfpc2019.Solution
{
    using System;

    public class SkipStrategyException : Exception
    {
        public SkipStrategyException(string? message = null)
            : base(message)
        {
        }
    }
}