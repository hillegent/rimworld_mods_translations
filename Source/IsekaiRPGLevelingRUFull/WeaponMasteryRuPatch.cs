using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IsekaiLeveling.Forge;
using IsekaiLeveling.UI;
using Verse;

namespace IsekaiRPGLevelingRUFull
{
    [StaticConstructorOnStartup]
    public static class ModBootstrap
    {
        static ModBootstrap()
        {
            try
            {
                new Harmony("artas48.isekai_rpg_leveling_ru_full").PatchAll(Assembly.GetExecutingAssembly());
                HardcodedStringTools.LocalizeSettingsTabs();
            }
            catch (Exception ex)
            {
                Log.Error("[ISEKAI RPG LEVELING RU] Failed to apply Russian translation patches: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(WeaponMasteryTracker), nameof(WeaponMasteryTracker.GetTierLabel))]
    public static class WeaponMasteryTracker_GetTierLabel_Patch
    {
        public static bool Prefix(MasteryTier tier, ref string __result)
        {
            switch (tier)
            {
                case MasteryTier.Novice:
                    __result = "Новичок";
                    return false;
                case MasteryTier.Apprentice:
                    __result = "Ученик";
                    return false;
                case MasteryTier.Skilled:
                    __result = "Опытный";
                    return false;
                case MasteryTier.Adept:
                    __result = "Адепт";
                    return false;
                case MasteryTier.Expert:
                    __result = "Эксперт";
                    return false;
                case MasteryTier.Master:
                    __result = "Мастер";
                    return false;
                case MasteryTier.Grandmaster:
                    __result = "Грандмастер";
                    return false;
                default:
                    __result = "Неизвестно";
                    return false;
            }
        }
    }

    [HarmonyPatch]
    public static class WindowMastery_Strings_Patch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "No pawn selected.", "Пешка не выбрана." },
            { "No Isekai data found.", "Данные Isekai не найдены." },
            { "Weapon Mastery", "Владение оружием" },
            { "Weapon Mastery ( ", "Владение оружием ( " },
            { "Equipped: ", "В руках: " },
            { "No weapon equipped", "Оружие не экипировано" },

            { "DEV MODE", "РЕЖИМ РАЗРАБ." },
            { "Max Equipped", "Макс. тек." },
            { "Reset Equipped", "Сброс тек." },
            { "Max All", "Макс. всё" },
            { "Reset All", "Сброс всё" },
            { "+1 Level", "+1 ранг" },

            { "Mastered Weapons (", "Освоенное оружие (" },
            { "Unmastered Weapons (", "Неосвоенное оружие (" },
            { " [Equipped]", " [в руках]" },
            { " XP (MAX)", " опыта (МАКС.)" },
            { " XP", " опыта" },

            { "No mastery yet. Equip and fight to gain XP.\n\nType: ", "Мастерства пока нет. Возьмите оружие в руки и сражайтесь, чтобы получать опыт.\n\nТип: " },
            { "Ranged", "Дальнее" },
            { "Melee", "Ближнее" },

            { "Type: ", "Тип: " },
            { "\nType: ", "\nТип: " },
            { "Tier: ", "Ранг: " },
            { "\nTier: ", "\nРанг: " },
            { "XP: ", "Опыт: " },
            { "\nXP: ", "\nОпыт: " },
            { "Current Bonuses:\n", "Текущие бонусы:\n" },
            { "\n\nCurrent Bonuses:\n", "\n\nТекущие бонусы:\n" },
            { "  Hit Chance: +", "  Шанс попадания: +" },
            { "  Attack Speed: +", "  Скорость атаки: +" },
            { "  Damage: +", "  Урон: +" },
            { "  None yet (reach Apprentice tier)\n", "  Пока нет (достигните ранга «Ученик»)\n" },
            { "\nNext tier at: ", "\nСледующий ранг на: " },
            { "Next tier at: ", "Следующий ранг на: " },
            { " XP (", " опыта (" },
            { " XP needed)", " опыта нужно)" },
            { "Maximum tier reached!", "Достигнут максимальный ранг!" },
            { "\nMaximum tier reached!", "\nДостигнут максимальный ранг!" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type type = typeof(Window_Mastery);
            foreach (string name in new[]
            {
                "DoWindowContents",
                "DrawDevButtons",
                "DrawMasteredRow",
                "DrawUnmasteredRow",
                "DrawMasteryTooltip",
            })
            {
                MethodInfo method = AccessTools.Method(type, name);
                if (method != null)
                    yield return method;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string text &&
                    Replacements.TryGetValue(text, out string replacement))
                {
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }
    }
}
