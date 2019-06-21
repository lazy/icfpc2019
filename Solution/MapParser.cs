namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class MapParser
    {
        public static Map Parse(string description)
        {
            var tokens = description.Split("#");
            var allPoints = new AllPoints();
            var edges = ParseContours(tokens[0], allPoints, false);
            var startPoint = ParsePoint(tokens[1]);
            var obstacles = ParseContours(tokens[2], allPoints, true);

            var cells = new Map.Cell[allPoints.MaxX + 1, allPoints.MaxY + 1];

            foreach (var e in edges)
            {
                cells[e.X, e.Y] = Map.Cell.Edge;
            }

            foreach (var o in obstacles)
            {
                cells[o.X, o.Y] = Map.Cell.Obstacle;
            }

            ParseBoosters(tokens[3], cells);

            return new Map(startPoint.X, startPoint.Y, cells);
        }

        private static List<Point> ParseContours(string description, AllPoints allPoints, bool inside)
        {
            var contours = description.Split(';');
            var pointsByContour = contours.Select(x => x.Split("),").Select(ParsePoint).ToArray()).ToArray();
            var directions = new[]
            {
                new Point { X = 1, Y = 0 },
                new Point { X = 0, Y = -1 },
                new Point { X = -1, Y = 0 },
                new Point { X = 0, Y = 1 },
            };
            var insideDirections = new[]
            {
                new Point { X = 0, Y = 0 },
                new Point { X = 0, Y = -1 },
                new Point { X = -1, Y = -1 },
                new Point { X = -1, Y = 0 },
            };
            var outsideDirections = new[]
            {
                new Point { X = 0, Y = -1 },
                new Point { X = -1, Y = -1 },
                new Point { X = -1, Y = 0 },
                new Point { X = 0, Y = 0 },
            };
            var edgeDirections = inside ? insideDirections : outsideDirections;
            var result = new List<Point>();
            foreach (var contour in pointsByContour)
            {
                for (var i = 0; i < contour.Length; ++i)
                {
                    var (pCur, pNext) = (contour[i], contour[(i + 1) % contour.Length]);
                    var dPoint = new Point { X = Math.Sign(pNext.X - pCur.X), Y = Math.Sign(pNext.Y - pCur.Y) };
                    var directionIndex = Array.IndexOf(directions, dPoint);
                    Trace.Assert(directionIndex >= 0);

                    var dEdge = edgeDirections[directionIndex];
                    var curX = pCur.X;
                    var curY = pCur.Y;
                    var nextReached = false;
                    while (!nextReached)
                    {
                        var edgePoint = new Point { X = curX + dEdge.X, Y = curY + dEdge.Y };
                        if (allPoints.Update(edgePoint))
                        {
                            result.Add(edgePoint);
                        }

                        curX += dPoint.X;
                        curY += dPoint.Y;
                        nextReached = curX == pNext.X && curY == pNext.Y;
                    }
                }
            }

            return result;
        }

        private static void ParseBoosters(string description, Map.Cell[,] cells)
        {
            foreach (var booster in description.Split(';'))
            {
                var boosterPos = ParsePoint(booster.Substring(1));
                cells[boosterPos.X, boosterPos.Y] = booster[0] switch
                {
                    'B' => Map.Cell.BoosterB,
                    'F' => Map.Cell.BoosterF,
                    'L' => Map.Cell.BoosterD,
                    'X' => Map.Cell.BoosterX,
                    var ch => throw new Exception(string.Format("Unknown booster type", ch)),
                };
            }
        }

        private static Point ParsePoint(string description)
        {
            var tokens = description.Trim('(', ')').Split(',');
            return new Point
            {
                X = int.Parse(tokens[0]),
                Y = int.Parse(tokens[1]),
            };
        }

        private struct Point : IEquatable<Point>
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
        }

        private class AllPoints
        {
            public AllPoints()
            {
                this.Points = new HashSet<Point>();
            }

            public HashSet<Point> Points { get; }

            public int MaxX { get; private set; }

            public int MaxY { get; private set; }

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
}