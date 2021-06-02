using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using NLog;
using NLog.Fluent;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using Torch.Managers;
using Torch.Managers.PatchManager;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;

namespace GridSplitNameKeeper
{
    [PatchShim]
    public static class GridPatch
    {
        private static readonly Logger Log = PluginCore.Instance.Log;
        //private static readonly MethodInfo NewNameRequest = typeof(MyCubeGrid).GetMethod("OnChangeDisplayNameRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(PatchContext ctx)
        {
            /*
            ctx.GetPattern(
                    typeof(MyCubeGridGroups).GetMethod("CreateLink", BindingFlags.Public | BindingFlags.Instance))
                .Suffixes.Add(typeof(GridPatch).GetMethod(nameof(CreateLink), BindingFlags.Static | BindingFlags.NonPublic ));
            */
            ctx.GetPattern(typeof(MyCubeGrid).GetMethod("MoveBlocks",  BindingFlags.Static|BindingFlags.NonPublic)).Suffixes
                .Add(typeof(GridPatch).GetMethod(nameof(OnGridSplit), BindingFlags.Static| BindingFlags.NonPublic));
        }
        /*
        private static void CreateLink(GridLinkTypeEnum type, long linkId, MyCubeGrid parent, MyCubeGrid child)
        {
            Log.Warn("Link Created");
        }

        */

        private static void OnGridSplit(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            if (!PluginCore.Instance.Config.Enable)return;
            var newName = GetName(from.DisplayName);
            var newGrid = to;
            var oldGrid = from;


            if (PluginCore.Instance.Config.CleanSplits)
            {
                PluginCore.Instance.Torch.InvokeAsync(() =>
                {
                    if (newGrid.BlocksCount > oldGrid.BlocksCount)
                    {
                        if (SkipGrid(oldGrid))return;
                        if (oldGrid.BlocksCount >= PluginCore.Instance.Config.SplitThreshold || oldGrid.GetBlocks()
                                .Count(x => x.FatBlock is MyShipController controller && controller.Pilot != null) !=
                            0) return;
                        oldGrid.SendGridCloseRequest();
                        Log.Info($"Closing grid {oldGrid.DisplayName} after splitting from {newGrid.DisplayName}");
                    }
                    else
                    {
                        if (SkipGrid(newGrid)) return;
                        if (newGrid.BlocksCount >= PluginCore.Instance.Config.SplitThreshold || newGrid.GetBlocks()
                                .Count(x => x.FatBlock is MyShipController controller && controller.Pilot != null) !=
                            0) return;
                        newGrid.SendGridCloseRequest();
                        Log.Info($"Closing grid {newGrid.DisplayName} after splitting from {oldGrid.DisplayName}");
                    }
                });
            }

            if (!PluginCore.Instance.Config.KeepSplitName) return;

            PluginCore.Instance.Torch.InvokeAsync(() =>
            {
                Thread.Sleep(100);
                newGrid.ChangeDisplayNameRequest(newName);
            });

       }

        private static bool SkipGrid(MyCubeGrid grid)
        {
            var foundIgnoreBlock = false;
            foreach (var block in grid.CubeBlocks)
            {
                if (!PluginCore.Instance.Config.IgnoreBlockList.Contains(block.BlockDefinition.Id.SubtypeId.ToString(),StringComparer.CurrentCultureIgnoreCase))continue;
                foundIgnoreBlock = true;
                break;
            }

            return foundIgnoreBlock;
        }

        private static string GetName(string current)
        {
            double count = 0;
            var grids = new List<MyCubeGrid>(MyEntities.GetEntities().OfType<MyCubeGrid>());

            foreach (var grid in grids)
            {
                if (!grid.DisplayName.Contains(current)) continue;
                count++;
            }

            return $"{current} {count:00}";
        }

    }
}