using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;

namespace PlasmaShieldImplantRuFull
{
    [StaticConstructorOnStartup]
    public static class Init
    {
        static Init()
        {
            new Harmony("artas48.plasmashieldimplant.rufull").PatchAll(Assembly.GetExecutingAssembly());
            PlasmaShieldImplantDefFallback.Apply();
        }
    }

    public static class PlasmaShieldImplantDefFallback
    {
        public static void Apply()
        {
            ThingDef thingDef = DefDatabase<ThingDef>.GetNamedSilentFail("PlasmaShieldImplant");
            if (thingDef != null)
            {
                thingDef.label = "имплант плазменного щита";
                thingDef.description = "Сердечный имплант, создающий защитное поле вокруг носителя. Поле блокирует как дистанционные атаки, так и атаки в ближнем бою, не мешая использовать снаряжение. Заряд ЭМИ мгновенно разрушит поле.";
            }

            HediffDef hediffDef = DefDatabase<HediffDef>.GetNamedSilentFail("PlasmaShieldImplant");
            if (hediffDef != null)
            {
                hediffDef.label = "имплант плазменного щита";
                hediffDef.labelNoun = "имплант плазменного щита";
                hediffDef.description = "Имплантированный генератор плазменного щита.";
            }

            RecipeDef recipeDef = DefDatabase<RecipeDef>.GetNamedSilentFail("InstallPlasmaShieldImplant");
            if (recipeDef != null)
            {
                recipeDef.label = "установить имплант плазменного щита";
                recipeDef.description = "Установить имплант плазменного щита.";
                recipeDef.jobString = "Устанавливает имплант плазменного щита.";
            }
        }
    }

    [HarmonyPatch]
    public static class PlasmaShieldImplantLabelPatch
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("PlasmaShieldImplant.HediffPlasmaShield_Implant");
            MethodInfo getter = type == null ? null : AccessTools.PropertyGetter(type, "Label");
            if (getter != null)
            {
                yield return getter;
            }
        }

        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
            {
                return;
            }

            __result = ReplaceIgnoreCase(__result, "a plasma shield implant", "имплант плазменного щита");
            __result = ReplaceIgnoreCase(__result, "plasma shield implant", "имплант плазменного щита");
        }

        private static string ReplaceIgnoreCase(string source, string oldValue, string newValue)
        {
            int index = source.IndexOf(oldValue, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return source;
            }

            return source.Substring(0, index) + newValue + source.Substring(index + oldValue.Length);
        }
    }

    [HarmonyPatch]
    public static class HardcodedStringsPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "Activate ", "Активировать: " },
            { "Error - Failed to find HediffCompPlasmaShield", "Ошибка: не найден HediffCompPlasmaShield" }
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(candidate => candidate.GetName().Name == "PlasmaShieldImplant");

            if (assembly == null)
            {
                Log.Warning("[Plasma Shield Implant RU] Не найдена сборка PlasmaShieldImplant для перевода хардкода.");
                yield break;
            }

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(type => type != null).ToArray();
            }

            foreach (Type type in types)
            {
                if (type.FullName == null || !type.FullName.StartsWith("PlasmaShieldImplant.", StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters)
                    {
                        continue;
                    }

                    MethodBody body = null;
                    try
                    {
                        body = method.GetMethodBody();
                    }
                    catch
                    {
                    }

                    if (body != null)
                    {
                        yield return method;
                    }
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                string text = instruction.operand as string;
                string translated;
                if (instruction.opcode == OpCodes.Ldstr && text != null && Replacements.TryGetValue(text, out translated))
                {
                    instruction.operand = translated;
                }

                yield return instruction;
            }
        }
    }
}
