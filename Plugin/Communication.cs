using NLog;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace StalkR.AsteroidOres
{
    internal class Communication
    {
        public const ushort MOD_ID = 27283; // keep in sync with Mod

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        internal static void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MOD_ID, messageHandler);
            Log.Info("message handler registered");
        }

        internal static void Unregister()
        {
            if (MyAPIGateway.Multiplayer != null) MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MOD_ID, messageHandler);
        }

        private static HashSet<Vector3D> points = new HashSet<Vector3D>();
        private static List<IMyPlayer> players = new List<IMyPlayer>();
        private static List<MyVoxelBase> voxels = new List<MyVoxelBase>();
        private static void messageHandler(ushort handlerId, byte[] data, ulong steamId, bool isArrivedFromServer)
        {
            if (isArrivedFromServer) return;
            points.Clear();
            foreach (var p in MyAPIGateway.Utilities.SerializeFromBinary<Vector3D[]>(data))
            {
                points.Add(p);
            }
            if (points.Count == 0) return;

            players.Clear();
            MyAPIGateway.Players.GetPlayers(players, e => e.SteamUserId == steamId);
            if (players.Count == 0) return;
            var player = players[0];

            // twice the view distance, otherwise spectator view can see
            // asteroids that only disappear when character moves closer
            var range = MyAPIGateway.Session.SessionSettings.ViewDistance * 2;

            var sphere = new BoundingSphereD(player.GetPosition(), range);
            voxels.Clear();
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, voxels);
            foreach (MyVoxelBase voxel in voxels)
            {
                // skip MyPlanet, MyVoxelPhysics; only asteroids remain
                if (!(voxel is MyVoxelMap)) continue;
                // remove all voxels existing server-side and we're left with voxels to delete client-side
                points.Remove(voxel.PositionLeftBottomCorner);
            }

            if (points.Count == 0) return;
            MyAPIGateway.Multiplayer.SendMessageTo(MOD_ID, MyAPIGateway.Utilities.SerializeToBinary(points.ToArray<Vector3D>()), steamId);
        }
    }
}
