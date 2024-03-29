﻿namespace Icfpc2019.Solution
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    // Key doesn't really matter
    using ImHashSet = ImTools.ImHashMap<(int, int), bool>;

    public class State
    {
        public static readonly (int, int)[] InitManipConfig =
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
        private readonly int totalPickedUpManipulatorExtensions;

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
                wrapped: UpdateWrappedCells(map, map.StartX, map.StartY, 0, InitManipConfig, ImHashSet.Empty, 0, null),
                pickedUpBoosterCoords: ImHashSet.Empty,
                drilledCells: ImHashSet.Empty,
                manipulatorExtensionCount: 0,
                fastWheelsCount: 0,
                drillsCount: 0,
                teleportsCount: 0,
                cloneCount: 0,
                totalPickedUpManipulatorExtensions: 0)
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
            int cloneCount,
            int totalPickedUpManipulatorExtensions)
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
            this.totalPickedUpManipulatorExtensions = totalPickedUpManipulatorExtensions;

            // Debug.Assert(this.wrappedCellsCount == this.wrappedCells.Enumerate().Count(), "Counts do not match!");
        }

        public Map Map => this.map;
        public int BotsCount => this.bots.Length;
        public int Hash => 0;
        public int ManipulatorExtensionCount => this.manipulatorExtensionCount;
        public int CloneBoosterCount => this.cloneCount;
        public int WrappedCellsCount => this.wrappedCellsCount;
        public ImHashSet WrappedCells => this.wrappedCells;

        public int ManipulatoExtensionsOnTheFloorCount =>
            this.Map.NumManipulatorExtensions - this.totalPickedUpManipulatorExtensions;

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

        public Bot GetBot(int i) => this.bots[i];

        public bool HaveManipulatorExtensions()
        {
            if (this.ManipulatorExtensionCount > 0)
            {
                return true;
            }

            foreach (var bot in this.bots)
            {
                if (this.map[bot.X, bot.Y] == Map.Cell.ManipulatorExtension && !this.IsPickedUp(bot.X, bot.Y))
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsWrapped(int x, int y) => this.wrappedCells.TryFind((x, y), out var _);

        public bool IsPickedUp(int x, int y) => this.pickedUpBoosterCoords.TryFind((x, y), out var _);

        public bool UnwrappedCellsVisibleInZone(int x, int y, int dir, HashSet<(int, int)> zone)
        {
            foreach (var delta in this.bots[0].ManipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);
                var manipCoord = (x + dx, y + dy);
                if (this.map.IsFree(x + dx, y + dy) &&
                    !this.wrappedCells.TryFind(manipCoord, out var _) &&
                    zone.Contains(manipCoord) &&
                    this.map.AreVisible(x, y, x + dx, y + dy))
                {
                    return true;
                }
            }

            return false;
        }

        public (int numVis, int maxDist) MaxUnwrappedVisibleDistFromCenter(int x, int y, int dir, DistsFromCenter dists)
        {
            // FIXME: we consider only one bot
            var sumVis = 0;
            var max = 0;
            foreach (var delta in this.bots[0].ManipConfig)
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

        public State? Next(params Command[] commands) =>
            this.Next(null, commands);

        public State? Next(List<(int, int)>? addedWrappedCells, params Command[] commands)
        {
            if (commands.Length > this.BotsCount)
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
            var newTotalPickedUpManipulatorExtensions = this.totalPickedUpManipulatorExtensions;

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
                    ref newCloneCount,
                    ref newTotalPickedUpManipulatorExtensions);

                if (i >= commands.Length)
                {
                    newBots[i] = bot;
                    continue;
                }

                var command = commands[i];
                var newBot = command switch
                    {
                    // we expect that once commands for bot are over than nulls will be fed up indefinitely for it
                    // but do not really verify that
                    null => bot,
                    Move move => bot.DoMove(this, move, ref newWrappedCells, ref newWrappedCellsCount, ref newDrilledCells, addedWrappedCells),
                    Turn turn => bot.DoTurn(this, turn, ref newWrappedCells, ref newWrappedCellsCount, addedWrappedCells),
                    UseManipulatorExtension useManip => bot.AttachManip(
                        this,
                        useManip,
                        ref newManipulatorExtensionCount,
                        ref newWrappedCells,
                        ref newWrappedCellsCount,
                        addedWrappedCells),
                    UseFastWheels useFastWheels => bot.UseFastWheels(ref newFastWheelsCount),
                    UseDrill useDrill => bot.UseDrill(ref newDrillsCount),
                    Clone clone => bot.Clone(
                        this,
                        ref newBots,
                        ref newCloneCount,
                        ref newWrappedCells,
                        ref newWrappedCellsCount,
                        addedWrappedCells),
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
                cloneCount: newCloneCount,
                totalPickedUpManipulatorExtensions: newTotalPickedUpManipulatorExtensions);
        }

        // Useful for running strategy with single bot
        public State ReplaceBots(bool resetBoosters, params Bot[] bot)
        {
            return new State(
                map: this.map,
                bots: bot,
                wrapped: (this.wrappedCells, this.WrappedCellsCount),
                pickedUpBoosterCoords: this.pickedUpBoosterCoords,
                drilledCells: ImHashSet.Empty,
                manipulatorExtensionCount: resetBoosters ? 0 : this.manipulatorExtensionCount,
                fastWheelsCount: resetBoosters ? 0 : this.fastWheelsCount,
                drillsCount: resetBoosters ? 0 : this.drillsCount,
                teleportsCount: resetBoosters ? 0 : this.teleportsCount,
                cloneCount: resetBoosters ? 0 : this.cloneCount,
                this.totalPickedUpManipulatorExtensions);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (ImHashSet wrappedCells, int wrappedCellsCount) UpdateWrappedCells(
            Map map,
            int x,
            int y,
            int dir,
            (int, int)[] manipConfig,
            ImHashSet wrappedCells,
            int wrappedCellsCount,
            List<(int, int)>? addedWrappedCells)
        {
            foreach (var delta in manipConfig)
            {
                var (dx, dy) = TurnManip(dir, delta);

                var manipCoord = (x + dx, y + dy);
                if (map.AreVisible(x, y, x + dx, y + dy) && !wrappedCells.TryFind(manipCoord, out var _))
                {
                    wrappedCells = wrappedCells.AddOrUpdate(manipCoord, true);
                    ++wrappedCellsCount;
                    addedWrappedCells?.Add(manipCoord);

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
                    remainingSpeedBoostedMoves: (remainingSpeedBoostedMoves ?? this.RemainingSpeedBoostedMoves) - timeCost,
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
                ref int newCloneCount,
                ref int newTotalPickedUpManipulatorExtensions)
            {
                switch (state.Map[this.X, this.Y])
                {
                    case Map.Cell.Empty:
                    case Map.Cell.SpawnPoint:
                        break;
                    case Map.Cell.Obstacle:
                        if (!state.drilledCells.TryFind((this.X, this.Y), out var _))
                        {
                            throw new Exception("ooops");
                        }

                        break;
                    case Map.Cell.FastWheels:
                        this.PickUpBooster(ref newPickedUpBoosterCoords, ref newFastWheelsCount);
                        break;
                    case Map.Cell.Drill:
                        this.PickUpBooster(ref newPickedUpBoosterCoords, ref newDrillsCount);
                        break;
                    case Map.Cell.ManipulatorExtension:
                        if (this.PickUpBooster(ref newPickedUpBoosterCoords, ref newManipulatorExtensionCount))
                        {
                            ++newTotalPickedUpManipulatorExtensions;
                        }

                        break;
                    case Map.Cell.Teleport:
                        this.PickUpBooster(ref newPickedUpBoosterCoords, ref newTeleportsCount);
                        break;
                    case Map.Cell.Clone:
                        this.PickUpBooster(ref newPickedUpBoosterCoords, ref newCloneCount);
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
                ref int newWrappedCellsCount,
                ref ImHashSet newDrilledCells,
                List<(int, int)>? addedWrappedCells)
            {
                var bot = this.DoMoveImpl(state, move, ref newWrappedCells, ref newWrappedCellsCount, ref newDrilledCells, 1, addedWrappedCells);

                if (bot == null)
                {
                    return null;
                }

                // if original bot had extra moves
                if (this.RemainingSpeedBoostedMoves > 0)
                {
                    bot = bot.DoMoveImpl(state, move, ref newWrappedCells, ref newWrappedCellsCount, ref newDrilledCells, 0, addedWrappedCells) ?? bot;
                }

                return bot;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? DoMoveImpl(
                State state,
                Move move,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount,
                ref ImHashSet newDrilledCells,
                int timeCost,
                List<(int, int)>? addedWrappedCells)
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

                if (this.RemainingDrillMoves > 0 && state.Map[newX, newY] == Map.Cell.Obstacle)
                {
                    newDrilledCells = newDrilledCells.AddOrUpdate((newX, newY), true);
                }

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    newX,
                    newY,
                    this.Dir,
                    this.ManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount,
                    addedWrappedCells);

                return this.With(x: newX, y: newY, timeCost: timeCost);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot DoTurn(
                State state,
                Turn turn,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount,
                List<(int, int)>? addedWrappedCells)
            {
                var newDir = (this.Dir + turn.Ddir) & 3;

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    this.X,
                    this.Y,
                    newDir,
                    this.ManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount,
                    addedWrappedCells);

                return this.With(dir: newDir);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Bot? AttachManip(
                State state,
                UseManipulatorExtension useManip,
                ref int newManipulatorExtensionsCount,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount,
                List<(int, int)>? addedWrappedCells)
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
                    newWrappedCellsCount,
                    addedWrappedCells);

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
            public Bot? Clone(
                State state,
                ref Bot[] newBots,
                ref int newCloneCount,
                ref ImHashSet newWrappedCells,
                ref int newWrappedCellsCount,
                List<(int, int)>? addedWrappedCells)
            {
                if (--newCloneCount < 0 || state.Map[this.X, this.Y] != Map.Cell.SpawnPoint)
                {
                    return null;
                }

                var newerBots = new Bot[newBots.Length + 1];
                Array.Copy(newBots, newerBots, newBots.Length);

                newerBots[newBots.Length] = new Bot(this.X, this.Y, 0, InitManipConfig, 0, 0);
                newBots = newerBots;

                (newWrappedCells, newWrappedCellsCount) = UpdateWrappedCells(
                    state.Map,
                    this.X,
                    this.Y,
                    0,
                    InitManipConfig,
                    newWrappedCells,
                    newWrappedCellsCount,
                    addedWrappedCells);

                return this;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private bool PickUpBooster(ref ImHashSet newPickedUpBoosterCoords, ref int counter)
            {
                if (!newPickedUpBoosterCoords.TryFind((this.X, this.Y), out var _))
                {
                    ++counter;
                    newPickedUpBoosterCoords = newPickedUpBoosterCoords.AddOrUpdate((this.X, this.Y), true);
                    return true;
                }

                return false;
            }
        }
    }
}