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

            this.insidePoints.UnionWith(tokens[1].Split("),").Select(Point.Parse));
            this.outsidePoints.UnionWith(tokens[2].Split("),").Select(Point.Parse));

            foreach (var pts in new[] { this.insidePoints, this.outsidePoints })
            {
                foreach (var p in pts)
                {
                    this.allPoints.Update(p);
                }
            }

            var rng = new Random(31337);
            var outPts = new List<Point>(this.outsidePoints.OrderBy(x => rng.Next()));

            foreach (var currentPoint in outPts)
            {
                var toLeft = currentPoint.X;
                var toRight = this.tSize - 1 - currentPoint.X;
                var toDown = currentPoint.Y;
                var toUp = this.tSize - 1 - currentPoint.Y;

                var tracePoint = currentPoint;
                if (Math.Min(toLeft, toRight) < Math.Min(toDown, toUp))
                {
                    if (toLeft < toRight)
                    {
                        while (tracePoint.X > 0)
                        {
                            --tracePoint.X;
                            this.outsidePoints.Add(tracePoint);
                        }
                    }
                    else
                    {
                        while (tracePoint.X < this.tSize)
                        {
                            ++tracePoint.X;
                            this.outsidePoints.Add(tracePoint);
                        }
                    }
                }
                else
                {
                    if (toDown < toUp)
                    {
                        while (tracePoint.Y > 0)
                        {
                            --tracePoint.Y;
                            this.outsidePoints.Add(tracePoint);
                        }
                    }
                    else
                    {
                        while (tracePoint.Y < this.tSize)
                        {
                            ++tracePoint.Y;
                            this.outsidePoints.Add(tracePoint);
                        }
                    }
                }
            }

            var vCount = (this.tSize * this.tSize) - this.outsidePoints.Count;

            Console.WriteLine($"vCount = {vCount}, vMax = {this.vMax}, vMin = {this.vMin}");
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
                        DrawRect(Brushes.White, x, y);
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
            }

            return bmp;
        }
    }
}