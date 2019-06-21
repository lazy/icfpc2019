﻿namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class DumbBfs : IStrategy
    {
        private static readonly Point[] Deltas =
        {
            new Point { X = 1, Y = 0 },
            new Point { X = -1, Y = 0 },
            new Point { X = 0, Y = 1 },
            new Point { X = 0, Y = -1 },
        };

        private static readonly Move[] Moves =
        {
            Move.MoveRight,
            Move.MoveLeft,
            Move.MoveUp,
            Move.MoveDown,
        };

        public IEnumerable<Move> Solve(Map map)
        {
            var startPoint = new Point { X = map.StartX, Y = map.StartY };
            var toVisit = map.CellsToVisit.Select(x => new Point { X = x.Item1, Y = x.Item2 }).ToHashSet();

            while (toVisit.Count > 0)
            {
                var result = this.RunBfs(map, startPoint, toVisit);
                if (result == null)
                {
                    throw new Exception("The BFS should have returned a result");
                }

                foreach (var m in result.PathFragment)
                {
                    yield return m;
                }

                startPoint = result.FirstReachedPoint;
                foreach (var t in result.TouchedPoints)
                {
                    toVisit.Remove(t);
                }
            }
        }

        private BfsResult? RunBfs(Map map, Point startPoint, HashSet<Point> toVisit)
        {
            var queue = new Queue<Point>();
            var visited = new HashSet<Point>();
            var pathTrace = new Dictionary<Point, (Move, Point)>();
            visited.Add(startPoint);
            queue.Enqueue(startPoint);

            while (queue.Count > 0)
            {
                var curPoint = queue.Dequeue();

                if (toVisit.Contains(curPoint))
                {
                    var result = new BfsResult
                    {
                        FirstReachedPoint = curPoint,
                    };
                    var pathPoint = curPoint;

                    Action<Point> addTouchedPoints = point =>
                    {
                        // FIXME: this code assumes we face right and do not rotate ever
                        result.TouchedPoints.Add(point);

                        for (var tdy = -1; tdy <= 1; ++tdy)
                        {
                            var touchCandidate = new Point { X = point.X + 1, Y = point.Y + tdy };
                            if (map.IsFree(touchCandidate.X, touchCandidate.Y))
                            {
                                result.TouchedPoints.Add(touchCandidate);
                            }
                        }
                    };

                    addTouchedPoints(curPoint);
                    while (pathTrace.ContainsKey(pathPoint))
                    {
                        var tr = pathTrace[pathPoint];
                        result.PathFragment.Add(tr.Item1);
                        pathPoint = tr.Item2;
                        addTouchedPoints(pathPoint);
                    }

                    result.PathFragment.Reverse();
                    result.FirstReachedPoint = curPoint;
                    result.TouchedPoints.Add(curPoint);
                    return result;
                }

                for (var idx = 0; idx < Deltas.Length; ++idx)
                {
                    var delta = Deltas[idx];
                    var nextPoint = new Point { X = curPoint.X + delta.X, Y = curPoint.Y + delta.Y };
                    if (!visited.Contains(nextPoint) && map.IsFree(nextPoint.X, nextPoint.Y))
                    {
                        visited.Add(nextPoint);
                        queue.Enqueue(nextPoint);
                        pathTrace.Add(nextPoint, (Moves[idx], curPoint));
                    }
                }
            }

            return null;
        }

        private class BfsResult
        {
            public HashSet<Point> TouchedPoints { get; } = new HashSet<Point>();
            public List<Move> PathFragment { get; } = new List<Move>();
            public Point FirstReachedPoint { get; set; }
        }
    }
}