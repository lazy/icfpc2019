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
        private readonly int x;
        private readonly int y;
        private readonly int dir;
        private readonly ImHashSet wrappedCells;
        private readonly int wrappedCellsCount;
        private readonly ImHashSet pickedUpBoosterCoords;
        private readonly ImHashSet drilledCells;
        private readonly (int, int)[] manipConfig;

        private readonly int manipulatorExtensionCount;
        private readonly int fastWheelsCount;
        private readonly int drillsCount;
        private readonly int mysteriousPointsCount;
        private readonly int teleportsCount;
        private readonly int cloneCount;

        private readonly int remainingSpeedBoostedMoves;
        private readonly int remainingDrillMoves;

        public State(Map map)
            : this(
                map,
                map.StartX,
                map.StartY,
                dir: 0,
                wrapped: UpdateWrappedCells(map, map.StartX, map.StartY, 0, InitManipConfig, ImHashSet.Empty, 0),
                pickedUpBoosterCoords: ImHashSet.Empty,
                drilledCells: ImHashSet.Empty,
                manipConfig: InitManipConfig,
                manipulatorExtensionCount: 0,
                fastWheelsCount: 0,
                drillsCount: 0,
                mysteriousPointsCount: 0,
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
            (ImHashSet Cells, int CellsCount) wrapped,
            ImHashSet pickedUpBoosterCoords,
            ImHashSet drilledCells,
            (int, int)[] manipConfig,
            int manipulatorExtensionCount,
            int fastWheelsCount,
            int drillsCount,
            int mysteriousPointsCount,
            int teleportsCount,
            int cloneCount,
            int remainingSpeedBoostedMoves,
            int remainingDrillMoves)
        {
            this.map = map;
            this.x = x;
            this.y = y;
            this.dir = dir;
            this.wrappedCells = wrapped.Cells;
            this.wrappedCellsCount = wrapped.CellsCount;
            this.pickedUpBoosterCoords = pickedUpBoosterCoords;
            this.drilledCells = drilledCells;
            this.manipConfig = manipConfig;
            this.manipulatorExtensionCount = manipulatorExtensionCount;
            this.fastWheelsCount = fastWheelsCount;
            this.drillsCount = drillsCount;
            this.mysteriousPointsCount = mysteriousPointsCount;
            this.teleportsCount = teleportsCount;
            this.cloneCount = cloneCount;
            this.remainingSpeedBoostedMoves = remainingSpeedBoostedMoves;
            this.remainingDrillMoves = remainingDrillMoves;

            // Debug.Assert(this.wrappedCellsCount == this.wrappedCells.Enumerate().Count(), "Counts do not match!");
        }

        public int X => this.x;
        public int Y => this.y;
        public int Dir => this.dir;
        public int WrappedCellsCount => this.wrappedCellsCount;

        public bool IsWrapped(int x, int y) => this.wrappedCells.TryFind((x, y), out var _);

        public bool UnwrappedVisible(int x, int y, int dir)
        {
            foreach (var delta in this.manipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);

                // TODO: check visibility
                var manipCoord = (x + dx, y + dy);
                if (this.map.IsFree(x + dx, y + dy) && !this.wrappedCells.TryFind(manipCoord, out var _))
                {
                    return true;
                }
            }

            return false;
        }

        public State? Next(Command command)
        {
            switch (command)
            {
                case Move move:
                    var newState = this.DoMove(move);

                    if (newState?.remainingSpeedBoostedMoves >= 0)
                    {
                        newState = newState.DoMove(move, timeCost: 0) ?? newState;
                    }

                    return newState;
                case Turn turn:
                    return this.With(dir: (this.dir + turn.Ddir) & 3);
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

                // TODO: check visibility
                var manipCoord = (x + dx, y + dy);
                if (map.IsFree(x + dx, y + dy) && !wrappedCells.TryFind(manipCoord, out var _))
                {
                    wrappedCells = wrappedCells.AddOrUpdate(manipCoord, true);
                    ++wrappedCellsCount;
                }
            }

            return (wrappedCells, wrappedCellsCount);
        }

        private static (int, int) TurnManip(int dir, (int, int) manipRelativeCoord)
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
            int? mysteriousPointsCount = null,
            int? teleportsCount = null,
            int? cloneCount = null,
            int? remainingSpeedBoostedMoves = null,
            int? remainingDrillMoves = null,
            int timeCost = 1)
        {
            var wrapped = UpdateWrappedCells(
                this.map,
                x ?? this.x,
                y ?? this.y,
                dir ?? this.dir,
                manipConfig ?? this.manipConfig,
                this.wrappedCells,
                this.wrappedCellsCount);

            return new State(
                this.map,
                x ?? this.x,
                y ?? this.y,
                dir ?? this.dir,
                wrapped: wrapped,
                pickedUpBoosterCoords ?? this.pickedUpBoosterCoords,
                drilledCells ?? this.drilledCells,
                manipConfig ?? this.manipConfig,
                manipulatorExtensionCount ?? this.manipulatorExtensionCount,
                fastWheelsCount ?? this.fastWheelsCount,
                drillsCount ?? this.drillsCount,
                mysteriousPointsCount ?? this.mysteriousPointsCount,
                teleportsCount ?? this.teleportsCount,
                cloneCount ?? this.cloneCount,
                remainingSpeedBoostedMoves ?? (this.remainingSpeedBoostedMoves - timeCost),
                remainingDrillMoves ?? (this.remainingDrillMoves - timeCost));
        }

        private State? DoMove(Move move, int timeCost = 1)
        {
            var dx = move.Dx;
            var dy = move.Dy;

            var (newX, newY) = (this.x + dx, this.y + dy);
            if (!this.map.IsFree(newX, newY) &&
                !this.drilledCells.TryFind((newX, newY), out var _) &&
                !(this.remainingDrillMoves > 0 && this.map[newX, newY] == Map.Cell.Obstacle))
            {
                // impossible move
                return null;
            }

            switch (this.map[newX, newY])
            {
                case Map.Cell.Empty:
                    return this.With(x: newX, y: newY, timeCost: timeCost);
                case Map.Cell.Obstacle:
                    return this.remainingDrillMoves <= 0
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
                case Map.Cell.MysteriousPoint:
                    var mystCnt = CountBooster(this.mysteriousPointsCount);
                    return this.With(x: newX, y: newY, pickedUpBoosterCoords: mystCnt.PickedUp, mysteriousPointsCount: mystCnt.Counter, timeCost: timeCost);
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
            var revDir = (4 - this.dir) & 3;
            var (dx, dy) = TurnManip(revDir, (useManip.Dx, useManip.Dy));
            var can = false;
            foreach (var oldCoord in this.manipConfig)
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

            var newManipConfig = new (int, int)[this.manipConfig.Length + 1];
            Array.Copy(this.manipConfig, newManipConfig, this.manipConfig.Length);
            newManipConfig[this.manipConfig.Length] = (dx, dy);

            return this.With(
                manipulatorExtensionCount: this.manipulatorExtensionCount - 1,
                manipConfig: newManipConfig);
        }
    }
}