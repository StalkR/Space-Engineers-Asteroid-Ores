using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.ModAPI;

namespace StalkR.AsteroidOres
{
    public class Client : ISide
    {
        private HashSet<long> active;
        private HashSet<long> pending;
        private Dictionary<long, IMyEntity> pendingEntities;
        private bool spawned;

        public Client() {
            this.active = new HashSet<long>();
            this.pending = new HashSet<long>();
            this.pendingEntities = new Dictionary<long, IMyEntity>();

            Mod.Log($"AsteroidOres: Client up");
        }

        public void UnloadData()
        {
            this.active.Clear();
            this.pending.Clear();
            this.pendingEntities.Clear();
        }

        public void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            //var p = entity.GetPosition(); // ToDo: no need for p if debug is off
            //Mod.Log($"AsteroidOres: add X:{p.X} Y:{p.Y} Z:{p.Z}");
            if (!this.active.Contains(entity.EntityId))
            {
                this.pending.Add(entity.EntityId);
                this.pendingEntities.Add(entity.EntityId, entity);
                entity.Render.UpdateRenderObject(false);
                entity.Physics.Deactivate();
            }
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            //var p = entity.GetPosition();
            //Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
            this.active.Remove(entity.EntityId);
            this.pending.Remove(entity.EntityId);
            this.pendingEntities.Remove(entity.EntityId);
        }

        public void UpdateBeforeSimulation100()
        {
            if (this.pending.Count == 0) return;
            Mod.Log($"AsteroidOres: ask server for {this.pending.Count} pending");
            Mod.SendMessageToServer(MyAPIGateway.Utilities.SerializeToBinary(new Message { pending = pending }));
        }

        public void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
			if (!isArrivedFromServer) return;
			Mod.Log($"AsteroidOres: Msg from server");
			if (!spawned)
			{
				IMyCharacter character = MyAPIGateway.Session?.Player?.Character;
				if (character != null) { // wait until first spawn 
					spawned = true;                
					Mod.Log($"AsteroidOres: Ready Player One");
				}
                // Do not optimize, drop first msg for more delay
				return;
			}
			
            var msg = MyAPIGateway.Utilities.SerializeFromBinary<Server.Message>(serialized);
            if (msg.active != null && msg.active.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.active.Count}) active");
                foreach (var entityId in msg.active)  // Parallel.ForEach ??
                {
                    if (!this.pendingEntities.ContainsKey(entityId)) continue;
                    var entity = pendingEntities[entityId];
                    //var p = entity.GetPosition();
                    //Mod.Log($"AsteroidOres: activate X:{p.X} Y:{p.Y} Z:{p.Z}");
                    entity.Render.UpdateRenderObject(true);
                    entity.Physics.Activate();
                    this.pending.Remove(entityId);
                    this.pendingEntities.Remove(entityId);
                    this.active.Add(entity.EntityId);
                }
            }
            if (msg.removed != null && msg.removed.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.removed.Count}) removed");
                foreach (var entityId in msg.removed)  // Parallel.ForEach ??
                {
                    if (!this.pendingEntities.ContainsKey(entityId)) continue;
                    var entity = pendingEntities[entityId];
                    //var p = entity.GetPosition();
                    //Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
                    this.pending.Remove(entityId);
                    this.pendingEntities.Remove(entityId);
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
