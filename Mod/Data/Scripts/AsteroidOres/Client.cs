using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.ModAPI;

namespace StalkR.AsteroidOres
{
    public class Client : ISide
    {
        private HashSet<long> active = new HashSet<long>();
        private HashSet<long> pending = new HashSet<long>();
        private Dictionary<long, IMyEntity> pendingEntities = new Dictionary<long, IMyEntity>();

        public void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: add X:{p.X} Y:{p.Y} Z:{p.Z}");
            if (!active.Contains(entity.EntityId))
            {
                pending.Add(entity.EntityId);
                pendingEntities.Add(entity.EntityId, entity);
                entity.Render.UpdateRenderObject(false);
                entity.Physics.Deactivate();
            }
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
            active.Remove(entity.EntityId);
            pending.Remove(entity.EntityId);
            pendingEntities.Remove(entity.EntityId);
        }

        public void UpdateBeforeSimulation100()
        {
            if (pending.Count == 0) return;
            Mod.Log($"AsteroidOres: ask server for {pending.Count} pending");
            Mod.SendMessageToServer(MyAPIGateway.Utilities.SerializeToBinary(new Message { pending = pending }));
        }

        public void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (!isArrivedFromServer) return;
            var msg = MyAPIGateway.Utilities.SerializeFromBinary<Server.Message>(serialized);
            if (msg.active != null && msg.active.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.active.Count}) active");
                foreach (var entityId in msg.active)
                {
                    if (!pendingEntities.ContainsKey(entityId)) continue;
                    var entity = pendingEntities[entityId];
                    var p = entity.GetPosition();
                    Mod.Log($"AsteroidOres: activate X:{p.X} Y:{p.Y} Z:{p.Z}");
                    entity.Render.UpdateRenderObject(true);
                    entity.Physics.Activate();
                    pending.Remove(entityId);
                    pendingEntities.Remove(entityId);
                    active.Add(entity.EntityId);
                }
            }
            if (msg.removed != null && msg.removed.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.removed.Count}) active");
                foreach (var entityId in msg.removed)
                {
                    if (!pendingEntities.ContainsKey(entityId)) continue;
                    var entity = pendingEntities[entityId];
                    var p = entity.GetPosition();
                    Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
                    pending.Remove(entityId);
                    pendingEntities.Remove(entityId);
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
