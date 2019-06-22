namespace Icfpc2019.Solution
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Key doesn't really matter
    using ImHashSet = ImTools.ImHashMap<(int, int), bool>;

    public class State
    {
        private static readonly (int, int)[] InitManipConfig =
        {
            (0, 0),
            (1, -1),
            (1, 0),
            (1, 1),
        };

        private readonly Map map;

        /*
        private readonly int coordsHash;
        private readonly int hash;
        */

        private readonly Bot[] bots;

        private readonly ImHashSet wrappedCells;
        private readonly int wrappedCellsCount;
        private readonly ImHashSet pickedUpBoosterCoords;
        private readonly ImHashSet drilledCells;

        private readonly int manipulatorExtensionCount;
        private readonly int fastWheelsCount;
        private readonly int drillsCount;
        private readonly int teleportsCount;
        private readonly int cloneCount;

        public State(Map map)
            : this(
                map,
                new[]
                {
                    new Bot(
                        x: map.StartX,
                        y: map.StartY,
                        dir: 0,
                        manipConfig: InitManipConfig,
                        remainingSpeedBoostedMoves: 0,
                        remainingDrillMoves: 0),
                },
                wrapped: UpdateWrappedCells(map, map.StartX, map.StartY, 0, InitManipConfig, ImHashSet.Empty, 0),
                pickedUpBoosterCoords: ImHashSet.Empty,
                drilledCells: ImHashSet.Empty,
                manipulatorExtensionCount: 0,
                fastWheelsCount: 0,
                drillsCount: 0,
                teleportsCount: 0,
                cloneCount: 0)
        {
        }

        private State(
            Map map,
            Bot[] bots,
            (ImHashSet Cells, int CellsCount) wrapped,
            ImHashSet pickedUpBoosterCoords,
            ImHashSet drilledCells,
            int manipulatorExtensionCount,
            int fastWheelsCount,
            int drillsCount,
            int teleportsCount,
            int cloneCount)
        {
            this.map = map;
            this.bots = bots;

            /*
            this.coordsHash = wrapped.coordsHash;
            this.hash = HashCode.Combine(
                x,
                y,
                dir,
                wrapped.coordsHash,
                manipConfig.GetHashCode(),
                fastWheelsCount + drillsCount + teleportsCount + cloneCount);
            */

            this.wrappedCells = wrapped.Cells;
            this.wrappedCellsCount = wrapped.CellsCount;
            this.pickedUpBoosterCoords = pickedUpBoosterCoords;
            this.drilledCells = drilledCells;
            this.manipulatorExtensionCount = manipulatorExtensionCount;
            this.fastWheelsCount = fastWheelsCount;
            this.drillsCount = drillsCount;
            this.teleportsCount = teleportsCount;
            this.cloneCount = cloneCount;

            // Debug.Assert(this.wrappedCellsCount == this.wrappedCells.Enumerate().Count(), "Counts do not match!");
        }

        public Map Map => this.map;
        public int BotsCount => this.bots.Length;
        public int X => this.bots[0].X;
        public int Y => this.bots[0].Y;
        public int Dir => this.bots[0].Dir;
        public (int, int)[] ManipConfig => this.bots[0].ManipConfig;
        public int Hash => 0;
        public int ManipulatorExtensionCount => this.manipulatorExtensionCount;
        public int WrappedCellsCount => this.wrappedCellsCount;

        public static (int, int) TurnManip(int dir, (int, int) manipRelativeCoord)
        {
            var (dx, dy) = manipRelativeCoord;
            return dir switch
                {
                0 => (dx, dy),
                1 => (-dy, dx),
                2 => (-dx, -dy),
                3 => (dy, -dx),
                _ => throw new ArgumentOutOfRangeException(nameof(dir)),
                };
        }

        public bool IsWrapped(int x, int y) => this.wrappedCells.TryFind((x, y), out var _);

        public bool IsPickedUp(int x, int y) => this.pickedUpBoosterCoords.TryFind((x, y), out var _);

        public bool UnwrappedVisible(int x, int y, int dir, DistsFromCenter dists) =>
            this.MaxUnwrappedVisibleDistFromCenter(x, y, dir, dists).Item1 != 0;

        public (int numVis, int maxDist) MaxUnwrappedVisibleDistFromCenter(int x, int y, int dir, DistsFromCenter dists)
        {
            var sumVis = 0;
            var max = 0;
            foreach (var delta in this.ManipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);

                var manipCoord = (x + dx, y + dy);
                if (this.map.IsFree(x + dx, y + dy) &&
                    !this.wrappedCells.TryFind(manipCoord, out var _) &&
                    this.map.AreVisible(x, y, x + dx, y + dy))
                {
                    sumVis += dists.GetDist(x + dx, y + dy);
                    if (dists.GetDist(x + dx, y + dy) > max)
                    {
                        max = dists.GetDist(x + dx, y + dy);
                    }
                }
            }

            return (sumVis, max);
        }

        public State? Next(params Command[] commands)
        {
            if (commands.Length != this.BotsCount)
            {
                return null;
            }

            var newBots = new Bot[this.bots.Length];
            var newWrappedCells = this.wrappedCells;
            var newWrappedCellsCount = this.wrappedCellsCount;
            var newPickedUpBoosterCoords = this.pickedUpBoosterCoords;
            var newDrilledCells = this.drilledCells;

            var newManipulatorExtensionCount = this.manipulatorExtensionCount;
            var newFastWheelsCount = this.fastWheelsCount;
            var newDrillsCount = this.drillsCount;
            var newTeleportsCount = this.teleportsCount;
            var newCloneCount = this.cloneCount;

            for (var i = 0; i < this.bots.Length; ++i)
            {
                var bot = this.bots[i];

                bot.PickUpBoosters(
                    this,
                    ref newPickedUpBoosterCoords,
                    ref newManipulatorExtensionCount,
                    ref newFastWheelsCount,
                    ref newDrillsCount,
                    ref newTeleportsCount,
                    ref newCloneCount);

                var command = commands[i];
                var newBot = command switch
                {
                // we expect that once commands for bot are over than nulls will be fed up indefinitely for it
                // but do not really verify that
                null => bot,
                Move move => bot.DoMove(this, move, ref newWrappedCells, ref newWrappedCellsCount),
                Turn turn => bot.DoTurn(this, turn, ref newWrappedCells, ref newWrappedCellsCount),
                UseManipulatorExtension useManip => bot.AttachManip(
                    this,
                    useManip,
                    ref newManipulatorExtensionCount,
                    ref newWrappedCells,
                    ref newWrappedCellsCount),
                UseFastWheels useFastWheels => bot.UseFastWheels(ref newFastWheelsCount),
                UseDrill useDrill => bot.UseDrill(ref newDrillsCount),
                Clone clone => throw new NotImplementedException(),
                _ => throw new ArgumentOutOfRangeException(nameof(command), command.ToString()),
                };

                if (newBot == null)
                {
                    return null;
                }

                newBots[i] = newBot;
            }

            return new State(
                map: this.map,
                bots: newBots,
                wrapped: (newWrappedCells, newWrappedCellsCount),
                pickedUpBoosterCoords: newPickedUpBoosterCoords,
                drilledCells: newDrilledCells,
                manipulatorExtensionCount: newManipulatorExtensionCount,
                fastWheelsCount: newFastWheelsCount,
                drillsCount: newDrillsCount,
                teleportsCount: newTeleportsCount,
                cloneCount: newCloneCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (ImHashSet wrappedCells, int wrappedCellsCount) UpdateWrappedCells(
            Map map,
            int x,
            int y,
            int dir,
            (int, int)[] manipConfig,
            ImHashSet wrappedCells,
            int wrappedCellsCount)
        {
            foreach (var delta in manipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);

                var manipCoord = (x + dx, y + dy);
                if (map.AreVisible(x, y, x + dx, y + dy) && !wrappedCells.TryFind(manipCoord, out var _))
                {
                    wrappedCells = wrappedCells.AddOrUpdate(manipCoord, true);
                    ++wrappedCellsCount;

                    // coordsHash += HashCode.Combine(x + dx, y + dy);
                }
            }

            return (wrappedCells, wrappedCellsCount);
        }

        public class Bot
        {
            public Bot(
                int x,
                int y,
                int dir,
                (int, int)[] manipConfig,
                int remainingSpeedBoostedMoves,
                int remainingDrillMoves)
            {
                this.X = x;
                this.Y = y;
                this.Dir = dir;
                this.ManipConfig = manipConfig;
                this.RemainingSpeedBoostedMoves = remainingSpeedBoostedMoves;
                this.RemainingDrillMoves = remainingDrillMoves;
            }

            public int X { get; }
            public int Y { get; }
            public int Dir { get; }

            public (int, int)[] ManipConfig { get; }
            public int RemainingSpeedBoostedMoves { get; }
            public int RemainingDrillMoves { get; }

#pragma warning disable SA1011

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot With(
                int? x = null,
                int? y = null,
                int? dir = null,
                (int, int)[]? manipConfig = null,
                int? remainingSpeedBoostedMoves = null,
                int? remainingDrillMoves = null,
                int timeCost = 1)
            {
                return new Bot(
                    x: x ?? this.X,
                    y: y ?? this.Y,
                    dir: dir ?? this.Dir,
                    manipConfig: manipConfig ?? this.ManipConfig,
                    remainingSpeedBoostedMoves: (remainingDrillMoves ?? this.RemainingSpeedBoostedMoves) - timeCost,
                    remainingDrillMoves: (remainingDrillMoves ?? this.RemainingDrillMoves) - timeCost);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void PickUpBoosters(
                State state,
                ref ImHashSet newPickedUpBoosterCoords,
                ref int newManipulatorExtensionCount,
                ref int newFastWheelsCount,
                ref int newDrillsCount,
                ref int newTeleportsCount,
                ref int newCloneCount)
            {
                switch (state.Map[this.X, this.Y])
                {
                    case Map.Cell.Empty:
                    case Map.Cell.SpawnPoint:
                        break;
                    case Map.Cell.Obstacle:
                        Debug.Assert(
                            state.drilledCells.TryFind((this.X, this.Y), out var _),
                            "Obstacles must be drilled");
                        break;
                    case Map.Cell.FastWheels:
                        this.CountBooster(ref newPickedUpBoosterCoords, ref newFastWheelsCount);
                        break;
                    case Map.Cell.Drill:
                        this.CountBooster(ref newPickedUpBoosterCoords, ref newDrillsCount);
                        break;
                    case Map.Cell.ManipulatorExtension:
                        this.CountBooster(ref newPickedUpBoosterCoords, ref newManipulatorExtensionCount);
                        break;
                    case Map.Cell.Teleport:
                        this.CountBooster(ref newPickedUpBoosterCoords, ref newTeleportsCount);
                        break;
                    case Map.Cell.Clone:
                        this.CountBooster(ref newPickedUpBoosterCoords, ref newCloneCount);
                        break;
                    case Map.Cell.Edge:
                    default:
                        throw new InvalidOperationException($"Unexpected cell: {state.Map[this.X, this.Y]}");
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? DoMove(
                State state,
                Move move,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount)
            {
                var dx = move.Dx;
                var dy = move.Dy;

                var (newX, newY) = (this.X + dx, this.Y + dy);
                if (!state.Map.IsFree(newX, newY) &&
                    !state.drilledCells.TryFind((newX, newY), out var _) &&
                    !(this.RemainingDrillMoves > 0 && state.Map[newX, newY] == Map.Cell.Obstacle))
                {
                    // impossible move
                    return null;
                }

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    newX,
                    newY,
                    this.Dir,
                    this.ManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount);

                return this.With(x: newX, y: newY);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot DoTurn(
                State state,
                Turn turn,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount)
            {
                var newDir = (this.Dir + turn.Ddir) & 3;

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    this.X,
                    this.Y,
                    newDir,
                    this.ManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount);

                return this.With(dir: newDir);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? AttachManip(
                State state,
                UseManipulatorExtension useManip,
                ref int newManipulatorExtensionsCount,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount)
            {
                if (--newManipulatorExtensionsCount < 0)
                {
                    return null;
                }

                var revDir = (4 - this.Dir) & 3;
                var (dx, dy) = TurnManip(revDir, (useManip.Dx, useManip.Dy));
                var can = false;
                foreach (var oldCoord in this.ManipConfig)
                {
                    var (oldDx, oldDy) = oldCoord;
                    var dist = Math.Abs(dx - oldDx) + Math.Abs(dy - oldDy);
                    if (dist == 0)
                    {
                        return null;
                    }

                    if (dist == 1)
                    {
                        can = true;
                    }
                }

                if (!can)
                {
                    return null;
                }

                var newManipConfig = new (int, int)[this.ManipConfig.Length + 1];
                Array.Copy(this.ManipConfig, newManipConfig, this.ManipConfig.Length);
                newManipConfig[this.ManipConfig.Length] = (dx, dy);

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    this.X,
                    this.Y,
                    this.Dir,
                    newManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount);

                return this.With(manipConfig: newManipConfig);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? UseFastWheels(ref int newFastWheelsCount) =>
                --newFastWheelsCount < 0
                    ? null
                    : this.With(remainingSpeedBoostedMoves: 51);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? UseDrill(ref int newDrillsCount) =>
                --newDrillsCount < 0
                    ? null
                    : this.With(remainingDrillMoves: 31);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void CountBooster(ref ImHashSet newPickedUpBoosterCoords, ref int counter)
            {
                if (!newPickedUpBoosterCoords.TryFind((this.X, this.Y), out var _))
                {
                    ++counter;
                    newPickedUpBoosterCoords = newPickedUpBoosterCoords.AddOrUpdate((this.X, this.Y), true);
                }
            }
        }
    }
}