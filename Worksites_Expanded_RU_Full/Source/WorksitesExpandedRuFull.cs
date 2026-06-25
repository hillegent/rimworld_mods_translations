using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace WorksitesExpandedRuFull
{
    [StaticConstructorOnStartup]
    public static class WorksitesExpandedRuFullInit
    {
        static WorksitesExpandedRuFullInit()
        {
            HardcodedStringPatch.LoadReplacements();
            var harmony = new Harmony("artas48.worksitesexpanded.rufull.hardcoded");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch]
    public static class HardcodedStringPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>();

        public static void LoadReplacements()
        {
            try
            {
                var mod = LoadedModManager.RunningModsListForReading.FirstOrDefault(m => string.Equals(m.PackageId, "artas48.worksitesexpanded.rufull", StringComparison.OrdinalIgnoreCase));
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
                    if (!string.IsNullOrEmpty(original) && !string.IsNullOrEmpty(translated) && original != translated) Replacements[original] = translated;
                }
                Log.Message("[Worksites Expanded RU] Loaded " + Replacements.Count + " hardcoded string replacements.");
            }
            catch (Exception ex)
            {
                Log.Warning("[Worksites Expanded RU] Failed to load hardcoded string replacements: " + ex);
            }
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Assembly targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => string.Equals(a.GetName().Name, "MiningOutpost", StringComparison.OrdinalIgnoreCase));
            if (targetAssembly == null) yield break;
            Type[] types;
            try { types = targetAssembly.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
            foreach (var type in types)
            {
                if (type == null) continue;
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
                if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string original && Replacements.TryGetValue(original, out var translated)) instruction.operand = translated;
                yield return instruction;
            }
        }
    }
}
