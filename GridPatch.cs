using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Torch.Managers;
using Torch.Managers.PatchManager;
using VRage.Network;

namespace GridSplitNameKeeper
{
    [PatchShim]
    public static class GridPatch
    {
        private static readonly ILogger Log = LogManager.GetLogger("GridSplitNameKeeper");
        static readonly ConcurrentQueue<(long newGridId, string newName, int frameCount, GridAction action)> _queuedAction = new ConcurrentQueue<(long, string, int,GridAction)>();

        public static void Patch(PatchContext ctx)
        {
            var patchee = typeof(MyCubeGrid).GetMethod("MoveBlocks", BindingFlags.Static | BindingFlags.NonPublic);
            var patcher = typeof(GridPatch).GetMethod(nameof(OnGridSplit), BindingFlags.Static | BindingFlags.NonPublic);
            ctx.GetPattern(patchee).Suffixes.Add(patcher);
        }

        private static void OnGridSplit(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            if (!PluginCore.Instance.Config.Enable) return;

            var fromGrid = MyEntities.GetEntityById(from.EntityId) as MyCubeGrid;
            var toGrid = MyEntities.GetEntityById(to.EntityId) as MyCubeGrid;
            if (fromGrid == null || !IsOpen(fromGrid)) return;
            if (toGrid == null || !IsOpen(toGrid)) return;
            var targetFrameCount = MySession.Static.GameplayFrameCounter + 60 * 2;
            if (PluginCore.Instance.Config.CleanSplits)
            {
                if (fromGrid.BlocksCount < toGrid.BlocksCount)
                {
                    _queuedAction.Enqueue((fromGrid.EntityId,toGrid.DisplayName,targetFrameCount-10,GridAction.delete));
                    //TryClose(fromGrid, toGrid.DisplayName);
                }
                else
                {
                    //TryClose(toGrid, fromGrid.DisplayName);
                    _queuedAction.Enqueue((toGrid.EntityId,fromGrid.DisplayName,targetFrameCount-10,GridAction.delete));
                }
            }

            if (!PluginCore.Instance.Config.KeepSplitName || toGrid.BlocksCount <= PluginCore.Instance.Config.RenameGridBlockMin) return;
            var newName = CreateName(from.DisplayName);
            toGrid.ChangeDisplayNameRequest(newName);
            Log.Info($"Renamed {toGrid.DisplayName} to {newName}");
            // https://discord.com/channels/929141809769226271/929144465782882324/948240055007322242
            // > clients doing it's own separated init without syncing object builder with server
            // > so you have to invoke change custom name request for grid a 1-2 seconds later
            _queuedAction.Enqueue((to.EntityId, newName, targetFrameCount,GridAction.changeName));
        }

        public static void OnGameLoop()
        {
            while (_queuedAction.TryPeek(out var p) &&
                   p.frameCount >= MySession.Static.GameplayFrameCounter)
            {
                _queuedAction.TryDequeue(out p);
                var (newGridId, newName, _,gridAction) = p;
                if (!MyEntities.TryGetEntityById(newGridId, out var newGrid)) continue;
                if (newGrid.Closed || newGrid.MarkedForClose) continue;
                if (gridAction == GridAction.delete)
                {
                    TryClose((MyCubeGrid)newGrid,newName);
                }
                else
                {
                    ((MyCubeGrid)newGrid).ChangeDisplayNameRequest(newName);
                }
            }
        }

        private static bool IsOpen(MyCubeGrid grid)
        {
            if (grid.Closed) return false;
            return !grid.MarkedForClose;
        }

        private static void TryClose(MyCubeGrid smallerGrid, string largerGridName)
        {
            if (smallerGrid.BlocksCount > PluginCore.Instance.Config.SplitThreshold) return;
            if (ContainsAnyBlocks(smallerGrid, PluginCore.Instance.Config.IgnoreBlockList)) return;
            if (smallerGrid.GetBlocks().Any(HasPilot)) return;

            smallerGrid.SendGridCloseRequest();
            Log.Info($"Closing grid {smallerGrid.DisplayName} after splitting from {largerGridName}");
        }

        private static string CreateName(string gridName)
        {
            var (gridNameWithoutPrefix, count) = SplitNameBySuffix(gridName);
            return $"{gridNameWithoutPrefix} {count + 1}";
        }

        private static (string, int) SplitNameBySuffix(string gridName)
        {
            var lastSpaceIndex = gridName.LastIndexOf(' ');
            if (lastSpaceIndex < 0) return (gridName, 0);

            var suffixIndex = lastSpaceIndex + 1;
            if (suffixIndex >= gridName.Length) return (gridName, 0);

            var suffixStr = gridName.Substring(suffixIndex, gridName.Length - suffixIndex);
            if (!int.TryParse(suffixStr.Trim(), out var numbering)) return (gridName, 0);

            var gridNameWithoutSuffix = gridName.Substring(0, lastSpaceIndex);
            return (gridNameWithoutSuffix, numbering);
        }

        private static bool HasPilot(MySlimBlock block)
        {
            return block.FatBlock is MyShipController controller && controller.Pilot != null;
        }

        private static bool ContainsAnyBlocks(MyCubeGrid grid, IEnumerable<string> blockTypeIds)
        {
            foreach (var block in grid.CubeBlocks) //todo optimize
            {
                var def = block.BlockDefinition.Id;
                if (blockTypeIds.Contains(def.SubtypeId.ToString(), StringComparer.CurrentCultureIgnoreCase) || blockTypeIds.Contains(def.TypeId.ToString().Substring(16)))
                {
                    return true;
                }
            }

            return false;
        }

        private enum GridAction
        {
            delete,
            changeName
        }
    }
}