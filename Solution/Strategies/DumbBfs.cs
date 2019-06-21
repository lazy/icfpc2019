namespace Icfpc2019.Solution.Strategies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

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
            var pathFragment = new List<Move>();
            Point? reachedPoint;
            var toVisit = this.CollectReachablePoints(map, startPoint, null, pathFragment, out reachedPoint);

            while (toVisit.Count > 0)
            {
                var touched = this.CollectReachablePoints(map, startPoint, toVisit, pathFragment, out reachedPoint);
                foreach (var m in pathFragment)
                {
                    yield return m;
                }

                if (reachedPoint == null)
                {
                    throw new Exception("The BFS should have return a point from toVisit");
                }

                startPoint = reachedPoint.Value;
                foreach (var t in touched)
                {
                    toVisit.Remove(t);
                }
            }
        }

        private HashSet<Point> CollectReachablePoints(Map map, Point startPoint, HashSet<Point>? toVisit, List<Move> pathFragment, out Point? reachedPoint)
        {
            reachedPoint = null;

            var queue = new Queue<Point>();
            var visited = new HashSet<Point>();
            var pathTrace = new Dictionary<Point, (Move, Point)>();
            var touchedPoints = new HashSet<Point>();
            visited.Add(startPoint);
            queue.Enqueue(startPoint);

            var result = new HashSet<Point>();

            while (queue.Count > 0)
            {
                var curPoint = queue.Dequeue();

                if (toVisit != null && toVisit.Contains(curPoint))
                {
                    var pathPoint = curPoint;
                    pathFragment.Clear();

                    Action<Point> addTouchedPoints = point =>
                    {
                        // FIXME: this code assumes we face right and do not rotate ever
                        touchedPoints.Add(point);

                        for (var tdy = -1; tdy <= 1; ++tdy)
                        {
                            var touchCandidate = new Point { X = point.X + 1, Y = point.Y + tdy };
                            if (map.IsFree(touchCandidate.X, touchCandidate.Y))
                            {
                                touchedPoints.Add(touchCandidate);
                            }
                        }
                    };

                    addTouchedPoints(curPoint);
                    while (pathTrace.ContainsKey(pathPoint))
                    {
                        var tr = pathTrace[pathPoint];
                        pathFragment.Add(tr.Item1);
                        pathPoint = tr.Item2;
                        addTouchedPoints(pathPoint);
                    }

                    pathFragment.Reverse();
                    reachedPoint = curPoint;
                    touchedPoints.Add(curPoint);
                    return touchedPoints;
                }

                for (var idx = 0; idx < Deltas.Length; ++idx)
                {
                    var delta = Deltas[idx];
                    var nextPoint = new Point { X = curPoint.X + delta.X, Y = curPoint.Y + delta.Y };
                    if (!visited.Contains(nextPoint) && map.IsFree(nextPoint.X, nextPoint.Y))
                    {
                        visited.Add(nextPoint);
                        queue.Enqueue(nextPoint);
                        result.Add(nextPoint);
                        pathTrace.Add(nextPoint, (Moves[idx], curPoint));
                    }
                }
            }

            Trace.Assert(toVisit == null);
            return result;
        }
    }
}