namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class Map
    {
        private readonly Cell[,] cells;
        private readonly HashSet<(int, int)> cellsToVisit;

        public Map(int startX, int startY, Cell[,] cells)
        {
            this.StartX = startX;
            this.StartY = startY;
            this.cells = cells;
            this.cellsToVisit = new HashSet<(int, int)>();
            this.FindCellsToVisit();

            // Mark all inaccessible free cells as edge
            for (var x = 0; x < this.Width; ++x)
            {
                for (var y = 0; y < this.Height; ++y)
                {
                    if (this.IsFree(x, y) && !this.cellsToVisit.Contains((x, y)))
                    {
                        this.cells[x, y] = Cell.Edge;
                    }
                }
            }
        }

        public enum Cell
        {
            Empty,
            Obstacle,
            Edge,
            ManipulatorExtension,
            FastWheels,
            Drill,
            MysteriousPoint,
            Teleport,
            Clone,
        }

        public IReadOnlyCollection<(int, int)> CellsToVisit => this.cellsToVisit;
        public int Height => this.cells.GetLength(1);

        public int StartX { get; }

        public int StartY { get; }

        public int Width => this.cells.GetLength(0);

        public Cell this[int x, int y] => this.cells[x, y];

        public static Map FromAscii(params string[] lines)
        {
            // matrix is transposed, because our coords are (x, y) but
            // visual coords are (y, x)
            var cells = new Cell[lines[0].Length, lines.Length];

            (int, int)? startPosition = null;

            // Turn map upside down and transpose so we can use convenient coords
            for (var reverseY = 0; reverseY < lines.Length; ++reverseY)
            {
                var y = lines.Length - reverseY - 1;
                var line = lines[reverseY];
                if (line.Length != lines[0].Length)
                {
                    throw new ArgumentException("All lines must be of equal length");
                }

                for (var x = 0; x < line.Length; ++x)
                {
                    if (line[x] == 'v')
                    {
                        if (startPosition != null)
                        {
                            throw new ArgumentException("More than one start position found");
                        }

                        startPosition = (x, y);
                    }
                    else
                    {
                        cells[x, y] = AsciiToCell(line[x]);
                    }
                }
            }

            var (startX, startY) = startPosition ?? throw new ArgumentException("Initial position was not found");

            return new Map(startX, startY, cells);
        }

        public bool InBounds(int x, int y)
        {
            return x >= 0 && y >= 0 && x < this.Width && y < this.Height;
        }

        public bool IsFree(int x, int y)
        {
            if (!this.InBounds(x, y))
            {
                return false;
            }

            var cell = this[x, y];
            return cell != Cell.Edge && cell != Cell.Obstacle;
        }

        public bool AreVisible(int x1, int y1, int x2, int y2)
        {
            if (!this.IsFree(x1, y1) || !this.IsFree(x2, y2))
            {
                return false;
            }

            var (dx, dy) = (x2 - x1, y2 - y1);
            var (dxSign, dySign) = (Math.Sign(dx), Math.Sign(dy));
            var (absDx, absDy) = (Math.Abs(dx), Math.Abs(dy));

            if (absDx <= 1 && absDy <= 1)
            {
                return true;
            }

            var xIsMinor = absDx == 1;
            var yIsMinor = absDy == 1;

            if (!xIsMinor && !yIsMinor)
            {
                return false;
            }

            var majorTotalDelta = xIsMinor ? absDy : absDx;

            bool CheckOneCell(int minorDelta, int majorDelta)
            {
                if (xIsMinor)
                {
                    var x = x1 + (minorDelta * dxSign);
                    var y = y1 + (majorDelta * dySign);
                    return this.IsFree(x, y);
                }
                else
                {
                    var x = x1 + (majorDelta * dxSign);
                    var y = y1 + (minorDelta * dySign);
                    return this.IsFree(x, y);
                }
            }

            switch (majorTotalDelta)
            {
                case 2:
                    return CheckOneCell(0, 1) && CheckOneCell(1, 1);
                case 3:
                    return CheckOneCell(0, 1) && CheckOneCell(1, 2);
                case 4:
                    return CheckOneCell(0, 1) && CheckOneCell(0, 2) && CheckOneCell(1, 2) && CheckOneCell(1, 3);
                default:
                    return false;
            }
        }

        public override string ToString()
        {
            var rows = new List<string>();

            for (var y = this.Height - 1; y >= 0; --y)
            {
                var currentRow = new StringBuilder();
                for (var x = 0; x < this.Width; ++x)
                {
                    var c = (x, y) == (this.StartX, this.StartY)
                        ? 'v'
                        : CellToAscii(this[x, y]);
                    currentRow.Append(c);
                }

                rows.Add(currentRow.ToString());
            }

            return string.Join("\n", rows);
        }

        private static Cell AsciiToCell(char c) =>
            c switch
                {
                '.' => Cell.Empty,
                '#' => Cell.Obstacle,
                'x' => Cell.Edge,
                'B' => Cell.ManipulatorExtension,
                'F' => Cell.FastWheels,
                'L' => Cell.Drill,
                'X' => Cell.MysteriousPoint,
                'C' => Cell.Clone,
                _ => throw new ArgumentOutOfRangeException($"Invalid cell ascii representation: {c}"),
                };

        private static char CellToAscii(Cell cell) =>
            cell switch
                {
                Cell.Empty => '.',
                Cell.Obstacle => '#',
                Cell.Edge => 'x',
                Cell.ManipulatorExtension => 'B',
                Cell.FastWheels => 'F',
                Cell.Drill => 'L',
                Cell.MysteriousPoint => 'X',
                Cell.Clone => 'C',
                _ => throw new Exception($"Invalid enum value: {cell}"),
                };

        private void FindCellsToVisit()
        {
            var queue = new Queue<(int, int)>();
            queue.Enqueue((this.StartX, this.StartY));

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                var (x, y) = point;
                if (!this.cellsToVisit.Contains(point))
                {
                    this.cellsToVisit.Add(point);
                    Add(-1, 0);
                    Add(1, 0);
                    Add(0, -1);
                    Add(0, 1);

                    void Add(int dx, int dy)
                    {
                        int x1 = x + dx;
                        int y1 = y + dy;
                        if (this.IsFree(x1, y1))
                        {
                            queue.Enqueue((x1, y1));
                        }
                    }
                }
            }
        }
    }
}