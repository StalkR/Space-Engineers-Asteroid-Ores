using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace StalkR.AsteroidOres
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Mod : MySessionComponentBase
    {
        const ushort MOD_ID = 27283; // keep in sync with Plugin

        public override void LoadData()
        {
            base.LoadData();
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MOD_ID, messageHandler);
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Entities.OnEntityAdd -= OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MOD_ID, messageHandler);
        }

        public override void BeforeStart()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, (IMyEntity entity) =>
            {
                OnEntityAdd(entity);
                return false;
            });
            MyAPIGateway.Entities.OnEntityAdd += OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove;
        }

        private HashSet<long> trackedEntityIds = new HashSet<long>();
        private HashSet<IMyEntity> pending = new HashSet<IMyEntity>();

        private void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Log($"AsteroidOres: OnEntityAdd: X:{p.X} Y:{p.Y} Z:{p.Z}");
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                trackedEntityIds.Add(entity.EntityId);
                return;
            }
            // client
            if (!trackedEntityIds.Contains(entity.EntityId))
            {
                pending.Add(entity);
                entity.Render.UpdateRenderObject(false);
                entity.Physics.Deactivate();
            }
        }

        private void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Log($"AsteroidOres: OnEntityRemove: X:{p.X} Y:{p.Y} Z:{p.Z}");
            trackedEntityIds.Remove(entity.EntityId);
            updateClients = true;
        }

        private bool isAsteroid(MyVoxelBase voxel)
        {
            // skip MyPlanet, MyVoxelPhysics
            if (!(voxel is MyVoxelMap)) return false;
            // skip boulders
            if (voxel.StorageName == null || !voxel.StorageName.StartsWith("Asteroid")) return false;
            return true;
        }

        private List<IMyPlayer> players = new List<IMyPlayer>();
        private bool updateClients = false;

        public void UpdateBeforeSimulation100()
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                if (!updateClients) return;
                updateClients = false;
                players.Clear();
                MyAPIGateway.Players.GetPlayers(players);
                Log($"AsteroidOres: Update: send entity ids to clients ({players.Count})");
                foreach (var player in players)
                {
                    if (player.SteamUserId == MyAPIGateway.Multiplayer.ServerId) continue;
                    SendTrackedEntityIds(player.SteamUserId);
                }
                return;
            }
            // client
            if (pending.Count == 0) return;
            Log($"AsteroidOres: Update: asking server for tracked entity ids (pending: {pending.Count})");
            MyAPIGateway.Multiplayer.SendMessageToServer(MOD_ID, MyAPIGateway.Utilities.SerializeToBinary(true));
        }

        private void SendTrackedEntityIds(ulong steamUserId)
        {
            MyAPIGateway.Multiplayer.SendMessageTo(MOD_ID, MyAPIGateway.Utilities.SerializeToBinary(trackedEntityIds), steamUserId);
        }

        private void messageHandler(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                Log($"AsteroidOres: messageHandler: sending client ({senderPlayerId}) tracked entity ids ({trackedEntityIds.Count})");
                SendTrackedEntityIds(senderPlayerId);
                return;
            }
            // client
            if (!isArrivedFromServer) return;
            trackedEntityIds = MyAPIGateway.Utilities.SerializeFromBinary<HashSet<long>>(serialized);
            Log($"AsteroidOres: messageHandler: received tracked entity ids ({trackedEntityIds.Count}), processing pending ({pending.Count})");
            foreach (var entity in pending)
            {
                if (trackedEntityIds.Contains(entity.EntityId))
                {
                    var p = entity.GetPosition();
                    Log($"AsteroidOres: messageHandler: activate X:{p.X} Y:{p.Y} Z:{p.Z}");
                    entity.Render.UpdateRenderObject(true);
                    entity.Physics.Activate();
                }
                else
                {
                    entity.Delete();
                }
            }
            pending.Clear();
        }

        private int ticks = 0;
        public override void UpdateBeforeSimulation()
        {
            ticks++;
            if (ticks >= 100)
            {
                this.UpdateBeforeSimulation100();
                ticks = 0;
            }
        }

        private void Log(string message)
        {
            //MyLog.Default.WriteLineAndConsole(message);
        }
    }
}
