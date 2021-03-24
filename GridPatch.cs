using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using NLog.Fluent;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers;
using Torch.Managers.PatchManager;
using VRage.Game.ObjectBuilders.Components;
using VRage.Network;

namespace GridSplitNameKeeper
{
    [PatchShim]
    public static class GridPatch
    {
        
        //private static readonly MethodInfo NewNameRequest = typeof(MyCubeGrid).GetMethod("OnChangeDisplayNameRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(PatchContext ctx)
        {
            ctx.GetPattern(typeof(MyCubeGrid).GetMethod("MoveBlocks",  BindingFlags.Static|BindingFlags.NonPublic)).Suffixes
                .Add(typeof(GridPatch).GetMethod(nameof(OnGridSplit), BindingFlags.Static| BindingFlags.NonPublic));
        }

        private static void OnGridSplit(ref MyCubeGrid from, ref MyCubeGrid to)
        {
            var newName = GetName(from.DisplayName);
            var newGrid = to;

            Task.Run(() =>
            {
                Thread.Sleep(100);
                newGrid.ChangeDisplayNameRequest(newName);
                //NetworkManager.RaiseEvent(newGrid, NewNameRequest, newName);
            });

        }

        private static string GetName(string current)
        {
            double count = 0;
            var grids = MyEntities.GetEntities().OfType<MyCubeGrid>().ToList();
            foreach (var grid in grids)
            {
                if (!grid.DisplayName.Contains(current)) continue;
                count++;
            }

            return $"{current} {count:00}";
        }

    }
}