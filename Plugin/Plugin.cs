using HarmonyLib;
using NLog;
using Torch;
using Torch.API;
using System.IO;
using Torch.API.Managers;
using Torch.Session;
using Torch.API.Session;

namespace StalkR.AsteroidOres
{
    public class Plugin : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static Harmony harmony;
        private static Persistent<Config> _config;
        public static Config Config => _config?.Data;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            harmony = new Harmony(typeof(Plugin).Namespace);
            harmony.PatchAll();

            var sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            sessionManager.SessionStateChanged += SessionChanged;

            var path = Path.Combine(StoragePath, "AsteroidOres.cfg");
            _config = Persistent<Config>.Load(path);
            Log.Info("config loaded: " + path);
            _config.Save();
        }

        public override void Dispose()
        {
            _config.Save();
            harmony.UnpatchAll(typeof(Plugin).Namespace);
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {
            switch (state) {
                case TorchSessionState.Loaded:
                    Communication.Register();
                    break;
                case TorchSessionState.Unloaded:
                    Communication.Unregister();
                    break;
            }
        }
    }
}