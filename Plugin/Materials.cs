using NLog;
using Sandbox.Game.World.Generator;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game;
using VRageMath;

namespace StalkR.AsteroidOres
{
    internal class Materials
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static HashSet<string> ores = new HashSet<string>();

        internal static bool ShouldGenerate(MyObjectSeed myObjectSeed, object myCompositeShapeProvider)
        {
            var should = shouldGenerate(myObjectSeed, myCompositeShapeProvider);
            if (!should)
            {
                // tell clients to delete the procedurally generated voxelmap at this location
                MyAPIGateway.Multiplayer.SendMessageToOthers(Plugin.MOD_ID, MyAPIGateway.Utilities.SerializeToBinary<Vector3D>(myObjectSeed.BoundingVolume.Center));
            }
            return should;
        }

        private static bool shouldGenerate(MyObjectSeed myObjectSeed, object myCompositeShapeProvider)
        {
            Vector3D p = myObjectSeed.BoundingVolume.Center;
            ListOres(myCompositeShapeProvider);

            foreach (var zone in Plugin.Config.Zones)
            {
                if (Vector3D.DistanceSquared(p, zone.Center) < zone.Radius * zone.Radius)
                {

                    if (zone.AllOres) return true;
                    foreach (var ore in ores)
                    {
                        if (!zone.Ores.Contains(ore)) return false;
                    }
                    return true;
                }
            }

            if (Plugin.Config.AllOres) return true;
            foreach (var ore in ores)
            {
                if (!Plugin.Config.Ores.Contains(ore)) return false;
            }
            return true;
        }

        private static void ListOres(object myCompositeShapeProvider)
        {
            var infoProvider = myCompositeShapeProvider.GetType().GetField("m_infoProvider", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(myCompositeShapeProvider);
            // can be either MyProceduralCompositeInfoProvider, or MyCombinedCompositeInfoProvider which inherits from it in which case we need the base type
            Type t = infoProvider.GetType();
            if (t.ToString() == "MyCombinedCompositeInfoProvider")
            {
                t = t.BaseType;
            }
            var defaultMaterial = t.GetField("m_defaultMaterial", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(infoProvider) as MyVoxelMaterialDefinition;

            ores.Clear();
            if (defaultMaterial.MinedOre != "Stone")
            {
                // in practice, that's just the 1% chance of ice asteroid
                ores.Add(defaultMaterial.MinedOre);
            }

            var deposits = infoProvider.GetType().GetField("m_deposits", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(infoProvider);
            foreach (var e in deposits as object[])
            {
                var deposit = e.GetType().GetField("m_deposit", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(e); // MyCompositeShapeOreDeposit
                var material = deposit.GetType().GetField("m_material", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(deposit) as MyVoxelMaterialDefinition;
                ores.Add(material.MinedOre);
            }
        }
    }
}
