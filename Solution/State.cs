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
            this.remainingSpeedBoostedMoves = remainingSpeedBoostedMoves;
            this.remainingDrillMoves = remainingDrillMoves;
        }

        public int WrappedCellsCount => this.wrappedCellsCount;

        public State? Next(Move move)
        {
            switch (move)
            {
                case Move.MoveUp:
                case Move.MoveDown:
                case Move.MoveLeft:
                case Move.MoveRight:
                    var newState = this.DoMove(move);

                    if (newState?.remainingSpeedBoostedMoves >= 0)
                    {
                        newState = this.DoMove(move, timeCost: 0) ?? newState;
                    }

                    return newState;
                case Move.TurnLeft:
                    return this.With(dir: (this.dir + 1) & 3);
                case Move.TurnRight:
                    return this.With(dir: (this.dir + 3) & 3);
                case Move.UseManipulatorExtension:
                    throw new NotImplementedException();
                case Move.UseFastWheels:
                    return this.fastWheelsCount <= 0
                        ? null
                        : this.With(fastWheelsCount: this.fastWheelsCount - 1, remainingSpeedBoostedMoves: 50);
                case Move.UseDrill:
                    return this.drillsCount <= 0
                        ? null
                        : this.With(drillsCount: this.drillsCount - 1, remainingDrillMoves: 30);
                default:
                    throw new ArgumentOutOfRangeException(nameof(move), move.ToString());
            }
        }

        private static (int, int) GetDelta(Move move) =>
            move switch
                {
                Move.MoveUp => (0, 1),
                Move.MoveDown => (0, -1),
                Move.MoveLeft => (-1, 0),
                Move.MoveRight => (1, 0),
                _ => throw new ArgumentOutOfRangeException(nameof(move)),
                };

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
                var (dx, dy) = delta;

                (dx, dy) = dir switch
                    {
                    0 => (dx, dy),
                    1 => (-dy, dx),
                    2 => (-dx, -dy),
                    3 => (dy, -dx),
                    _ => throw new ArgumentOutOfRangeException(nameof(dir)),
                    };

                // TODO: check visibility
                var manipCoord = (x + dx, y + dy);
                if (!wrappedCells.TryFind(manipCoord, out var val))
                {
                    wrappedCells = wrappedCells.AddOrUpdate(manipCoord, true);
                    ++wrappedCellsCount;
                }
            }

            return (wrappedCells, wrappedCellsCount);
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
                remainingSpeedBoostedMoves ?? (this.remainingSpeedBoostedMoves - timeCost),
                remainingDrillMoves ?? (this.remainingDrillMoves - timeCost));
        }

        private State? DoMove(Move move, int timeCost = 1)
        {
            var (dx, dy) = GetDelta(move);

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
                    return this.With(x: newX, y: newY);
                case Map.Cell.Obstacle:
                    return this.remainingDrillMoves <= 0
                        ? throw new InvalidOperationException()
                        : this.With(x: newX, y: newY, drilledCells: this.drilledCells.AddOrUpdate((newX, newY), true));
                case Map.Cell.FastWheels:
                    var fwCnt = CountBooster(this.fastWheelsCount);
                    return this.With(pickedUpBoosterCoords: fwCnt.PickedUp, fastWheelsCount: fwCnt.Counter);
                case Map.Cell.Drill:
                    var drillCnt = CountBooster(this.drillsCount);
                    return this.With(pickedUpBoosterCoords: drillCnt.PickedUp, drillsCount: drillCnt.Counter);
                case Map.Cell.ManipulatorExtension:
                    var manipCnt = CountBooster(this.manipulatorExtensionCount);
                    return this.With(pickedUpBoosterCoords: manipCnt.PickedUp, manipulatorExtensionCount: manipCnt.Counter);
                case Map.Cell.MysteriousPoint:
                    var mystCnt = CountBooster(this.mysteriousPointsCount);
                    return this.With(pickedUpBoosterCoords: mystCnt.PickedUp, mysteriousPointsCount: mystCnt.Counter);
                case Map.Cell.Teleport:
                    var teleCnt = CountBooster(this.teleportsCount);
                    return this.With(pickedUpBoosterCoords: teleCnt.PickedUp, teleportsCount: teleCnt.Counter);
                case Map.Cell.Edge:
                default:
                    throw new InvalidOperationException($"Unexpected cell: {this.map[newX, newY]}");
            }

            (ImHashSet PickedUp, int Counter) CountBooster(int counter) =>
                this.pickedUpBoosterCoords.TryFind((newX, newY), out var _)
                    ? (this.pickedUpBoosterCoords, counter)
                    : (this.pickedUpBoosterCoords.AddOrUpdate((newX, newY), true), counter + 1);
        }
    }
}