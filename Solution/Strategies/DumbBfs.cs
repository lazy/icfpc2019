namespace Icfpc2019.Solution.Strategies
{
    using System.Collections.Generic;
    using System.Diagnostics;
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
            var pathFragment = new List<Move>();
            var toVisit = this.CollectReachablePoints(map, startPoint, null, pathFragment);

            while (toVisit.Count > 0)
            {
                var nextDestinations = this.CollectReachablePoints(map, startPoint, toVisit, pathFragment);
                foreach (var m in pathFragment)
                {
                    yield return m;
                }

                Trace.Assert(nextDestinations.Count == 1);
                var dest = nextDestinations.First();
                startPoint = dest;
                toVisit.Remove(dest);
            }
        }

        private HashSet<Point> CollectReachablePoints(Map map, Point startPoint, HashSet<Point>? toVisit, List<Move> pathFragment)
        {
            var queue = new Queue<Point>();
            var visited = new HashSet<Point>();
            var pathTrace = new Dictionary<Point, (Move, Point)>();
            visited.Add(startPoint);
            queue.Enqueue(startPoint);

            var result = new HashSet<Point>();

            while (queue.Count > 0)
            {
                var curPoint = queue.Dequeue();
                for (var idx = 0; idx < Deltas.Length; ++idx)
                {
                    var delta = Deltas[idx];
                    var nextPoint = new Point { X = curPoint.X + delta.X, Y = curPoint.Y + delta.Y };
                    if (nextPoint.X >= 0 && nextPoint.Y >= 0 && !visited.Contains(nextPoint) && map.IsFree(nextPoint.X, nextPoint.Y))
                    {
                        visited.Add(nextPoint);
                        queue.Enqueue(nextPoint);
                        result.Add(nextPoint);
                        pathTrace.Add(nextPoint, (Moves[idx], curPoint));
                        if (toVisit != null && toVisit.Contains(nextPoint))
                        {
                            var pathPoint = nextPoint;
                            pathFragment.Clear();
                            while (pathTrace.ContainsKey(pathPoint))
                            {
                                var tr = pathTrace[pathPoint];
                                pathFragment.Add(tr.Item1);
                                pathPoint = tr.Item2;
                            }

                            pathFragment.Reverse();
                            return new HashSet<Point> { nextPoint };
                        }
                    }
                }
            }

            Trace.Assert(toVisit == null);
            return result;
        }
    }
}