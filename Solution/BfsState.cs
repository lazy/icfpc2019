namespace Icfpc2019.Solution
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;

    public struct BfsState
    {
        public BfsState(Map map)
        {
            this.Generation = 1;
            this.Nodes = new Node[map.Width, map.Height, 4];
            this.Queue = new Queue<(int, int, int)>();
            this.Path = new List<Command>();
        }

        public int Generation { get; set; }
        public Node[,,] Nodes { get; }
        public Queue<(int, int, int)> Queue { get; }
        public List<Command> Path { get; }

        public void FindBackwardPath(int x, int y, int dir, State.Bot bot, Move[] moves)
        {
            this.Path.Clear();
            while ((x, y, dir) != (bot.X, bot.Y, bot.Dir))
            {
                if (this.Nodes[x, y, dir].Generation != this.Generation)
                {
                    throw new Exception("ooops");
                }

                Debug.Assert(this.Nodes[x, y, dir].Generation == this.Generation, "oops");

                var moveIdx = this.Nodes[x, y, dir].MoveIdx;
                if (moveIdx >= 0)
                {
                    var move = moves[moveIdx];
                    this.Path.Add(move);
                    x -= move.Dx;
                    y -= move.Dy;
                }
                else
                {
                    var ddir = -moveIdx;
                    dir = (4 + dir - ddir) & 3;
                    this.Path.Add(ddir == 1 ? Turn.Left : Turn.Right);
                }
            }

            this.Path.Reverse();
        }

        public void FindBackwardPath(int x, int y, int dir, State.Bot bot) =>
            this.FindBackwardPath(x, y, dir, bot, Move.All);

        public struct Node
        {
            public Node(int generation, int moveIdx, int depth)
            {
                this.Generation = generation;
                this.MoveIdx = moveIdx;
                this.Depth = depth;
            }

            public int Generation { get; }
            public int MoveIdx { get; }
            public int Depth { get; }
        }
    }
}
