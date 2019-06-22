namespace Icfpc2019.Solution
{
    using System;
    using System.Runtime.CompilerServices;

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
                map.StartX,
                map.StartY,
                dir: 0,
                wrapped: UpdateWrappedCells(map, map.StartX, map.StartY, 0, InitManipConfig, ImHashSet.Empty, 0, 0),
                pickedUpBoosterCoords: ImHashSet.Empty,
                drilledCells: ImHashSet.Empty,
                manipConfig: InitManipConfig,
                manipulatorExtensionCount: 0,
                fastWheelsCount: 0,
                drillsCount: 0,
                teleportsCount: 0,
                cloneCount: 0,
                remainingDrillMoves: 0,
                remainingSpeedBoostedMoves: 0)
        {
        }

        private State(
            Map map,
            int x,
            int y,
            int dir,
            (ImHashSet Cells, int CellsCount, int coordsHash) wrapped,
            ImHashSet pickedUpBoosterCoords,
            ImHashSet drilledCells,
            (int, int)[] manipConfig,
            int manipulatorExtensionCount,
            int fastWheelsCount,
            int drillsCount,
            int teleportsCount,
            int cloneCount,
            int remainingSpeedBoostedMoves,
            int remainingDrillMoves)
        {
            this.map = map;

            this.bots = new[]
            {
                new Bot(x, y, dir, manipConfig, remainingSpeedBoostedMoves, remainingDrillMoves),
            };

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

            var manipulatorExtensionCount = this.manipulatorExtensionCount;

            for (var i = 0; i < this.BotsCount; /*++i*/)
            {
                var command = commands[i];
                switch (command)
                {
                    case null:
                        // we expect that once commands for bot are over than nulls will be fed up indefinitely for it
                        return this;
                    case Move move:
                        var newState = this.DoMove(move);

                        if (newState?.bots[0]?.RemainingSpeedBoostedMoves >= 0)
                        {
                            newState = newState.DoMove(move, timeCost: 0) ?? newState;
                        }

                        return newState;
                    case Turn turn:
                        return this.With(dir: (this.Dir + turn.Ddir) & 3);
                    case UseManipulatorExtension useManip:
                        return this.manipulatorExtensionCount <= 0
                            ? null
                            : this.AttachManip(useManip);
                    case UseFastWheels useFastWheels:
                        return this.fastWheelsCount <= 0
                            ? null
                            : this.With(fastWheelsCount: this.fastWheelsCount - 1, remainingSpeedBoostedMoves: 50);
                    case UseDrill useDrill:
                        return this.drillsCount <= 0
                            ? null
                            : this.With(drillsCount: this.drillsCount - 1, remainingDrillMoves: 30);
                    case Clone clone:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), command.ToString());
                }
            }

            throw new ArgumentException(nameof(commands));
        }

        private static (ImHashSet wrappedCells, int wrappedCellsCount, int coordsHash) UpdateWrappedCells(
            Map map,
            int x,
            int y,
            int dir,
            (int, int)[] manipConfig,
            ImHashSet wrappedCells,
            int wrappedCellsCount,
            int coordsHash)
        {
            foreach (var delta in manipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);

                var manipCoord = (x + dx, y + dy);
                if (map.AreVisible(x, y, x + dx, y + dy) && !wrappedCells.TryFind(manipCoord, out var _))
                {
                    wrappedCells = wrappedCells.AddOrUpdate(manipCoord, true);
                    ++wrappedCellsCount;
                    coordsHash += HashCode.Combine(x + dx, y + dy);
                }
            }

            return (wrappedCells, wrappedCellsCount, coordsHash);
        }

