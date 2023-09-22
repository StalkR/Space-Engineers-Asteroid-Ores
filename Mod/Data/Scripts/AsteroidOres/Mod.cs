using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRageMath;

namespace StalkR.AsteroidOres
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Mod : MySessionComponentBase
    {
        const ushort MOD_ID = 27283; // keep in sync with Plugin

        public override void LoadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer) return;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(MOD_ID, messageHandler);
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.Multiplayer.IsServer) return;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(MOD_ID, messageHandler);
        }

        private List<MyVoxelBase> voxels = new List<MyVoxelBase>();
        private HashSet<Vector3D> inRange = new HashSet<Vector3D>();
        private HashSet<Vector3D> tracked = new HashSet<Vector3D>();
        private List<Vector3D> request = new List<Vector3D>();

        public override void UpdateBeforeSimulation()
        {
            if (MyAPIGateway.Multiplayer.IsServer) return;

            if (MyAPIGateway.Session == null
                || MyAPIGateway.Session.IsCameraUserControlledSpectator
                || MyAPIGateway.Session.Player == null
                || MyAPIGateway.Session.SessionSettings == null) return;
            var position = MyAPIGateway.Session.Player.GetPosition();

            // twice the view distance, otherwise spectator view can see
            // asteroids that only disappear when character moves closer
            var range = MyAPIGateway.Session.SessionSettings.ViewDistance * 2;

            // get all voxels in range, delete the ones we need to
            voxels.Clear();
            inRange.Clear();
            var sphere = new BoundingSphereD(position, range);
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, voxels);
            foreach (var voxel in voxels)
            {
                // skip MyPlanet, MyVoxelPhysics; only asteroids remain
                if (!(voxel is MyVoxelMap)) continue;
                if (deletes.Contains(voxel.PositionLeftBottomCorner))
                {
                    voxel.Delete();
                    continue;
                }
                inRange.Add(voxel.PositionLeftBottomCorner);
            }
            deletes.Clear();

            // forget voxels out of range
            tracked.RemoveWhere(k => !inRange.Contains(k));

            // track and request new voxels
            request.Clear();
            foreach (var v in inRange)
            {
                if (!tracked.Contains(v))
                {
                    tracked.Add(v);
                    request.Add(v);
                }
            }

            if (request.Count == 0) return;
            MyAPIGateway.Multiplayer.SendMessageToServer(MOD_ID, MyAPIGateway.Utilities.SerializeToBinary(request.ToArray()));
        }

        HashSet<Vector3D> deletes = new HashSet<Vector3D>();

        private void messageHandler(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (!isArrivedFromServer) return;
            foreach (var v in MyAPIGateway.Utilities.SerializeFromBinary<Vector3D[]>(serialized))
            {
                deletes.Add(v);
            }
        }
    }
}
