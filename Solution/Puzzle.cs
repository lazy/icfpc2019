namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;

    public class Puzzle
    {
        private const int RectDim = 5;
        private readonly AllPoints allPoints = new AllPoints();
        private readonly HashSet<Point> insidePoints = new HashSet<Point>();
        private readonly HashSet<Point> outsidePoints = new HashSet<Point>();
        private readonly HashSet<Point> contourPoints = new HashSet<Point>();
        private int bNum;
        private int eNum;
        private int tSize;
        private int vMin;
        private int vMax;
        private int mNum;
        private int fNum;
        private int dNum;
        private int rNum;
        private int cNum;
        private int xNum;

        public Puzzle(string description)
        {
            var tokens = description.Split('#');

            var hyperParams = tokens[0].Split(',');

            this.bNum = int.Parse(hyperParams[0]);
            this.eNum = int.Parse(hyperParams[1]);
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
            var dx = new[] { 0, 1, 0, -1 };
            var dy = new[] { 1, 0, -1, 0 };

            bool GoodXy(int x, int y) => x >= 0 && x <= this.tSize && y >= 0 && y <= this.tSize;

            foreach (var currentPoint in outPts)
            {
                var queue = new Queue<Point>();
                var prev = new Dictionary<Point, Point>();
                var visited = new HashSet<Point>();

                queue.Enqueue(currentPoint);
                visited.Add(currentPoint);

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    if (!GoodXy(cur.X, cur.Y))
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

            var dir = 1;
            var startPoint = new Point { X = 0, Y = 0 };
            this.contourPoints.Add(startPoint);
            var curContour = startPoint;

            while (true)
            {
                var nextX = curContour.X + dx[dir];
                var nextY = curContour.Y + dy[dir];
                var nextContour = new Point { X = nextX, Y = nextY };
                if (GoodXy(nextX, nextY) && !this.outsidePoints.Contains(nextContour))
                {
                    var turnRightDir = (dir + 1) % 4;
                    var rightX = nextContour.X + dx[turnRightDir];
                    var rightY = nextContour.Y + dy[turnRightDir];
                    var rightContour = new Point { X = rightX, Y = rightY };
                    if (GoodXy(rightX, rightY) && !this.outsidePoints.Contains(rightContour))
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

        private static Point CorrectPoint(Point p)
        {
            return new Point { X = p.X - 1, Y = p.Y - 1 };
        }
    }
}