#pragma warning disable SA1011

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private State With(
            int? x = null,
            int? y = null,
            int? dir = null,
            ImHashSet? pickedUpBoosterCoords = null,
            ImHashSet? drilledCells = null,
            (int, int)[]? manipConfig = null,
            int? manipulatorExtensionCount = null,
            int? fastWheelsCount = null,
            int? drillsCount = null,
            int? teleportsCount = null,
            int? cloneCount = null,
            int? remainingSpeedBoostedMoves = null,
            int? remainingDrillMoves = null,
            int timeCost = 1)
        {
            var wrapped = UpdateWrappedCells(
                this.map,
                x ?? this.X,
                y ?? this.Y,
                dir ?? this.Dir,
                manipConfig ?? this.ManipConfig,
                this.wrappedCells,
                this.wrappedCellsCount,
                0 /*this.coordsHash*/);

            return new State(
                this.map,
                x ?? this.X,
                y ?? this.Y,
                dir ?? this.Dir,
                wrapped: wrapped,
                pickedUpBoosterCoords ?? this.pickedUpBoosterCoords,
                drilledCells ?? this.drilledCells,
                manipConfig ?? this.ManipConfig,
                manipulatorExtensionCount ?? this.manipulatorExtensionCount,
                fastWheelsCount ?? this.fastWheelsCount,
                drillsCount ?? this.drillsCount,
                teleportsCount ?? this.teleportsCount,
                cloneCount ?? this.cloneCount,
                remainingSpeedBoostedMoves ?? (this.bots[0].RemainingSpeedBoostedMoves - timeCost),
                remainingDrillMoves ?? (this.bots[0].RemainingDrillMoves - timeCost));
        }

        private State? DoMove(Move move, int timeCost = 1)
        {
            var dx = move.Dx;
            var dy = move.Dy;

            var (newX, newY) = (this.X + dx, this.Y + dy);
            if (!this.map.IsFree(newX, newY) &&
                !this.drilledCells.TryFind((newX, newY), out var _) &&
                !(this.bots[0].RemainingDrillMoves > 0 && this.map[newX, newY] == Map.Cell.Obstacle))
            {
                // impossible move
                return null;
            }

            switch (this.map[newX, newY])
            {
                case Map.Cell.Empty:
                case Map.Cell.SpawnPoint:
                    return this.With(x: newX, y: newY, timeCost: timeCost);
                case Map.Cell.Obstacle:
                    return this.bots[0].RemainingDrillMoves <= 0
                        ? throw new InvalidOperationException()
                        : this.With(x: newX, y: newY, drilledCells: this.drilledCells.AddOrUpdate((newX, newY), true), timeCost: timeCost);
                case Map.Cell.FastWheels:
                    var fwCnt = CountBooster(this.fastWheelsCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: fwCnt.PickedUp, fastWheelsCount: fwCnt.Counter, timeCost: timeCost);
                case Map.Cell.Drill:
                    var drillCnt = CountBooster(this.drillsCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: drillCnt.PickedUp, drillsCount: drillCnt.Counter, timeCost: timeCost);
                case Map.Cell.ManipulatorExtension:
                    var manipCnt = CountBooster(this.manipulatorExtensionCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: manipCnt.PickedUp, manipulatorExtensionCount: manipCnt.Counter, timeCost: timeCost);
                case Map.Cell.Teleport:
                    var teleCnt = CountBooster(this.teleportsCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: teleCnt.PickedUp, teleportsCount: teleCnt.Counter, timeCost: timeCost);
                case Map.Cell.Clone:
                    var cloneCnt = CountBooster(this.cloneCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: cloneCnt.PickedUp, cloneCount: cloneCnt.Counter, timeCost: timeCost);
                case Map.Cell.Edge:
                default:
                    throw new InvalidOperationException($"Unexpected cell: {this.map[newX, newY]}");
            }

            (ImHashSet PickedUp, int Counter) CountBooster(int counter) =>
                this.pickedUpBoosterCoords.TryFind((newX, newY), out var _)
                    ? (this.pickedUpBoosterCoords, counter)
                    : (this.pickedUpBoosterCoords.AddOrUpdate((newX, newY), true), counter + 1);
        }

        private State? AttachManip(UseManipulatorExtension useManip)
        {
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

            return this.With(
                manipulatorExtensionCount: this.manipulatorExtensionCount - 1,
                manipConfig: newManipConfig);
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
        }
    }
}