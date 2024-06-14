using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.ModAPI;

namespace StalkR.AsteroidOres
{
    public class Server : ISide
    {
        private HashSet<long> tracked = new HashSet<long>();

        public void UnloadData()
        {
            tracked.Clear();
        }

        public void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: add X:{p.X} Y:{p.Y} Z:{p.Z}");
            tracked.Add(entity.EntityId);
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
            tracked.Remove(entity.EntityId);
        }

        public void UpdateBeforeSimulation100() { }

        public void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (isArrivedFromServer) return;
            var request = MyAPIGateway.Utilities.SerializeFromBinary<Client.Message>(serialized);
            if (request.pending.Count == 0) return;
            var response = new Message
            {
                active = new HashSet<long>(),
                removed = new HashSet<long>(),
            };

            foreach (var entityId in request.pending)
            {
                if (tracked.Contains(entityId))
                    response.active.Add(entityId);
                else
                    response.removed.Add(entityId);
            }
            Mod.Log($"AsteroidOres: {senderPlayerId} asked {request.pending.Count} pending, responded {response.active.Count} active and {response.removed.Count} removed");
            Mod.SendMessageTo(MyAPIGateway.Utilities.SerializeToBinary(response), senderPlayerId);
        }

        [ProtoContract]
        public class Message
        {
            [ProtoMember(1)]
            public HashSet<long> active { get; set; }
            [ProtoMember(2)]
            public HashSet<long> removed { get; set; }
        }
    }
}
