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
            { "Tax Days", "Дни налогов" },
            { "Tithe Mod", "Модификатор десятины" },
            { "Worker Cost", "Стоимость рабочего" },
            { "'s loadout has been assigned to ", " назначен поселению " },
            { "Clear", "Очистить" },
            { "Current:", "Текущий:" },
            { "Old:", "Старый:" },
            { "Brightness", "Яркость" },
            { " - Cost: ", " - Стоимость: " },
            { " ea. / $", " шт. / $" },
            { "None", "Нет" },
            { "Select A Unit", "Выберите юнита" },
            { "New Unit ", "Новый юнит " },
            { "New Unit {0}", "Новый юнит {0}" },
            { "New Squad ", "Новый отряд " },
            { "New Squad {0}", "Новый отряд {0}" },
            { "New Fire Support ", "Новая огневая поддержка " },
            { "Create Squads", "Создание отрядов" },
            { "Create Units", "Создание юнитов" },
            { "This template references defs from unloaded mods: ", "Этот шаблон ссылается на defs из отключенных модов: " },
            { ".\nImporting will substitute defaults for missing items.", ".\nПри импорте отсутствующие элементы будут заменены стандартными." },
            { ".\nImporting will drop missing projectiles.", ".\nПри импорте отсутствующие снаряды будут удалены." },
            { "Lv ", "Ур. " },
            { "  •  Mil ", "  •  Воен. " },
            { "Mil ", "Воен. " },
            { " • Def ", " • Обор. " },
            { "BW ", "ПП " },
            { " entries  •  ", " записей  •  " },
            { " base)", " баз.)" },
            { " d", " дн." },
            { " hours", " ч." },
            { " kg  $", " кг  $" },
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
                "FactionColonies.CodexProvider_DifficultySettings",
                "FactionColonies.MilUnitFC",
                "FactionColonies.Dialog_ManageSquadExportsFC",
                "FactionColonies.Dialog_ManageUnitExportsFC",
                "FactionColonies.Dialog_ManageFireSupportExportsFC",
                "FactionColonies.DesignUnitsWindow",
                "FactionColonies.DesignSquadsWindow",
                "FactionColonies.MainTabWindow_Colony",
                "FactionColonies.FCWindow_MechPicker",
                "FactionColonies.PatchNotesDisplayWindow",
                "FactionColonies.CodexTab_Settlements",
                "FactionColonies.Dialog_SquadPicker",
                "FactionColonies.InventoryListWidget",
                "FactionColonies.util.TicksExtensions",
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
