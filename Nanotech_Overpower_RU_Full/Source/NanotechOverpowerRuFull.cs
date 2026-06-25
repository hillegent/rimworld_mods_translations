using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace NanotechOverpowerRuFull
{
    [StaticConstructorOnStartup]
    public static class NanotechOverpowerRuFullInit
    {
        static NanotechOverpowerRuFullInit()
        {
            HardcodedStringPatch.LoadReplacements();
            var harmony = new Harmony("artas48.nanotechoverpower.rufull.hardcoded");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch]
    public static class NanotechSettingsTabsPatch
    {
        private static readonly string[] TranslatedTabs =
        {
            "Нанохранилище",
            "Бустер механоидов",
            "Обелиск (осн.)",
            "Обелиск (цели)",
            "Обелиск (доп.)",
            "Прочее"
        };

        private static FieldInfo TabsField;

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = AccessTools.TypeByName("Nanotech.NanotechMod");
            MethodInfo method = type == null ? null : AccessTools.Method(type, "DoSettingsWindowContents");
            TabsField = type == null ? null : AccessTools.Field(type, "tabs");
            if (method != null && TabsField != null)
            {
                yield return method;
            }
        }

        public static void Prefix(object __instance)
        {
            if (__instance == null || TabsField == null)
            {
                return;
            }

            string[] tabs = TabsField.GetValue(__instance) as string[];
            if (tabs == null)
            {
                return;
            }

            int count = Math.Min(tabs.Length, TranslatedTabs.Length);
            for (int i = 0; i < count; i++)
            {
                tabs[i] = TranslatedTabs[i];
            }
        }
    }

    [HarmonyPatch]
    public static class HardcodedStringPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>();
        private const string TargetAssemblyName = "Nanotech";
        private const string TargetTypePrefix = "Nanotech.";

        public static void LoadReplacements()
        {
            try
            {
                var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(m => string.Equals(m.PackageId, "artas48.nanotechoverpower.rufull", StringComparison.OrdinalIgnoreCase));
                if (mod == null) return;
                var path = Path.Combine(mod.RootDir, "Languages", "Russian", "Strings", "HardcodedStrings.tsv");
                if (!File.Exists(path)) return;
                foreach (var line in File.ReadAllLines(path))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    var parts = line.Split('\t');
                    if (parts.Length < 2) continue;
                    var original = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                    var translated = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
                    if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(translated) && original != translated)
                    {
                        Replacements[original] = translated;
                    }
                }
                Log.Message("[Nanotech Overpower RU] Loaded " + Replacements.Count + " hardcoded string replacements.");
            }
            catch (Exception ex)
            {
                Log.Warning("[Nanotech Overpower RU] Failed to load hardcoded string replacements: " + ex);
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Assembly targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, TargetAssemblyName, StringComparison.OrdinalIgnoreCase));
            if (targetAssembly == null) yield break;
            Type[] types;
            try { types = targetAssembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }

            foreach (var type in types)
            {
                if (type == null || type.FullName == null || !type.FullName.StartsWith(TargetTypePrefix, StringComparison.Ordinal)) continue;
                const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                foreach (var method in type.GetMethods(flags))
                {
                    if (!method.IsAbstract && !method.ContainsGenericParameters && method.GetMethodBody() != null) yield return method;
                }
                foreach (var ctor in type.GetConstructors(flags))
                {
                    if (!ctor.ContainsGenericParameters && ctor.GetMethodBody() != null) yield return ctor;
                }
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                var original = instruction.operand as string;
                string translated;
                if (instruction.opcode == OpCodes.Ldstr && original != null && Replacements.TryGetValue(original, out translated))
                {
                    instruction.operand = translated;
                }
                yield return instruction;
            }
        }
    }
}
