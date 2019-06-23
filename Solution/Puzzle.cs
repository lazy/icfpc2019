namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Text;

    public class Puzzle
    {
        private const int RectDim = 5;

        private static int[] dx = { 0, 1, 0, -1 };
        private static int[] dy = { 1, 0, -1, 0 };
        private readonly AllPoints allPoints = new AllPoints();
        private readonly HashSet<Point> contourPoints = new HashSet<Point>();
        private readonly HashSet<Point> insidePoints = new HashSet<Point>();
        private readonly HashSet<Point> outsidePoints = new HashSet<Point>();
        private readonly Random rng = new Random(31337);
        private int cNum;
        private int dNum;
        private int fNum;
        private int mNum;
        private int rNum;
        private int tSize;
        private int vMax;
        private int vMin;
        private int xNum;

        public Puzzle(string description)
        {
            var tokens = description.Split('#');

            var hyperParams = tokens[0].Split(',');

            this.tSize = int.Parse(hyperParams[2]);
            this.vMin = int.Parse(hyperParams[3]);
            this.vMax = int.Parse(hyperParams[4]);
            this.mNum = int.Parse(hyperParams[5]);
            this.fNum = int.Parse(hyperParams[6]);
            this.dNum = int.Parse(hyperParams[7]);
            this.rNum = int.Parse(hyperParams[8]);
            this.cNum = int.Parse(hyperParams[9]);
            this.xNum = int.Parse(hyperParams[10]);

            this.insidePoints.UnionWith(tokens[1].Split("),").Select(Point.Parse).Select(CorrectPoint));
            this.outsidePoints.UnionWith(tokens[2].Split("),").Select(Point.Parse).Select(CorrectPoint));

            foreach (var pts in new[] { this.insidePoints, this.outsidePoints })
            {
                foreach (var p in pts)
                {
                    this.allPoints.Update(p);
                }
            }

            var outPts = new List<Point>(this.outsidePoints);

            foreach (var currentPoint in outPts)
            {
                this.ConnectPointWithBoundary(currentPoint);
            }

            this.CollectContourPoints();

            if (this.contourPoints.Count <= this.vMin)
            {
                for (var iter = 0; iter < this.vMin - this.contourPoints.Count; ++iter)
                {
                    this.ConnectPointWithBoundary(this.SelectRandomPoint(null, this.tSize, this.tSize));
                }
            }

            this.CollectContourPoints();
        }

        public Bitmap SaveToBitmap()
        {
            var totalWidth = Math.Max(this.allPoints.MaxX, this.tSize) + 1;
            var totalHeight = Math.Max(this.allPoints.MaxY, this.tSize) + 1;
            var bmp = new Bitmap(RectDim * totalWidth, RectDim * totalHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.ScaleTransform(1.0f, -1.0f);
                g.TranslateTransform(0.0f, -totalHeight * RectDim);

                void DrawRect(Brush b, int x, int y)
                {
                    g.FillRectangle(b, x * RectDim, y * RectDim, RectDim, RectDim);
                }

                for (var x = 0; x < totalWidth; ++x)
                {
                    for (var y = 0; y < totalHeight; ++y)
                    {
                        DrawRect(Brushes.Bisque, x, y);
                    }
                }

                foreach (var p in this.insidePoints)
                {
                    DrawRect(Brushes.White, p.X, p.Y);
                }

                foreach (var p in this.outsidePoints)
                {
                    DrawRect(Brushes.Black, p.X, p.Y);
                }

                foreach (var p in this.contourPoints)
                {
                    const int pDelta = 1;
                    g.FillEllipse(Brushes.Red, (p.X * RectDim) - pDelta, (p.Y * RectDim) - pDelta, 2 * pDelta, 2 * pDelta);
                }
            }

            return bmp;
        }

        public string SaveToMap()
        {
            string FmtPoint(Point p) => $"({p.X},{p.Y})";
            var contourPointsStr = string.Join(',', this.contourPoints.Select(FmtPoint));

            var selectedPoints = new HashSet<Point>();
            var contourAllPoints = new AllPoints();
            foreach (var p in this.contourPoints)
            {
                contourAllPoints.Update(p);
            }

            var startPos = this.SelectRandomPoint(selectedPoints, contourAllPoints.MaxX, contourAllPoints.MaxY);

            var boosters = new StringBuilder();

            void SelectBoosters(char sym, int count)
            {
                for (var i = 0; i < count; ++i)
                {
                    if (boosters.Length > 0)
                    {
                        boosters.Append(';');
                    }

                    var boosterPoint = this.SelectRandomPoint(selectedPoints, contourAllPoints.MaxX, contourAllPoints.MaxY);
                    boosters.Append($"{sym}{FmtPoint(boosterPoint)}");
                }
            }

            SelectBoosters('B', this.mNum);
            SelectBoosters('F', this.fNum);
            SelectBoosters('L', this.dNum);
            SelectBoosters('R', this.rNum);
            SelectBoosters('C', this.cNum);
            SelectBoosters('X', this.xNum);

            return $"{contourPointsStr}#{FmtPoint(startPos)}##{boosters}";
        }

        private static Point CorrectPoint(Point p)
        {
            return new Point { X = p.X - 1, Y = p.Y - 1 };
        }

        private Point SelectRandomPoint(HashSet<Point>? alreadySelected, int maxX, int maxY)
        {
            while (true)
            {
                var rndPoint = new Point
                {
                    X = this.rng.Next(maxY),
                    Y = this.rng.Next(maxY),
                };
                if (!this.outsidePoints.Contains(rndPoint) && !this.insidePoints.Contains(rndPoint))
                {
                    if (alreadySelected != null)
                    {
                        if (!alreadySelected.Contains(rndPoint))
                        {
                            alreadySelected.Add(rndPoint);
                        }
                    }

                    return rndPoint;
                }
            }
        }

        private void CollectContourPoints()
        {
            this.contourPoints.Clear();
            var dir = 1;
            var startPoint = new Point { X = 0, Y = 0 };
            this.contourPoints.Add(startPoint);
            var curContour = startPoint;

            while (true)
            {
                var nextX = curContour.X + dx[dir];
                var nextY = curContour.Y + dy[dir];
                var nextContour = new Point { X = nextX, Y = nextY };
                if (this.GoodXy(nextX, nextY) && !this.outsidePoints.Contains(nextContour))
                {
                    var turnRightDir = (dir + 1) % 4;
                    var rightX = nextContour.X + dx[turnRightDir];
                    var rightY = nextContour.Y + dy[turnRightDir];
                    var rightContour = new Point { X = rightX, Y = rightY };
                    if (this.GoodXy(rightX, rightY) && !this.outsidePoints.Contains(rightContour))
                    {
                        AddContourPointForDir(dir, curContour.X, curContour.Y);
                        curContour = rightContour;
                        dir = turnRightDir;
                    }
                    else
                    {
                        curContour = nextContour;
                    }
                }
                else
                {
                    AddContourPointForDir(dir, curContour.X, curContour.Y);
                    dir = (dir + 3) % 4;
                }

                if (curContour == startPoint)
                {
                    break;
                }
            }

            void AddContourPointForDir(int d, int fromX, int fromY)
            {
                int x;
                int y;
                switch (d)
                {
                    case 0:
                        x = 1;
                        y = 1;
                        break;
                    case 1:
                        x = 1;
                        y = 0;
                        break;
                    case 2:
                        x = 0;
                        y = 0;
                        break;
                    case 3:
                        x = 0;
                        y = 1;
                        break;
                    default:
                        throw new Exception($"Unexpected direction {d}");
                }

                this.contourPoints.Add(new Point { X = fromX + x, Y = fromY + y });
            }

            Trace.Assert(this.contourPoints.Count <= this.vMax);
        }

        private void ConnectPointWithBoundary(Point currentPoint)
        {
            var queue = new Queue<Point>();
            var prev = new Dictionary<Point, Point>();
            var visited = new HashSet<Point>();

            queue.Enqueue(currentPoint);
            visited.Add(currentPoint);

            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (!this.GoodXy(cur.X, cur.Y))
                {
                    while (prev.ContainsKey(cur))
                    {
                        this.outsidePoints.Add(cur);
                        cur = prev[cur];
                    }

                    break;
                }

                for (var idx = 0; idx < 4; ++idx)
                {
                    var nextPoint = new Point { X = cur.X + dx[idx], Y = cur.Y + dy[idx] };
                    if (!visited.Contains(nextPoint) && !this.insidePoints.Contains(nextPoint))
                    {
                        queue.Enqueue(nextPoint);
                        prev[nextPoint] = cur;
                        visited.Add(nextPoint);
                    }
                }
            }
        }

        private bool GoodXy(int x, int y) => x >= 0 && x < this.tSize && y >= 0 && y < this.tSize;
    }
}