using NLog;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game;
using VRageMath;
using static HarmonyLib.Code;

namespace StalkR.AsteroidOres
{
    internal class Check
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        static HashSet<string> ores = new HashSet<string>();

        internal static bool ShouldGenerate(MyObjectSeed myObjectSeed, object myCompositeShapeProvider)
        {
            Vector3D p = myObjectSeed.BoundingVolume.Center;
            ListOres(myCompositeShapeProvider);

            foreach (var zone in Plugin.Config.Zones)
            {
                if (zone.Contains(p))
                {
                    if (zone.AllOres)
                    {
                        //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): contained in zone (X:{4} Y:{5} Z:{6} max:{7} min:{8} h:{9}), all ores: keep", p.X, p.Y, p.Z, string.Join("/", ores), zone.Center.X, zone.Center.Y, zone.Center.Z, zone.MaxRadius, zone.MinRadius, zone.Height));
                        return true;
                    }
                    foreach (var ore in ores)
                    {
                        if (!zone.Ores.Contains(ore))
                        {
                            //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): contained in zone (X:{4} Y:{5} Z:{6} max:{7} min:{8} h:{9}), does not contain {10}: remove", p.X, p.Y, p.Z, string.Join("/", ores), zone.Center.X, zone.Center.Y, zone.Center.Z, zone.MaxRadius, zone.MinRadius, zone.Height, ore));
                            return false;
                        }
                    }
                    //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): contained in zone (X:{4} Y:{5} Z:{6} max:{7} min:{8} h:{9}), contains desired ores {10}: keep", p.X, p.Y, p.Z, string.Join("/", ores), zone.Center.X, zone.Center.Y, zone.Center.Z, zone.MaxRadius, zone.MinRadius, zone.Height, string.Join("/", zone.Ores)));
                    return true;
                }
            }

            if (Plugin.Config.AllOres)
            {
                //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): in space, all ores: keep", p.X, p.Y, p.Z, string.Join("/", ores)));
                return true;
            }
            foreach (var ore in ores)
            {
                if (!Plugin.Config.Ores.Contains(ore))
                {
                    //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): in space, does not contain {4}: remove", p.X, p.Y, p.Z, string.Join("/", ores), ore));
                    return false;
                }
            }
            //Log.Info(string.Format("Asteroid at X:{0} Y:{1} Z:{2} ({3}): in space, contains desired ores {4}: keep", p.X, p.Y, p.Z, string.Join("/", ores), string.Join("/", Plugin.Config.Ores)));
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
