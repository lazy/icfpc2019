namespace Icfpc2019.Solution
{
    public abstract class Command
    {
    }

    public class Move : Command
    {
        private Move(int dx, int dy, string repr)
        {
            this.Dx = dx;
            this.Dy = dy;
            this.Repr = repr;
        }

        public static Move Up { get; } = new Move(0, 1, "W");
        public static Move Down { get; } = new Move(0, -1, "S");
        public static Move Left { get; } = new Move(-1, 0, "A");
        public static Move Right { get; } = new Move(1, 0, "D");

        public static Move[] All { get; } = new[] { Left, Up, Right, Down };

        public int Dx { get; }
        public int Dy { get; }
        public string Repr { get; }

        public override string ToString() => this.Repr;
    }

    public class Turn : Command
    {
        private Turn(int ddir, string repr)
        {
            this.Ddir = ddir;
            this.Repr = repr;
        }

        public static Turn Left { get; } = new Turn(1, "Q");
        public static Turn Right { get; } = new Turn(3, "E");

        public static Turn[] All { get; } = new[] { Left, Right };

        public int Ddir { get; }
        public string Repr { get; }

        public override string ToString() => this.Repr;
    }

    public class UseManipulatorExtension : Command
    {
        public UseManipulatorExtension(int dx, int dy)
        {
            this.Dx = dx;
            this.Dy = dy;
        }

        public int Dx { get; }
        public int Dy { get; }

        public override bool Equals(object obj) =>
            obj is UseManipulatorExtension that && that.Dx == this.Dx && that.Dy == this.Dy;

        public override int GetHashCode() => (this.Dx, this.Dy).GetHashCode();

        public override string ToString() => $"B({this.Dx},{this.Dy})";
    }

    public class UseFastWheels : Command
    {
        private UseFastWheels()
        {
        }

        public static UseFastWheels Instance { get; } = new UseFastWheels();

        public override string ToString() => "F";
    }

    public class UseDrill : Command
    {
        private UseDrill()
        {
        }

        public static UseDrill Instance { get; } = new UseDrill();

        public override string ToString() => "L";
    }

    public class Clone : Command
    {
        private Clone()
        {
        }

        public static Clone Instance { get; } = new Clone();

        public override string ToString() => "C";
    }
}