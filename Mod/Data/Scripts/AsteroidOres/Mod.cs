using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.Components;
using VRageMath;

namespace StalkR.AsteroidOres
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
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

        List<MyVoxelBase> voxels = new List<MyVoxelBase>();

        private void messageHandler(ushort handlerId, byte[] serialized, ulong senderPlayerId, bool isArrivedFromServer)
        {
            if (!isArrivedFromServer) return;
            Vector3D p = MyAPIGateway.Utilities.SerializeFromBinary<Vector3D>(serialized);
            var sphere = new BoundingSphereD(p, 1);
            voxels.Clear();
            MyGamePruningStructure.GetAllVoxelMapsInSphere(ref sphere, voxels);
            foreach (MyVoxelBase voxel in voxels)
            {
                // skip MyPlanet, MyVoxelPhysics; only asteroids remain
                if (!(voxel is MyVoxelMap)) continue;
                voxel.Delete();
            }
        }
    }
}
