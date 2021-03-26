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
using Torch.Managers;
using Torch.Managers.PatchManager;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;

namespace GridSplitNameKeeper
{
    [PatchShim]
    public static class GridPatch
    {
        private static Logger Log = PluginCore.Instance.Log;
        //private static readonly MethodInfo NewNameRequest = typeof(MyCubeGrid).GetMethod("OnChangeDisplayNameRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyCubeGrid).GetMethod("MoveBlocks",  BindingFlags.Static|BindingFlags.NonPublic)).Suffixes
                .Add(typeof(GridPatch).GetMethod(nameof(OnGridSplit), BindingFlags.Static| BindingFlags.NonPublic));
        }

        private static void OnGridSplit(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            if (!PluginCore.Instance.Config.Enable)return;
            var newName = GetName(from.DisplayName);
            var newGrid = to;
            var oldGrid = from;


            PluginCore.Instance.Torch.InvokeAsync(() =>
            {
                if (newGrid.BlocksCount > oldGrid.BlocksCount)
                {
                    if (PluginCore.Instance.Config.CleanSplits &&
                        oldGrid.BlocksCount < PluginCore.Instance.Config.SplitThreshold)
                    {
                        oldGrid.SendGridCloseRequest();
                        Log.Info($"Closing grid {newGrid.DisplayName} after splitting from {oldGrid.DisplayName}");
                    }
                }
                else
                {
                    if (PluginCore.Instance.Config.CleanSplits &&
                        newGrid.BlocksCount < PluginCore.Instance.Config.SplitThreshold)
                    {
                        newGrid.SendGridCloseRequest();
                        Log.Info($"Closing grid {newGrid.DisplayName} after splitting from {newGrid.DisplayName}");
                    }
                }
            });

            if (!PluginCore.Instance.Config.KeepSplitName) return;

            PluginCore.Instance.Torch.InvokeAsync(() =>
            {
                Thread.Sleep(100);
                newGrid.ChangeDisplayNameRequest(newName);
            });

            /*
            Task.Run(() =>
            {
                Thread.Sleep(100);
                newGrid.ChangeDisplayNameRequest(newName);
                //NetworkManager.RaiseEvent(newGrid, NewNameRequest, newName);
            });
            */
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