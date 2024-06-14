using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Collections;
using VRage.ModAPI;

namespace StalkR.AsteroidOres
{
    public class Client : ISide
    {
        private MyConcurrentHashSet<long> active = new MyConcurrentHashSet<long>();
        private MyConcurrentDictionary<long, IMyEntity> pending = new MyConcurrentDictionary<long, IMyEntity>();
        private bool isLoadingComplete = false;

        public void UnloadData()
        {
            active.Clear();
            pending.Clear();
            isLoadingComplete = false;
        }

        public void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            if (active.Contains(entity.EntityId)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: add X:{p.X} Y:{p.Y} Z:{p.Z} IsLoadingComplete:{isLoadingComplete}");
            pending.Add(entity.EntityId, entity);
            // During loading, do not touch entities or game hangs at 50% (#11).
            // It is fine as they will be removed quickly enough.
            // After game is loaded, we want to handle them immediately to avoid flickering.
            if (!isLoadingComplete) return;
            entity.Render.UpdateRenderObject(false);
            entity.Physics.Deactivate();
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
            active.Remove(entity.EntityId);
            pending.Remove(entity.EntityId);
        }

        public void UpdateBeforeSimulation100()
        {
            if (pending.Count == 0) return;
            Mod.Log($"AsteroidOres: ask server for {pending.Count} pending");
            var msg = new Message { pending = pending.Keys.ToHashSet<long>() };
            Mod.SendMessageToServer(MyAPIGateway.Utilities.SerializeToBinary(msg));
        }

        public void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (!isArrivedFromServer) return;
            // Consider game loading complete when we receive the first message from server.
            isLoadingComplete = true;
            var msg = MyAPIGateway.Utilities.SerializeFromBinary<Server.Message>(serialized);
            IMyEntity entity;
            if (msg.active != null && msg.active.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.active.Count}) active");
                foreach (var entityId in msg.active)
                {
                    if (!pending.TryRemove(entityId, out entity)) continue;
                    var p = entity.GetPosition();
                    Mod.Log($"AsteroidOres: activate X:{p.X} Y:{p.Y} Z:{p.Z}");
                    entity.Render.UpdateRenderObject(true);
                    entity.Physics.Activate();
                    pending.Remove(entityId);
                    active.Add(entity.EntityId);
                }
            }
            if (msg.removed != null && msg.removed.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.removed.Count}) removed");
                foreach (var entityId in msg.removed)
                {
                    if (!pending.TryRemove(entityId, out entity)) continue;
                    var p = entity.GetPosition();
                    Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
                    pending.Remove(entityId);
                    entity.Delete();
                }
            }
        }

        [ProtoContract]
        public class Message
        {
            [ProtoMember(1)]
            public HashSet<long> pending { get; set; }
        }
    }
}
