namespace Icfpc2019.Solution
{
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
    }
}
