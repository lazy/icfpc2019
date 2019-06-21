namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public struct Map
    {
        private readonly Cell[,] cells;

        public Map(int startX, int startY, Cell[,] cells)
        {
            this.StartX = startX;
            this.StartY = startY;
            this.cells = cells;
        }

        public enum Cell
        {
            Empty,
            Obstacle,
            Edge,
            BoosterB,
            BoosterF,
            BoosterD,
            BoosterX,
        }

        public int Width => this.cells.GetLength(0);
        public int Height => this.cells.GetLength(1);

        public int StartX { get; }

        public int StartY { get; }

        public Cell this[int i, int j] => this.cells[i, j];

        public override string ToString()
        {
            var rows = new List<string>();

            for (var y = this.Height - 1; y >= 0; --y)
            {
                var currentRow = new StringBuilder();
                for (var x = 0; x < this.Width; ++x)
                {
                    currentRow.Append(this.CoordsToChar(x, y));
                }

                rows.Add(currentRow.ToString());
            }

            return string.Join("\n", rows);
        }

        private char CoordsToChar(int x, int y)
        {
            if (x == this.StartX && y == this.StartY)
            {
                return 'v';
            }

            var cell = this[x, y];
            return cell switch
                {
                Cell.Empty => '.',
                Cell.Obstacle => '#',
                Cell.Edge => 'x',
                Cell.BoosterB => 'B',
                Cell.BoosterF => 'F',
                Cell.BoosterD => 'D',
                Cell.BoosterX => 'X',
                _ => throw new Exception(string.Format("Invalid enum value: {}", cell)),
                };
        }
    }
}