namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    public struct DistsFromCenter
    {
        private const int RectDim = 5;

        private readonly int[,] cellDists;

        public DistsFromCenter(State state)
        {
            // Calculate cell dists
            this.cellDists = new int[state.Map.Width, state.Map.Height];
            var queue = new Queue<(int, int)>();

            var (centerX, centerY) = this.FindGraphCenter(state);

            queue.Enqueue((centerX, centerY));
            this.cellDists[centerX, centerY] = 1;

            var cellDists = this.cellDists;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();

                TryAdd(-1, 0);
                TryAdd(1, 0);
                TryAdd(0, -1);
                TryAdd(0, 1);

                void TryAdd(int dx, int dy)
                {
                    var nx = x + dx;
                    var ny = y + dy;
                    if (state.Map.IsFree(nx, ny) && cellDists[nx, ny] == 0)
                    {
                        cellDists[nx, ny] = cellDists[x, y] + 1;
                        queue.Enqueue((nx, ny));
                    }
                }
            }
        }

        public int GetDist(int x, int y) => this.cellDists[x, y];

        public Bitmap SaveToBitmap()
        {
            var width = this.cellDists.GetLength(0);
            var height = this.cellDists.GetLength(1);

            var bmp = new Bitmap(RectDim * width, RectDim * height);
            using var g = Graphics.FromImage(bmp);

            g.ScaleTransform(1.0f, -1.0f);
            g.TranslateTransform(0.0f, -height * RectDim);

            void DrawRect(Brush b, int x, int y)
            {
                g.FillRectangle(b, x * RectDim, y * RectDim, RectDim, RectDim);
            }

            var maxDist = 0;
            for (var x = 0; x < this.cellDists.GetLength(0); ++x)
            {
                for (var y = 0; y < this.cellDists.GetLength(1); ++y)
                {
                    maxDist = Math.Max(maxDist, this.cellDists[x, y]);
                }
            }

            var distToBrush = new Brush[maxDist + 1];
            distToBrush[0] = Brushes.Bisque;

            for (int i = 1; i <= maxDist; ++i)
            {
                var c = (i * 255) / maxDist;
                distToBrush[i] = new SolidBrush(Color.FromArgb(c, c, c));
            }

            for (var x = 0; x < this.cellDists.GetLength(0); ++x)
            {
                for (var y = 0; y < this.cellDists.GetLength(1); ++y)
                {
                    var brush = distToBrush[this.cellDists[x, y]];
                    DrawRect(brush, x, y);
                }
            }

            return bmp;
        }

        private (int, int) FindGraphCenter(State state)
        {
            var queue = new Queue<(int, int)>();

            (int, int, int[,]) Traverse(int startX, int startY)
            {
                var dists = new int[state.Map.Width, state.Map.Height];
                queue.Enqueue((startX, startY));
                dists[startX, startY] = 1;

                while (queue.Count > 0)
                {
                    var (x, y) = queue.Dequeue();

                    TryAdd(-1, 0);
                    TryAdd(1, 0);
                    TryAdd(0, -1);
                    TryAdd(0, 1);

                    void TryAdd(int dx, int dy)
                    {
                        var nx = x + dx;
                        var ny = y + dy;
                        if (state.Map.IsFree(nx, ny) && dists[nx, ny] == 0)
                        {
                            dists[nx, ny] = dists[x, y] + 1;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }

                var (maxX, maxY, maxDist) = (0, 0, 0);
                for (var x = 0; x < state.Map.Width; ++x)
                {
                    for (var y = 0; y < state.Map.Height; ++y)
                    {
                        if (dists[x, y] > maxDist &&
                            state.Map.IsFree(x, y) &&
                            !state.IsWrapped(x, y))
                        {
                            (maxX, maxY, maxDist) = (x, y, dists[x, y]);
                        }
                    }
                }

                return (maxX, maxY, dists);
            }

            var (furthestX, furthestY, dists1) = Traverse(state.Map.StartX, state.Map.StartY);
            (furthestX, furthestY, dists1) = Traverse(furthestX, furthestY);

            var mid = (1 + dists1[furthestX, furthestY]) / 2;

            for (var x = 0; x < state.Map.Width; ++x)
            {
                for (var y = 0; y < state.Map.Height; ++y)
                {
                    if (dists1[x, y] == mid)
                    {
                        return (x, y);
                    }
                }
            }

            throw new InvalidOperationException("No center found!");
        }
    }
}