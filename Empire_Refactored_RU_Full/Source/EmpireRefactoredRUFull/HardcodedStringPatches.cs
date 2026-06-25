using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace EmpireRefactoredRUFull
{
    [StaticConstructorOnStartup]
    public static class Bootstrap
    {
        static Bootstrap()
        {
            new Harmony("artas48.empirerefactored.rufull").PatchAll();
        }
    }

    [HarmonyPatch]
    public static class HardcodedStringPatches
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "Major only", "Только крупные" },
            { "Minor and above", "Малые и выше" },
            { "Hotfix and above", "Хотфиксы и выше" },
            { "Patch and above", "Патчи и выше" },
            { "Never", "Никогда" },
            { "'s loadout has been assigned to ", " назначен поселению " },
            { "Clear", "Очистить" },
            { "Current:", "Текущий:" },
            { "Old:", "Старый:" },
            { "Brightness", "Яркость" },
            { " - Cost: ", " - Стоимость: " },
            { " ea. / $", " шт. / $" },
            { "None", "Нет" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            string[] typeNames =
            {
                "FactionColonies.FactionColonies",
                "FactionColonies.MilitaryCustomizationUtil",
                "FactionColonies.FactionCustomizeWindowFC",
                "FactionColonies.FCWindow_ColorPicker",
                "FactionColonies.FCWindow_RacePicker",
                "FactionColonies.FireSupportWindow",
                "FactionColonies.SettlementWindowFC",
            };

            foreach (string typeName in typeNames)
            {
                System.Type type = AccessTools.TypeByName(typeName);
                if (type == null)
                {
                    continue;
                }

                foreach (MethodBase method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    yield return method;
                }

                foreach (ConstructorInfo constructor in AccessTools.GetDeclaredConstructors(type))
                {
                    if (!constructor.ContainsGenericParameters)
                    {
                        yield return constructor;
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string value &&
                    Replacements.TryGetValue(value, out string replacement))
                {
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }
    }
}
