using HarmonyLib;
using NLog;
using System;
using Torch;
using Torch.API;
using System.IO;

namespace StalkR.AsteroidOres
{
    public class Plugin : TorchPluginBase
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static Harmony harmony;
        private static Persistent<Config> _config;
        public static Config Config => _config?.Data;
        public const ushort MOD_ID = 27283; // keep in sync with Mod

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);

            harmony = new Harmony(typeof(Plugin).Namespace);
            harmony.PatchAll();

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
    }
}