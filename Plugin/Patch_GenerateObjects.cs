using HarmonyLib;
using NLog;
using Sandbox.Game.World.Generator;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace StalkR.AsteroidOres
{
    [HarmonyPatch(typeof(MyProceduralAsteroidCellGenerator), nameof(MyProceduralAsteroidCellGenerator.GenerateObjects))]
    internal class Patch_GenerateObjects
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            var code = new List<CodeInstruction>(instructions);
            int found = -1;
            var continueLabel = il.DefineLabel();

            for (var i = 0; i < code.Count; i++)
            {
                // MyStorageBase myStorageBase = new MyOctreeStorage(
                //     MyCompositeShapeProvider.CreateAsteroidShape(myObjectSeed.Params.Seed, myObjectSeed.Size,
                //                                                  this.m_data.UseGeneratorSeed ? myObjectSeed.Params.GeneratorSeed : 0, null),
                //     MyProceduralAsteroidCellGenerator.GetAsteroidVoxelSize((double)myObjectSeed.Size));
                // 152	01D1	ldloc.1 // myObjectSeed
                // 153	01D2	ldfld   class [VRage.Game] VRage.Game.MyObjectSeedParams Sandbox.Game.World.Generator.MyObjectSeed::Params
                // 154	01D7	ldfld int32 [VRage.Game] VRage.Game.MyObjectSeedParams::Seed
                // 155	01DC	ldloc.1 // myObjectSeed
                // 156	01DD	callvirt    instance float32 Sandbox.Game.World.Generator.MyObjectSeed::get_Size()
                // 157	01E2	ldarg.0 // this
                // 158	01E3	ldfld   class Sandbox.Definitions.MyAsteroidGeneratorDefinition Sandbox.Game.World.Generator.MyProceduralAsteroidCellGenerator::m_data
                // 159	01E8	ldfld   bool Sandbox.Definitions.MyAsteroidGeneratorDefinition::UseGeneratorSeed
                // 160	01ED	brtrue.s    163 (01F2) ldloc.1 
                // 161	01EF	ldc.i4.0 // 0
                // 162	01F0	br.s	166 (01FD) ldloca.s V_14(14)
                // 163	01F2	ldloc.1 // myObjectSeed
                // 164	01F3	ldfld class [VRage.Game] VRage.Game.MyObjectSeedParams Sandbox.Game.World.Generator.MyObjectSeed::Params
                // 165	01F8	ldfld int32 [VRage.Game] VRage.Game.MyObjectSeedParams::GeneratorSeed
                // 166	01FD	ldloca.s V_14 (14)
                // 167	01FF	initobj valuetype[netstandard] System.Nullable`1<int32>
                // 168	0205	ldloc.s V_14 (14)
                // 169	0207	call class Sandbox.Game.World.Generator.MyCompositeShapeProvider Sandbox.Game.World.Generator.MyCompositeShapeProvider::CreateAsteroidShape(int32, float32, int32, valuetype[netstandard] System.Nullable`1<int32>)
                // => inject here
                // 170	020C	ldloc.1 // myObjectSeed
                // 171	020D	callvirt instance float32 Sandbox.Game.World.Generator.MyObjectSeed::get_Size()
                // 172	0212	conv.r8 // (double)
                // 173	0213	call valuetype[VRage.Math]VRageMath.Vector3I Sandbox.Game.World.Generator.MyProceduralAsteroidCellGenerator::GetAsteroidVoxelSize(float64)
                // 174	0218	newobj instance void Sandbox.Engine.Voxels.MyOctreeStorage::.ctor(class [VRage.Game] VRage.Voxels.IMyStorageDataProvider, valuetype[VRage.Math] VRageMath.Vector3I)
                // 175	021D	stloc.s V_12(12)
                var j = i - 1;
                if (code[++j].opcode == OpCodes.Ldloc_1 &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Ldloc_1 &&
                    code[++j].opcode == OpCodes.Callvirt &&
                    code[++j].opcode == OpCodes.Ldarg_0 &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Brtrue_S &&
                    code[++j].opcode == OpCodes.Ldc_I4_0 &&
                    code[++j].opcode == OpCodes.Br_S &&
                    code[++j].opcode == OpCodes.Ldloc_1 &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Ldfld &&
                    code[++j].opcode == OpCodes.Ldloca_S &&
                    code[++j].opcode == OpCodes.Initobj &&
                    code[++j].opcode == OpCodes.Ldloc_S &&
                    code[++j].opcode == OpCodes.Call && code[j].operand.ToString() == "Sandbox.Game.World.Generator.MyCompositeShapeProvider CreateAsteroidShape(Int32, Single, Int32, System.Nullable`1[System.Int32])" &&
                    code[++j].opcode == OpCodes.Ldloc_1 &&
                    code[++j].opcode == OpCodes.Callvirt &&
                    code[++j].opcode == OpCodes.Conv_R8 &&
                    code[++j].opcode == OpCodes.Call && code[j].operand.ToString() == "VRageMath.Vector3I GetAsteroidVoxelSize(Double)" &&
                    code[++j].opcode == OpCodes.Newobj &&
                    code[++j].opcode == OpCodes.Stloc_S &&
                    code[i - 152 + 287].opcode == OpCodes.Leave_S)
                {
                    // intercept after: MyCompositeShapeProvider shapeProvider = CreateAsteroidShape()
                    found = i + 170 - 152;
                    // label where `if (flag) continue` goes, so we can skip asteroid generation
                    code[i - 152 + 287].labels.Add(continueLabel);
                }
            }
            if (found == -1)
            {
                Log.Error("injection not found");
                return code;
            }

            var shapeProvider = il.DeclareLocal(AccessTools.TypeByName("Sandbox.Game.World.Generator.MyCompositeShapeProvider"));
            var inj = new List<CodeInstruction>();
            inj.Add(new CodeInstruction(OpCodes.Stloc_S, shapeProvider)); // store: MyCompositeShapeProvider shapeProvider = CreateAsteroidShape()
            inj.Add(new CodeInstruction(OpCodes.Ldloc_1)); // myObjectSeed
            inj.Add(new CodeInstruction(OpCodes.Ldloc_S, shapeProvider));
            inj.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Materials), nameof(Materials.ShouldGenerate))));
            inj.Add(new CodeInstruction(OpCodes.Brfalse, continueLabel));
            inj.Add(new CodeInstruction(OpCodes.Ldloc_S, shapeProvider));

            code.InsertRange(found, inj);
            Log.Info("injection successful");
            return code;
        }
    }
}