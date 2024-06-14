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
        // As far as I tested, now it works....
        // I also saw some negative <long> values (game debuggin via dnSpy) while logging,
        // so I set correct types instead of var where needed
        // Game loads entities in parallel, so hashset needs to be threadsafe  
        private MyConcurrentHashSet<long> active = new MyConcurrentHashSet<long>();
        private MyConcurrentDictionary<long, IMyEntity> pendingEntities = new MyConcurrentDictionary<long, IMyEntity>();
        
        // false = game is loading || true = game fully loaded (character present etc.)
        private bool game = false;

        public void UnloadData()
        {
            active.Clear();
            pendingEntities.Clear();
        }

        public void OnEntityAdd(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            if (!active.Contains(entity.EntityId))
            {
                pendingEntities.Add(entity.EntityId, entity);
                var p = entity.GetPosition();
                Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
                Mod.Log($"AsteroidOres: loading finished? {game} ");
                // DO NOT perform render changes to entities on load
                // otherwise entity could be removed by the client but the server does not get it
                // So the server waits for this removed entity to get loaded -> 50% hang 
                // This happens when other grids loads ore are still present at server
                // So error did not happen when the grid around the player needs to be loaded
                // Thats why that after a server restart players are able to connect without error
                // remember game loads stuff in parallel here
                // We need to perform render changes when game is running so there are no "plopping" 
                // roids like in the past version

                // QUESTION:
                // Is there a chance that a player joins - spawns roid within space at another
                // player and this player ram it? Gap time is loading time till reoid removed...
                // 
                if (game)
                {
                    entity.Render.UpdateRenderObject(false);
                    entity.Physics.Deactivate();
                }
            }
        }

        public void OnEntityRemove(IMyEntity entity)
        {
            if (!(entity is MyVoxelBase)) return;
            if (!Mod.isAsteroid(entity as MyVoxelBase)) return;
            var p = entity.GetPosition();
            Mod.Log($"AsteroidOres: remove X:{p.X} Y:{p.Y} Z:{p.Z}");
            active.Remove(entity.EntityId);
            pendingEntities.Remove(entity.EntityId);
        }

        public void UpdateBeforeSimulation100()
        {
            if (pendingEntities.Count == 0) return;
            // Not needed anymore, had no effect at all. False positive.
            //if (MyAPIGateway.Session?.Player?.Character == null) return;
            Mod.Log($"AsteroidOres: ask server for {pendingEntities.Count} pending");
            Mod.SendMessageToServer(MyAPIGateway.Utilities.SerializeToBinary(
                new Message { pending = pendingEntities.Keys.ToHashSet<long>() }
                ));
        }

        public void OnMessage(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (!isArrivedFromServer) return;
            // Not needed anymore, had no effect at all. False positive.
            //if (MyAPIGateway.Session?.Player?.Character == null) return;
            var msg = MyAPIGateway.Utilities.SerializeFromBinary<Server.Message>(serialized);            
            IMyEntity entity;

            if (msg.active != null && msg.active.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.active.Count}) active");
                foreach (var entityId in msg.active)
                {
                    if (!pendingEntities.TryRemove(entityId, out entity)) continue;
                    entity.Render.UpdateRenderObject(true);
                    entity.Physics.Activate();
                    active.Add(entity.EntityId);
                }
            }
            if (msg.removed != null && msg.removed.Count > 0)
            {
                Mod.Log($"AsteroidOres: server sent ({msg.removed.Count}) removed");
                foreach (var entityId in msg.removed)
                {
                    if (!pendingEntities.TryRemove(entityId, out entity)) continue;
                    // QUESTION: Why delete() instead of close() ?
                    entity.Delete();
                    // no need to put other stuff here becuase OnEntityRemove is called anyway
                }
            }
            // Now the game should be running.
            // We need a way to monitor when loading is completed
            // Because a lot is in parallel, 50% hang could occur if there are very very huge grids to load...
            // I need to add a gui / hud check, then player would see poping roids one time but thats it...
            // Tested and working with about 500k PCU on load
            game = true;
        }

        [ProtoContract]
        public class Message
        {
            [ProtoMember(1)]
            public HashSet<long> pending { get; set; }
        }
    }
}
