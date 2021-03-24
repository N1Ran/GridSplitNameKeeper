using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;
using Torch.Utils;
using VRage.Network;

namespace GridSplitNameKeeper
{
    public class GridSplitNameKeeperCore : TorchPluginBase
    {
        public readonly Logger Log = LogManager.GetLogger("GridSplitNameKeeper");
        public static GridSplitNameKeeperCore Instance { get; private set; }
        private TorchSessionManager _sessionManager;


        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;
        }

        private static void SessionChanged(ITorchSession session, TorchSessionState newState)
        {
            switch (newState)
            {
                case TorchSessionState.Loading:
                    break;
                case TorchSessionState.Loaded:
                    break;
                case TorchSessionState.Unloading:
                    break;
                case TorchSessionState.Unloaded:
                    break;
                default:
                    return;
            }
        }



    }
}
