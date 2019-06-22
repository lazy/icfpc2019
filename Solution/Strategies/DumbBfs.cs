namespace Icfpc2019.Solution.Strategies
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

        private static readonly Command[] Moves =
        {
            Move.Right,
            Move.Left,
            Move.Up,
            Move.Down,
        };

        public Command[][] Solve(State state) =>
            new[] { this.Solve1(state.Map).ToArray() };

        public IEnumerable<Command> Solve1(Map map)
        {
            int negativeExtension = 1;
            int positiveExtension = 1;
            int drillTime = 0;
            int speedTime = 0;

            var startPoint = new Point { X = map.StartX, Y = map.StartY };
            var toVisit = map.CellsToVisit.Select(x => new Point { X = x.Item1, Y = x.Item2 }).ToHashSet();

            while (toVisit.Count > 0)
            {
                if (map.IsFree(startPoint.X, startPoint.Y))
                {
                    if (map[startPoint.X, startPoint.Y] == Map.Cell.ManipulatorExtension)
                    {
                        if (positiveExtension == negativeExtension)
                        {
                            yield return new UseManipulatorExtension(1, positiveExtension + 1);
                            ++positiveExtension;
                        }
                        else
                        {
                            yield return new UseManipulatorExtension(1, -(negativeExtension + 1));
                            ++negativeExtension;
                        }
                    }

                    if (map[startPoint.X, startPoint.Y] == Map.Cell.Drill)
                    {
                        yield return UseDrill.Instance;
                        drillTime = 30;
                    }
                    else if (map[startPoint.X, startPoint.Y] == Map.Cell.FastWheels)
                    {
                        yield return UseFastWheels.Instance;
                        speedTime = 50;
                    }
                }

                var result = this.RunBfs(map, startPoint, toVisit, positiveExtension, negativeExtension, drillTime, speedTime);
                if (result == null)
                {
                    throw new Exception("The BFS should have returned a result");
                }

                foreach (var m in result.PathFragment)
                {
                    yield return m;
                    --drillTime;
                    --speedTime;
                }

                startPoint = result.FirstReachedPoint;
                foreach (var t in result.TouchedPoints)
                {
                    toVisit.Remove(t);
                }
            }
        }

        private BfsResult? RunBfs(Map map, Point startPoint, HashSet<Point> toVisit, int positiveExtension, int negativeExtension, int drillTime, int speedTime)
        {
            var queue = new Queue<Point>();
            var visited = new HashSet<Point>();
            var pathTrace = new Dictionary<Point, (Command, Point)>();
            var dist = new Dictionary<Point, int>();
            visited.Add(startPoint);
            queue.Enqueue(startPoint);
            dist.Add(startPoint, 0);

            while (queue.Count > 0)
            {
                var curPoint = queue.Dequeue();
                var curDist = dist[curPoint];
                var drillActive = curDist < drillTime;
                var speedActive = curDist < speedTime;

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

                        for (var tdy = -negativeExtension; tdy <= positiveExtension; ++tdy)
                        {
                            var touchCandidate = new Point { X = point.X + 1, Y = point.Y + tdy };
                            if (map.AreVisible(point.X, point.Y, touchCandidate.X, touchCandidate.Y))
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
                    var mul = speedActive ? 2 : 1;
                    var intermediatePoint = new Point { X = curPoint.X + delta.X, Y = curPoint.Y + delta.Y };
                    var nextPoint = new Point { X = curPoint.X + (delta.X * mul), Y = curPoint.Y + (delta.Y * mul) };

                    bool CanGoThrough(int x, int y)
                    {
                        if (map.IsFree(x, y))
                        {
                            return true;
                        }

                        return drillActive && map.InBounds(x, y) && map[x, y] == Map.Cell.Obstacle;
                    }

                    if (!visited.Contains(nextPoint) && CanGoThrough(intermediatePoint.X, intermediatePoint.Y) && CanGoThrough(nextPoint.X, nextPoint.Y))
                    {
                        visited.Add(nextPoint);
                        queue.Enqueue(nextPoint);
                        dist.Add(nextPoint, curDist + 1);
                        pathTrace.Add(nextPoint, (Moves[idx], curPoint));
                    }
                }
            }

            return null;
        }

        private class BfsResult
        {
            public HashSet<Point> TouchedPoints { get; } = new HashSet<Point>();
            public List<Command> PathFragment { get; } = new List<Command>();
            public Point FirstReachedPoint { get; set; }
        }
    }
}