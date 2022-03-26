using System;
using System.IO;
using System.Windows.Controls;
using NLog;
using Torch;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Plugins;
using Torch.API.Session;
using Torch.Session;

namespace GridSplitNameKeeper
{
    public class PluginCore : TorchPluginBase, IWpfPlugin
    {
        static readonly ILogger Log = LogManager.GetCurrentClassLogger();
        public static PluginCore Instance { get; private set; }
        private Control _control;

        public UserControl GetControl() => _control ?? (_control = new Control(this));
        private Persistent<Config> _config;
        private TorchSessionManager _sessionManager;
        public Config Config => _config?.Data;

        public void Save() => _config.Save();

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Instance = this;
            _sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (_sessionManager != null)
                _sessionManager.SessionStateChanged += SessionChanged;

            LoadConfig();
            LoggerConfig.Set();

        }

        public void LoadConfig()
        {
            var configFile = Path.Combine(StoragePath, "GridSplitNameKeeper.cfg");

            try 
            {

                _config = Persistent<Config>.Load(configFile);

            }
            catch (Exception e) 
            {
                Log.Warn(e);
            }

            if (_config?.Data != null) return;

            Log.Info("Created Default Config, because none was found!");

            _config = new Persistent<Config>(configFile, new Config());
            _config.Save();

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

        public override void Update()
        {
            GridPatch.OnGameLoop();
        }
    }
}
