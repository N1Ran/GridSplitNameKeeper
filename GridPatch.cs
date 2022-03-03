using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers.PatchManager;

namespace GridSplitNameKeeper
{
    [PatchShim]
    public static class GridPatch
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        static readonly ConcurrentQueue<(long newGridId, string newName, int frameCount)> _changedNames = new ConcurrentQueue<(long, string, int)>();

        public static void Patch(PatchContext ctx)
        {
            var patchee = typeof(MyCubeGrid).GetMethod("MoveBlocks", BindingFlags.Static | BindingFlags.NonPublic);
            var patcher = typeof(GridPatch).GetMethod(nameof(OnGridSplit), BindingFlags.Static | BindingFlags.NonPublic);
            ctx.GetPattern(patchee).Suffixes.Add(patcher);
        }

        static void OnGridSplit(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            if (!PluginCore.Instance.Config.Enable) return;

            var fromGrid = MyEntities.GetEntityById(from.EntityId) as MyCubeGrid;
            var toGrid = MyEntities.GetEntityById(to.EntityId) as MyCubeGrid;
            if (fromGrid == null || !IsOpen(fromGrid)) return;
            if (toGrid == null || !IsOpen(toGrid)) return;

            if (PluginCore.Instance.Config.CleanSplits)
            {
                if (fromGrid.BlocksCount < toGrid.BlocksCount)
                {
                    TryClose(fromGrid, toGrid.DisplayName);
                }
                else
                {
                    TryClose(toGrid, fromGrid.DisplayName);
                }
            }

            if (PluginCore.Instance.Config.KeepSplitName)
            {
                var newName = CreateName(from.DisplayName);
                toGrid.ChangeDisplayNameRequest(newName);

                // https://discord.com/channels/929141809769226271/929144465782882324/948240055007322242
                // > clients doing it's own separated init without syncing object builder with server
                // > so you have to invoke change custom name request for grid a 1-2 seconds later
                var targetFrameCount = MySession.Static.GameplayFrameCounter + 60 * 2;
                _changedNames.Enqueue((to.EntityId, newName, targetFrameCount));
            }
        }

        public static void OnGameLoop()
        {
            while (_changedNames.TryPeek(out var p) &&
                   p.frameCount >= MySession.Static.GameplayFrameCounter)
            {
                _changedNames.TryDequeue(out p);
                var (newGridId, newName, _) = p;
                if (MyEntities.TryGetEntityById(newGridId, out var newGrid))
                {
                    ((MyCubeGrid)newGrid).ChangeDisplayNameRequest(newName);
                }
            }
        }

        static bool IsOpen(MyCubeGrid grid)
        {
            if (grid.Closed) return false;
            if (grid.MarkedForClose) return false;
            return true;
        }

        static void TryClose(MyCubeGrid smallerGrid, string largerGridName)
        {
            if (smallerGrid.BlocksCount > PluginCore.Instance.Config.SplitThreshold) return;
            if (ContainsAnyBlocks(smallerGrid, PluginCore.Instance.Config.IgnoreBlockList)) return;
            if (smallerGrid.GetBlocks().Any(x => HasPilot(x))) return;

            smallerGrid.SendGridCloseRequest();
            Log.Info($"Closing grid {smallerGrid.DisplayName} after splitting from {largerGridName}");
        }

        static string CreateName(string gridName)
        {
            var (gridNameWithoutPrefix, count) = SplitNameBySuffix(gridName);
            return $"{gridNameWithoutPrefix} {count + 1}";
        }

        static (string, int) SplitNameBySuffix(string gridName)
        {
            var lastSpaceIndex = gridName.LastIndexOf(' ');
            if (lastSpaceIndex < 0) return (gridName, 0);

            var suffixIndex = lastSpaceIndex + 1;
            if (suffixIndex >= gridName.Length) return (gridName, 0);

            var gridNameWithoutSuffix = gridName.Substring(0, lastSpaceIndex);
            var suffixStr = gridName.Substring(suffixIndex, gridName.Length - suffixIndex);
            int.TryParse(suffixStr, out var suffix);

            return (gridNameWithoutSuffix, suffix);
        }

        static bool HasPilot(MySlimBlock block)
        {
            return block.FatBlock is MyShipController controller && controller.Pilot != null;
        }

        static bool ContainsAnyBlocks(MyCubeGrid grid, IEnumerable<string> blockTypeIds)
        {
            foreach (var block in grid.CubeBlocks) //todo optimize
            {
                var typeId = block.BlockDefinition.Id.SubtypeId.ToString();
                if (blockTypeIds.Contains(typeId, StringComparer.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}