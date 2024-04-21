using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Utils;

namespace StalkR.AsteroidOres
{
    // ISide abstracts each side (client/server) implementations.
    interface ISide
    {
        void OnEntityAdd(IMyEntity entity);
        void OnEntityRemove(IMyEntity entity);
        void UpdateBeforeSimulation100();
        void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer);
    }

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Mod : MySessionComponentBase
    {
        const ushort MOD_ID = 27283;
        private ISide side;

        public override void LoadData()
        {
            base.LoadData();
            if (MyAPIGateway.Multiplayer.IsServer) side = new Server();
            else side = new Client();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MOD_ID, side.OnMessage);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Entities.OnEntityAdd -= side.OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= side.OnEntityRemove;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MOD_ID, side.OnMessage);
        }

        public override void BeforeStart()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, (IMyEntity entity) =>
            {
                side.OnEntityAdd(entity);
                return false;
            });
            MyAPIGateway.Entities.OnEntityAdd += side.OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += side.OnEntityRemove;
        }

        private int ticks = 0;
        public override void UpdateBeforeSimulation()
        {
            ticks++;
            if (ticks >= 100)
            {
                side.UpdateBeforeSimulation100();
                ticks = 0;
            }
        }

        public static bool isAsteroid(MyVoxelBase voxel)
        {
            // skip MyPlanet, MyVoxelPhysics
            if (!(voxel is MyVoxelMap)) return false;
            // skip boulders
            if (voxel.StorageName == null || !voxel.StorageName.StartsWith("Asteroid")) return false;
            return true;
        }

        public static bool SendMessageTo(byte[] message, ulong recipient)
        {
            return MyAPIGateway.Multiplayer.SendMessageTo(MOD_ID, message, recipient);
        }

        public static bool SendMessageToServer(byte[] message)
        {
            return MyAPIGateway.Multiplayer.SendMessageToServer(MOD_ID, message);
        }

        public static void Log(string message)
        {
            //MyLog.Default.WriteLineAndConsole(message);
        }
    }
}
