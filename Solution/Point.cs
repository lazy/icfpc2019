namespace Icfpc2019.Solution
{
    using System;

    public struct Point : IEquatable<Point>
    {
        public int X { get; set; }

        public int Y { get; set; }

        public static bool operator !=(Point left, Point right) => !left.Equals(right);

        public static bool operator ==(Point left, Point right) => left.Equals(right);

        public bool Equals(Point other) => this.X == other.X && this.Y == other.Y;

        public override bool Equals(object obj) => obj is Point other && this.Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.X * 397) ^ this.Y;
            }
        }

        public override string ToString() => string.Format("({0}, {1})", this.X, this.Y);
    }
}