namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;

    public class AllPoints
    {
        public AllPoints()
        {
            this.Points = new HashSet<Point>();
        }

        public int MaxX { get; private set; }

        public int MaxY { get; private set; }

        public HashSet<Point> Points { get; }

        public bool Update(Point p)
        {
            if (p.X < 0 || p.Y < 0)
            {
                return false;
            }

            this.Points.Add(p);

            this.MaxX = Math.Max(this.MaxX, p.X);
            this.MaxY = Math.Max(this.MaxY, p.Y);

            return true;
        }
    }
}