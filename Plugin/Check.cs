using NLog;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game;
using VRageMath;

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
            var oresList = string.Join("/", ores);

            foreach (var zone in Plugin.Config.Zones)
            {
                if (zone.Contains(p))
                {
                    if (zone.AllOres)
                    {
                        if (Plugin.Config.Debug) Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): contained in zone (X:{zone.Center.X} Y:{zone.Center.Y} Z:{zone.Center.Z} max:{zone.MaxRadius} min:{zone.MinRadius} h:{zone.Height}), all ores: keep");
                        return true;
                    }
                    foreach (var ore in ores)
                    {
                        if (!zone.Ores.Contains(ore))
                        {
                            if (Plugin.Config.Debug) Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): contained in zone (X:{zone.Center.X} Y:{zone.Center.Y} Z:{zone.Center.Z} max:{zone.MaxRadius} min:{zone.MinRadius} h:{zone.Height}), does not contain {ore}: remove");
                            return false;
                        }
                    }
                    if (Plugin.Config.Debug)
                    {
                        var zoneOresList = string.Join("/", zone.Ores);
                        Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): contained in zone (X:{zone.Center.X} Y:{zone.Center.Y} Z:{zone.Center.Z} max:{zone.MaxRadius} min:{zone.MinRadius} h:{zone.Height}), contains desired ores {zoneOresList}: keep");
                    }
                    return true;
                }
            }

            if (Plugin.Config.AllOres)
            {
                if (Plugin.Config.Debug) Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): in space, all ores: keep");
                return true;
            }
            foreach (var ore in ores)
            {
                if (!Plugin.Config.Ores.Contains(ore))
                {
                    if (Plugin.Config.Debug) Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): in space, does not contain {ore}: remove");
                    return false;
                }
            }
            if (Plugin.Config.Debug)
            {
                var pluginConfigOresList = string.Join("/", Plugin.Config.Ores);
                Log.Info($"Asteroid at X:{p.X} Y:{p.Y} Z:{p.Z} ({oresList}): in space, contains desired ores {pluginConfigOresList}: keep");
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
