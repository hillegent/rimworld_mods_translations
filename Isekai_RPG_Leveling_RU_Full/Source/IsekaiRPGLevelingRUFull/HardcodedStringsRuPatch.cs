using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using IsekaiLeveling;
using IsekaiLeveling.Compatibility;
using IsekaiLeveling.MobRanking;
using IsekaiLeveling.UI;

namespace IsekaiRPGLevelingRUFull
{
    public static class HardcodedStringTools
    {
        public static void LocalizeSettingsTabs()
        {
            FieldInfo field = AccessTools.Field(typeof(IsekaiMod), "tabNames");
            if (field?.GetValue(null) is string[] tabs && tabs.Length >= 9)
            {
                tabs[0] = "Общее";
                tabs[1] = "Задания";
                tabs[2] = "Эффекты";
            }
        }

        public static IEnumerable<CodeInstruction> ReplaceLdstr(IEnumerable<CodeInstruction> instructions, Dictionary<string, string> replacements)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string text &&
                    replacements.TryGetValue(text, out string replacement))
                {
                    instruction.operand = replacement;
                }

                yield return instruction;
            }
        }

        public static IEnumerable<MethodBase> NamedMethods(Type type, params string[] names)
        {
            foreach (string name in names)
            {
                foreach (MethodInfo method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.Name == name)
                        yield return method;
                }
            }
        }
    }

    [HarmonyPatch]
    public static class GeneralHardcodedStrings_RuPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            // Settings window
            { "x  <color=#888888>(Default: 3.0x)</color>", "x  <color=#888888>(по умолчанию: 3.0x)</color>" },
            { "  <color=#888888>(Default: 1)</color>", "  <color=#888888>(по умолчанию: 1)</color>" },
            { "  <color=#888888>(Default: 9999)</color>", "  <color=#888888>(по умолчанию: 9999)</color>" },
            { "  <color=#888888>(Default: Random)</color>", "  <color=#888888>(по умолчанию: случайно)</color>" },
            { "  <color=#888888>(Default: 50%)</color>", "  <color=#888888>(по умолчанию: 50%)</color>" },
            { "x  <color=#888888>(Default: 1.0x)</color>", "x  <color=#888888>(по умолчанию: 1.0x)</color>" },
            { "x  <color=#888888>(Default: 1.00x)</color>", "x  <color=#888888>(по умолчанию: 1.00x)</color>" },
            { "  <color=#888888>(Default: S)</color>", "  <color=#888888>(по умолчанию: S)</color>" },
            { "  <color=#888888>(Default: 1.0 day)</color>", "  <color=#888888>(по умолчанию: 1.0 день)</color>" },
            { "  <color=#888888>(Default: All)</color>", "  <color=#888888>(по умолчанию: все)</color>" },
            { "  <color=#888888>(Default: 25%)</color>", "  <color=#888888>(по умолчанию: 25%)</color>" },
            { "x  <color=#888888>(Default: 1.2x)</color>", "x  <color=#888888>(по умолчанию: 1.2x)</color>" },
            { "  <color=#888888>(Default: ComponentIndustrial)</color>", "  <color=#888888>(по умолчанию: ComponentIndustrial)</color>" },
            { "<b>Forge System</b>", "<b>Система кузницы</b>" },
            { "Enable Forge System", "Включить систему кузницы" },
            { "Enable weapon/armor refinement, mastery, and rune socketing.", "Включает улучшение оружия и брони, мастерство оружия и установку рун." },
            { "Refinement Success Multiplier: ", "Множитель шанса улучшения: " },
            { "Enable Weapon Mastery", "Включить мастерство оружия" },
            { "Enable per-weapon-type mastery XP and stat bonuses from combat use.", "Включает опыт мастерства и боевые бонусы для каждого типа оружия." },
            { "Mastery XP Multiplier: ", "Множитель опыта мастерства: " },

            // Level-up notices and inspect strings
            { "{0} Leveled Up!", "{0} повышает уровень!" },
            { "{0} has reached level {1}!\n\n", "{0} достигает уровня {1}!\n\n" },
            { "They have {0} stat point(s) to spend.", "Доступно очков характеристик: {0}." },
            { " (Lv", " (ур." },
            { "Rank: {0} - Level {1}", "Ранг: {0} - уровень {1}" },
            { "Rank: {0}{1} - {2}{3}", "Ранг: {0}{1} - {2}{3}" },
            { " (Elite)", " (элита)" },
            { "HP: ", "ЗД: " },
            { "DMG: ", "УРН: " },
            { "SPD: ", "СКР: " },
            { "ARM: ", "БРН: " },
            { "Isekai DEX ({0}): {1}{2:F0}%", "Isekai ЛОВ ({0}): {1}{2:F0}%" },
            { "Isekai DEX ({0}): {1:F0}%", "Isekai ЛОВ ({0}): {1:F0}%" },
            { "Isekai STR ({0}): {1}{2:F0}%", "Isekai СИЛ ({0}): {1}{2:F0}%" },
            { "Isekai STR ({0}): {1:F0}%", "Isekai СИЛ ({0}): {1:F0}%" },
            { "Isekai VIT ({0}): {1}{2:F0}% suppressability", "Isekai ЖВС ({0}): {1}{2:F0}% к подавляемости" },
            { "Isekai VIT ({0}): {1}{2:F0}%", "Isekai ЖВС ({0}): {1}{2:F0}%" },
            { "Isekai INT ({0}): {1}{2:F0}%", "Isekai ИНТ ({0}): {1}{2:F0}%" },
            { "Isekai WIS ({0}): {1}{2:F0}%", "Isekai МДР ({0}): {1}{2:F0}%" },
            { "Isekai WIS ({0}): {1:+0;-0}%", "Isekai МДР ({0}): {1:+0;-0}%" },
            { "Isekai CHA ({0}): {1}{2:F0}%", "Isekai ХАР ({0}): {1}{2:F0}%" },
            { "Isekai DEX ({comp.stats.dexterity}): {sign}{percent:F0}%", "Isekai ЛОВ ({comp.stats.dexterity}): {sign}{percent:F0}%" },
            { "Isekai DEX ({comp.stats.dexterity}): {percent:F0}%", "Isekai ЛОВ ({comp.stats.dexterity}): {percent:F0}%" },
            { "Isekai STR ({comp.stats.strength}): {sign}{percent:F0}%", "Isekai СИЛ ({comp.stats.strength}): {sign}{percent:F0}%" },
            { "Isekai STR ({comp.stats.strength}): {percent:F0}%", "Isekai СИЛ ({comp.stats.strength}): {percent:F0}%" },
            { "Isekai VIT ({comp.stats.vitality}): {sign}{percent:F0}% suppressability", "Isekai ЖВС ({comp.stats.vitality}): {sign}{percent:F0}% к подавляемости" },
            { "Isekai VIT ({comp.stats.vitality}): {sign}{percent:F0}%", "Isekai ЖВС ({comp.stats.vitality}): {sign}{percent:F0}%" },
            { "Isekai INT ({comp.stats.intelligence}): {sign}{percent:F0}%", "Isekai ИНТ ({comp.stats.intelligence}): {sign}{percent:F0}%" },
            { "Isekai WIS ({comp.stats.wisdom}): {sign}{percent:F0}%", "Isekai МДР ({comp.stats.wisdom}): {sign}{percent:F0}%" },
            { "Isekai WIS ({comp.stats.wisdom}): {pct:+0;-0}%", "Isekai МДР ({comp.stats.wisdom}): {pct:+0;-0}%" },
            { "Isekai CHA ({comp.stats.charisma}): {sign}{percent:F0}%", "Isekai ХАР ({comp.stats.charisma}): {sign}{percent:F0}%" },

            // Stat descriptions
            { "Increases melee damage and carry capacity.", "Повышает урон в ближнем бою и грузоподъёмность." },
            { "Improves movement speed, melee dodge, shooting accuracy, melee hit chance, and aiming speed.", "Улучшает скорость передвижения, уклонение в ближнем бою, точность стрельбы, шанс попадания в ближнем бою и скорость прицеливания." },
            { "Boosts injury healing, toxic resistance, damage reduction, natural armor, pain shock threshold, and rest efficiency.", "Усиливает заживление ран, сопротивление токсинам, снижение входящего урона, естественную броню, порог болевого шока и эффективность отдыха." },
            { "Enhances work speed, research speed, learning rate, and hacking speed. Affects psychic sensitivity with WIS.", "Повышает скорость работы, исследований, обучения и взлома. Вместе с МДР влияет на психическую чувствительность." },
            { "Improves mental stability, meditation focus, neural heat recovery, and psyfocus efficiency. Affects psychic sensitivity with INT and neural heat limit with VIT.", "Улучшает психическую устойчивость, фокус медитации, восстановление нейронагрева и эффективность психофокуса. Вместе с ИНТ влияет на психическую чувствительность, а вместе с ЖВС - на предел нейронагрева." },
            { "Improves social impact, negotiation ability, trade prices, animal taming, and arrest success.", "Улучшает социальное влияние, переговоры, торговые цены, приручение животных и шанс ареста." },
            { "Unknown stat.", "Неизвестная характеристика." },
            { "Unknown", "Неизвестно" },

            // Character Editor integration
            { "Isekai Leveling Stats", "Характеристики Isekai Leveling" },
            { "Isekai Stats - {0}", "Характеристики Isekai - {0}" },
            { "No pawn selected", "Пешка не выбрана" },
            { "Level: ", "Уровень: " },
            { "Level: {0}", "Уровень: {0}" },
            { "Available Points: {0}", "Доступно очков: {0}" },
            { "STR (Strength)", "СИЛ (сила)" },
            { "DEX (Dexterity)", "ЛОВ (ловкость)" },
            { "VIT (Vitality)", "ЖВС (живучесть)" },
            { "INT (Intelligence)", "ИНТ (интеллект)" },
            { "WIS (Wisdom)", "МДР (мудрость)" },
            { "CHA (Charisma)", "ХАР (харизма)" },
            { "Reset All Stats", "Сбросить все характеристики" },
            { "Auto-Distribute Points", "Автораспределить очки" },

            // Item use effects
            { "A Hero Has Been Summoned!", "Герой призван!" },
            { "Summoned Hero", "призванный герой" },
            { "A Hero Arrives — {0}!", "Прибытие героя - {0}!" },
            { "A summoning circle has torn open a rift between worlds!\n\n", "Круг призыва разорвал завесу между мирами!\n\n" },
            { "{0} has been pulled from another dimension and joins your colony as a {1}.\n\n", "{0} был вытянут из другого измерения и присоединяется к вашей колонии как {1}.\n\n" },
            { "• x3 XP multiplier\n", "- множитель опыта x3\n" },
            { "• +1 bonus stat point per level\n", "- +1 бонусное очко характеристик за уровень\n" },
            { "• Highly resistant to mental breaks\n", "- высокая устойчивость к психическим срывам\n" },
            { "• +25% social impact", "- +25% к социальному влиянию" },
            { "Must be used on a map.", "Можно использовать только на карте." },
            { "Awakened: {0}!", "Пробуждение: {0}!" },
            { "{0} — {1}!", "{0} - {1}!" },
            { "{0} has awakened as a {1}!\n\n", "{0} пробуждается как {1}!\n\n" },
            { "{0} cannot use this — they already have a conflicting trait.", "{0} не может это использовать: уже есть конфликтующая черта." },
            { "This pawn has no Isekai progression.", "У этой пешки нет прогрессии Isekai." },
            { "Already has this trait.", "Эта черта уже есть." },
            { "Conflicts with existing trait: {0}", "Конфликтует с имеющейся чертой: {0}" },
            { "+{0} Star Point", "+{0} звёздное очко" },
            { "s", "" },
            { " + Multiclass Unlock", " + открытие мультикласса" },
            { "Can only be absorbed by an Isekai adventurer.", "Может быть поглощено только искателем приключений Isekai." },

            // Quest/hunt runtime strings
            { "{0}-Rank Hunt: {1}", "Охота ранга {0}: {1}" },
            { "Bounty contract voided", "Контракт на цель аннулирован" },
            { "The bounty target", "Цель контракта" },
            { "{0} died in custody rather than in combat. ", "{0} погибает в плену, а не в бою. " },
            { "The contract pays only for combat kills — executing a captured prisoner does not qualify. ", "Контракт засчитывает только убийство в бою; казнь пленника не подходит. " },
            { "No XP, silver, or loot will be awarded.", "Опыт, серебро и добыча не будут выданы." },
            { "Pack Status: {0}/{1} remaining", "Состояние стаи: осталось {0}/{1}" },

            // Elite creature names
            { "Beast", "Зверь" },
            { "{0} the {1}", "{1} по прозвищу «{0}»" },
            { "Worldrender", "Разрушитель миров" },
            { "Skyeater", "Пожиратель неба" },
            { "Doomspeaker", "Вестник рока" },
            { "Voidmaw", "Пасть пустоты" },
            { "Ashking", "Пепельный король" },
            { "Bonecrusher", "Костолом" },
            { "Stormfang", "Грозовой клык" },
            { "Direhorn", "Жуткий рог" },
            { "Nightreaver", "Ночной потрошитель" },
            { "Ironhide", "Железная шкура" },
            { "Bloodmane", "Кровавая грива" },
            { "Frostclaw", "Морозный коготь" },
            { "Shadowstalker", "Теневой охотник" },
            { "Grimjaw", "Мрачная пасть" },
            { "Sharpfang", "Острый клык" },
            { "Wildhowl", "Дикий вой" },
            { "Brokentusk", "Сломанный бивень" },
            { "Scarred", "Шрамованный" },
            { "Cunning", "Хитрец" },
            { "Restless", "Неугомонный" },
            { "Old", "Старый" },
            { "Mean", "Злобный" },
            { "Stubborn", "Упрямец" },
            { "Wary", "Настороженный" },
            { "Limping", "Хромой" },
            { "Strange", "Странный" },
            { "Quiet", "Тихий" },
            { "Lone", "Одинокий" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(IsekaiMod),
                "DoSettingsWindowContents", "DrawGeneralSettings", "DrawQuestsSettings", "DrawEffectsSettings"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(IsekaiStatInfo),
                "GetStatDescription"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(IsekaiComponent),
                "LevelUp", "DevAddLevel", "CompInspectStringExtra"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(MobRankComponent),
                "LevelUp", "CompInspectStringExtra"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(MobRankUtility),
                "GenerateEliteName", "GetEliteAdjectivesForRank"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(CharacterEditorCompatibility),
                "DrawIsekaiStatsSection"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(Window_IsekaiEditor),
                "DoWindowContents", "DrawStatRow"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(CompUseEffect_SummonHero),
                "DoEffect", "CanBeUsedBy"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(CompUseEffect_IsekaiTrait),
                "DoEffect", "CanBeUsedBy"))
                yield return method;

            foreach (MethodBase method in HardcodedStringTools.NamedMethods(typeof(CompUseEffect_StarFragment),
                "DoEffect", "CanBeUsedBy"))
                yield return method;

            Type huntWorker = AccessTools.TypeByName("IsekaiLeveling.Quests.IncidentWorker_IsekaiHunt");
            if (huntWorker != null)
            {
                foreach (MethodBase method in HardcodedStringTools.NamedMethods(huntWorker,
                    "CreateHuntSite", "VoidBountyContract"))
                    yield return method;
            }

            Type worldHuntPart = AccessTools.TypeByName("IsekaiLeveling.Quests.QuestPart_IsekaiWorldHunt");
            if (worldHuntPart != null)
            {
                foreach (MethodBase method in HardcodedStringTools.NamedMethods(worldHuntPart,
                    "GetTargetDescription"))
                    yield return method;
            }

            foreach (string typeName in new[]
            {
                "IsekaiLeveling.Compatibility.StatPart_CE_AimingAccuracy",
                "IsekaiLeveling.Compatibility.StatPart_CE_AimingTime",
                "IsekaiLeveling.Compatibility.StatPart_CE_Recoil",
                "IsekaiLeveling.Compatibility.StatPart_CE_SuppressionResistance",
                "IsekaiLeveling.Compatibility.StatPart_CE_ReloadSpeed",
                "IsekaiLeveling.Compatibility.StatPart_CE_MeleePenetration",
                "IsekaiLeveling.Compatibility.StatPart_CE_MeleeParry",
                "IsekaiLeveling.Compatibility.StatPart_CE_MeleeCounterParry",
                "IsekaiLeveling.Compatibility.StatPart_CE_MeleeCrit",
                "IsekaiLeveling.Compatibility.StatPart_CE_MeleeDamage",
                "IsekaiLeveling.Compatibility.StatPart_CE_Toughness",
                "IsekaiLeveling.Compatibility.StatPart_CE_CarryBulk",
                "IsekaiLeveling.Compatibility.StatPart_CE_CarryWeight",
                "IsekaiLeveling.Compatibility.StatPart_IsekaiHygieneRate",
                "IsekaiLeveling.Compatibility.StatPart_IsekaiBladderRate",
                "IsekaiLeveling.Compatibility.StatPart_Hospitality_Recruitment",
                "IsekaiLeveling.Compatibility.StatPart_Hospitality_SocialImpact",
                "IsekaiLeveling.Compatibility.StatPart_IsekaiWeaponSwapSpeed",
                "IsekaiLeveling.Compatibility.StatPart_VPE_MeditationFocus",
                "IsekaiLeveling.Compatibility.StatPart_VPE_NeuralHeatRecovery",
                "IsekaiLeveling.Compatibility.StatPart_VPE_PsyfocusSensitivity",
            })
            {
                Type type = AccessTools.TypeByName(typeName);
                if (type == null) continue;
                foreach (MethodBase method in HardcodedStringTools.NamedMethods(type, "ExplanationPart"))
                    yield return method;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return HardcodedStringTools.ReplaceLdstr(instructions, Replacements);
        }
    }

    [HarmonyPatch(typeof(Window_SkillTree))]
    public static class WindowSkillTreeHardcodedStrings_RuPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "Class Selection", "Выбор класса" },
            { "Pawn Class : {0}", "Класс пешки: {0}" },
            { "— soon —", "- скоро -" },
            { "{0}  —  Lv.{1} [{2}]", "{0}  -  ур.{1} [{2}]" },
            { "Class Passive", "Пассивка класса" },
            { "Not unlocked — allocate nodes in Warlord's Path", "Не открыто - вложите очки в путь Воеводы" },
            { "Tier {0}/4 — Below {1} HP → up to +{2}", "Уровень {0}/4 - ниже {1} здоровья -> до +{2}" },
            { "ACTIVE — HP: {0}% → +{1}% Melee Damage", "АКТИВНО - ЗД: {0}% -> +{1}% к урону в ближнем бою" },
            { "Inactive — HP: {0}% (triggers below {1})", "Неактивно - ЗД: {0}% (срабатывает ниже {1})" },
            { "Not unlocked — allocate nodes in Archmage's Path", "Не открыто - вложите очки в путь Архимага" },
            { "Tier {0}/4 — Above {1} psyfocus → up to +{2}", "Уровень {0}/4 - выше {1} психофокуса -> до +{2}" },
            { "ACTIVE — Psyfocus: {0}% → +{1}% Psychic Sensitivity", "АКТИВНО - психофокус: {0}% -> +{1}% к психической чувствительности" },
            { "Inactive — Psyfocus: {0}% (triggers above {1})", "Неактивно - психофокус: {0}% (срабатывает выше {1})" },
            { "Not unlocked — allocate nodes in Sanctuary's Vow", "Не открыто - вложите очки в путь Обета святилища" },
            { "Tier {0}/4 — Cap {1} dmg absorbed → up to +{2} on release", "Уровень {0}/4 - предел {1} поглощённого урона -> до +{2} при высвобождении" },
            { "CHARGED: {0}/{1} dmg → +{2}% next strike", "ЗАРЯЖЕНО: {0}/{1} урона -> +{2}% к следующему удару" },
            { "No charge — take hits to build retribution, then strike", "Нет заряда - получайте удары, чтобы накопить возмездие, затем атакуйте" },
            { "Not unlocked — allocate nodes in Ascetic Path", "Не открыто - вложите очки в путь Аскета" },
            { "Tier {0}/4 — Build calm over {1} → up to +{2} Tend/Surgery", "Уровень {0}/4 - спокойствие копится за {1} -> до +{2} к лечению/операциям" },
            { "FULL CALM: +{0}% Tend/Surgery, +{1}% Research", "ПОЛНОЕ СПОКОЙСТВИЕ: +{0}% к лечению/операциям, +{1}% к исследованиям" },
            { "Building: +{0}% Tend/Surgery, +{1}% Research", "Копится: +{0}% к лечению/операциям, +{1}% к исследованиям" },
            { "Just hit — calm resets and begins building again", "Недавно получен удар - спокойствие сброшено и копится заново" },
            { "Not unlocked — allocate nodes in Hawkeye Path", "Не открыто - вложите очки в путь Соколиного глаза" },
            { "Tier {0}/4 — Max {1} stacks, +{2}/stack → up to +{3}", "Уровень {0}/4 - максимум зарядов: {1}, +{2}/заряд -> до +{3}" },
            { "LOCKED ON: {0}/{1} stacks → +{2}% Shooting Accuracy", "ЦЕЛЬ ЗАХВАЧЕНА: {0}/{1} зарядов -> +{2}% к точности стрельбы" },
            { "No target — land ranged hits to stack focus", "Нет цели - попадайте дальними атаками, чтобы копить фокус" },
            { "Not unlocked — allocate nodes in Centerpoint Path", "Не открыто - вложите очки в путь Центра равновесия" },
            { "Tier {0}/4 — Max {1} charges, +{2}/charge → up to +{3}", "Уровень {0}/4 - максимум зарядов: {1}, +{2}/заряд -> до +{3}" },
            { "CHARGED: {0}/{1} charges → +{2}% Melee Damage", "ЗАРЯЖЕНО: {0}/{1} зарядов -> +{2}% к урону в ближнем бою" },
            { "No charges — dodge melee attacks to store counters", "Нет зарядов - уклоняйтесь от ближних атак, чтобы копить контрудары" },
            { "Not unlocked — allocate nodes in Mastercraft Path", "Не открыто - вложите очки в путь Мастерского ремесла" },
            { "Tier {0}/4 — Crafting skill scales Work Speed, up to +{1}", "Уровень {0}/4 - навык ремесла усиливает скорость работы до +{1}" },
            { "ACTIVE: Crafting {0}/20 → +{1}% Work Speed", "АКТИВНО: ремесло {0}/20 -> +{1}% к скорости работы" },
            { "Inactive — Crafting skill at 0", "Неактивно - навык ремесла равен 0" },
            { "Not unlocked — allocate nodes in Rally Path", "Не открыто - вложите очки в путь Сплочения" },
            { "Tier {0}/4 — +{1}/colonist, cap {2} colonists", "Уровень {0}/4 - +{1}/колонист, предел {2} колонистов" },
            { "ACTIVE: {0} colonists → +{1}% Social Impact", "АКТИВНО: колонистов: {0} -> +{1}% к социальному влиянию" },
            { "No colonists on map — recruit allies to empower", "На карте нет колонистов - вербуйте союзников для усиления" },
            { "Not unlocked — allocate nodes in Unyielding Path", "Не открыто - вложите очки в Несгибаемый путь" },
            { "Tier {0}/4 — Low mood boosts Immunity & Rest, up to +{1}", "Уровень {0}/4 - низкое настроение усиливает иммунитет и отдых до +{1}" },
            { "ACTIVE: Mood {0}% → +{1}% Immunity/Rest", "АКТИВНО: настроение {0}% -> +{1}% к иммунитету/отдыху" },
            { "Mood above 50% — bonus activates when mood drops", "Настроение выше 50% - бонус включится, когда настроение упадёт" },
            { "Not unlocked — allocate nodes in Frenzy Path", "Не открыто - вложите очки в путь Ярости" },
            { "Tier {0}/4 — Max {1} stacks, +{2}/kill → up to +{3}", "Уровень {0}/4 - максимум зарядов: {1}, +{2}/убийство -> до +{3}" },
            { "FRENZIED: {0}/{1} stacks → +{2}% Melee Damage & Move Speed", "БЕШЕНСТВО: {0}/{1} зарядов -> +{2}% к урону в ближнем бою и скорости" },
            { "No frenzy — kill enemies to stack blood rage", "Нет бешенства - убивайте врагов, чтобы копить кровавую ярость" },
            { "Not unlocked — allocate nodes in Eureka Path", "Не открыто - вложите очки в путь Эврики" },
            { "Tier {0}/4 — Max {1} stacks, +{2}/tend → up to +{3}", "Уровень {0}/4 - максимум зарядов: {1}, +{2}/лечение -> до +{3}" },
            { "EUREKA: {0}/{1} stacks → +{2}% TendQ, +{3}% WorkSpeed", "ЭВРИКА: {0}/{1} зарядов -> +{2}% к качеству лечения, +{3}% к скорости работы" },
            { "No insight — tend patients to build stacks", "Нет озарения - лечите пациентов, чтобы копить заряды" },
            { "Not unlocked — allocate nodes in Pack Path", "Не открыто - вложите очки в путь Стаи" },
            { "Tier {0}/4 — +{1}/bonded animal, max {2} counted", "Уровень {0}/4 - +{1}/связанное животное, учитывается до {2}" },
            { "ACTIVE: {0} bonded animals → +{1}% Taming/Training/Gather", "АКТИВНО: связанных животных: {0} -> +{1}% к приручению/дрессировке/сбору" },
            { "No bonded animals on map — bond animals to empower", "На карте нет связанных животных - создавайте связи для усиления" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return HardcodedStringTools.NamedMethods(typeof(Window_SkillTree),
                "DrawClassPanel", "FormatBonus", "DrawDetailSection_ClassGimmick");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return HardcodedStringTools.ReplaceLdstr(instructions, Replacements);
        }
    }

    [HarmonyPatch(typeof(ITab_IsekaiStats))]
    public static class ITabIsekaiStatsHardcodedStrings_RuPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "XP: {0} / {1}", "Опыт: {0} / {1}" },
            { "CLASS PASSIVE", "ПАССИВКА КЛАССА" },
            { "WEAPON MASTERY", "МАСТЕРСТВО ОРУЖИЯ" },
            { "{0}: {1} ({2} XP)", "{0}: {1} ({2} опыта)" },
            { "No class assigned", "Класс не назначен" },
            { "Reach D rank (Lv11) to unlock", "Достигните ранга D (ур. 11), чтобы открыть" },
            { "{0}  [Not unlocked]", "{0}  [не открыто]" },
            { "+{0} Melee Damage", "+{0} к урону в ближнем бою" },
            { "+{0} Psychic Sensitivity", "+{0} к психической чувствительности" },
            { "+{0} next strike", "+{0} к следующему удару" },
            { "{0} dmg stored", "накоплено урона: {0}" },
            { "+{0} Tend/Surgery", "+{0} к лечению/операциям" },
            { "Building calm...", "Спокойствие копится..." },
            { "+{0} Shooting Accuracy", "+{0} к точности стрельбы" },
            { "{0} stacks", "зарядов: {0}" },
            { "{0} charges", "зарядов: {0}" },
            { "+{0} Work Speed", "+{0} к скорости работы" },
            { "Inactive", "Неактивно" },
            { "+{0} Social Impact", "+{0} к социальному влиянию" },
            { "No colonists", "Нет колонистов" },
            { "+{0} Immunity/Rest", "+{0} к иммунитету/отдыху" },
            { "Mood too high", "Настроение слишком высокое" },
            { "+{0} Melee/Speed", "+{0} к ближнему бою/скорости" },
            { "+{0} TendQ", "+{0} к качеству лечения" },
            { "+{0} Taming/Training", "+{0} к приручению/дрессировке" },
            { "No bonded animals", "Нет связанных животных" },
            { "{0}  [Tier {1}/4] — {2}", "{0}  [уровень {1}/4] - {2}" },
            { "Allocate Warlord's Path nodes to unlock", "Вложите очки в путь Воеводы, чтобы открыть" },
            { "Wrath of the Fallen  [Not unlocked]", "Гнев павших  [не открыто]" },
            { "Wrath of the Fallen  [Tier {0}/4]", "Гнев павших  [уровень {0}/4]" },
            { "ACTIVE: HP {0}  →  +{1} Melee Damage", "АКТИВНО: ЗД {0}  ->  +{1} к урону в ближнем бою" },
            { "Inactive — HP {0}  (triggers below {1})", "Неактивно - ЗД {0}  (срабатывает ниже {1})" },
            { "At threshold → up to +{0} Melee Damage", "На пороге -> до +{0} к урону в ближнем бою" },
            { "Arcane Overflow  [Not unlocked]", "Арканическое переполнение  [не открыто]" },
            { "Allocate Archmage's Path nodes to unlock", "Вложите очки в путь Архимага, чтобы открыть" },
            { "Arcane Overflow  [Tier {0}/4]", "Арканическое переполнение  [уровень {0}/4]" },
            { "ACTIVE: Psyfocus {0}  →  +{1} Psychic Sensitivity", "АКТИВНО: психофокус {0}  ->  +{1} к психической чувствительности" },
            { "Inactive — Psyfocus {0}  (triggers above {1})", "Неактивно - психофокус {0}  (срабатывает выше {1})" },
            { "At full focus → up to +{0} Psychic Sensitivity", "При полном фокусе -> до +{0} к психической чувствительности" },
            { "Divine Retribution  [Not unlocked]", "Божественное возмездие  [не открыто]" },
            { "Allocate Sanctuary's Vow nodes to unlock", "Вложите очки в путь Обета святилища, чтобы открыть" },
            { "Divine Retribution  [Tier {0}/4]", "Божественное возмездие  [уровень {0}/4]" },
            { "CHARGED: {0}/{1} dmg absorbed  →  +{2} next strike", "ЗАРЯЖЕНО: поглощено урона {0}/{1}  ->  +{2} к следующему удару" },
            { "No charge — take damage to build retribution ({0}/{1})", "Нет заряда - получайте урон, чтобы накопить возмездие ({0}/{1})" },
            { "At full charge → up to +{0} melee damage", "При полном заряде -> до +{0} к урону в ближнем бою" },
            { "Inner Calm  [Not unlocked]", "Внутреннее спокойствие  [не открыто]" },
            { "Allocate Ascetic Path nodes to unlock", "Вложите очки в путь Аскета, чтобы открыть" },
            { "4 hours", "4 часа" },
            { "3 hours", "3 часа" },
            { "2 hours", "2 часа" },
            { "1 hour", "1 час" },
            { "Inner Calm  [Tier {0}/4]", "Внутреннее спокойствие  [уровень {0}/4]" },
            { "CALM: +{0} Tend/Surgery, +{1} Research", "СПОКОЙСТВИЕ: +{0} к лечению/операциям, +{1} к исследованиям" },
            { "Just hit — calm is reset, building over {0}", "Недавно получен удар - спокойствие сброшено, накопление за {0}" },
            { "At full calm → up to +{0} Tend / Surgery", "При полном спокойствии -> до +{0} к лечению/операциям" },
            { "Predator Focus  [Not unlocked]", "Фокус хищника  [не открыто]" },
            { "Allocate Hawkeye Path nodes to unlock", "Вложите очки в путь Соколиного глаза, чтобы открыть" },
            { "Predator Focus  [Tier {0}/4]", "Фокус хищника  [уровень {0}/4]" },
            { "LOCKED ON: {0}/{1} stacks  →  +{2} Shooting Accuracy", "ЦЕЛЬ ЗАХВАЧЕНА: {0}/{1} зарядов  ->  +{2} к точности стрельбы" },
            { "No target — land ranged hits to stack focus ({0}/{1})", "Нет цели - попадайте дальними атаками, чтобы копить фокус ({0}/{1})" },
            { "At max stacks → up to +{0} Shooting Accuracy", "При максимуме зарядов -> до +{0} к точности стрельбы" },
            { "Counter Strike  [Not unlocked]", "Контрудар  [не открыто]" },
            { "Allocate Centerpoint Path nodes to unlock", "Вложите очки в путь Центра равновесия, чтобы открыть" },
            { "Counter Strike  [Tier {0}/4]", "Контрудар  [уровень {0}/4]" },
            { "CHARGED: {0}/{1} charges  →  +{2} Melee Damage", "ЗАРЯЖЕНО: {0}/{1} зарядов  ->  +{2} к урону в ближнем бою" },
            { "No charges — dodge melee attacks to store counters ({0}/{1})", "Нет зарядов - уклоняйтесь от ближних атак, чтобы копить контрудары ({0}/{1})" },
            { "At max charges → up to +{0} Melee Damage", "При максимуме зарядов -> до +{0} к урону в ближнем бою" },
            { "Masterwork Insight  [Not unlocked]", "Интуиция мастерства  [не открыто]" },
            { "Allocate Mastercraft Path nodes to unlock", "Вложите очки в путь Мастерского ремесла, чтобы открыть" },
            { "Masterwork Insight  [Tier {0}/4]", "Интуиция мастерства  [уровень {0}/4]" },
            { "Crafting {0}/20 → +{1} Work Speed", "Ремесло {0}/20 -> +{1} к скорости работы" },
            { "Inactive — Crafting skill at 0", "Неактивно - навык ремесла равен 0" },
            { "At max Crafting → up to +{0} Work Speed", "При максимальном ремесле -> до +{0} к скорости работы" },
            { "Rallying Presence  [Not unlocked]", "Вдохновляющее присутствие  [не открыто]" },
            { "Allocate Rally Path nodes to unlock", "Вложите очки в путь Сплочения, чтобы открыть" },
            { "Rallying Presence  [Tier {0}/4]", "Вдохновляющее присутствие  [уровень {0}/4]" },
            { "{0} colonists → +{1} Social Impact", "колонистов: {0} -> +{1} к социальному влиянию" },
            { "No colonists on map — recruit allies to empower", "На карте нет колонистов - вербуйте союзников для усиления" },
            { "Unyielding Spirit  [Not unlocked]", "Несгибаемый дух  [не открыто]" },
            { "Allocate Unyielding Path nodes to unlock", "Вложите очки в Несгибаемый путь, чтобы открыть" },
            { "Unyielding Spirit  [Tier {0}/4]", "Несгибаемый дух  [уровень {0}/4]" },
            { "Mood {0}% → +{1} Immunity/Rest", "Настроение {0}% -> +{1} к иммунитету/отдыху" },
            { "Mood above 50% — bonus activates when mood drops", "Настроение выше 50% - бонус включится, когда настроение упадёт" },
            { "At lowest mood → up to +{0} Immunity/Rest", "При самом низком настроении -> до +{0} к иммунитету/отдыху" },
            { "Blood Frenzy  [Not unlocked]", "Кровавое бешенство  [не открыто]" },
            { "Allocate Frenzy Path nodes to unlock", "Вложите очки в путь Ярости, чтобы открыть" },
            { "Blood Frenzy  [Tier {0}/4]", "Кровавое бешенство  [уровень {0}/4]" },
            { "FRENZIED: {0}/{1} stacks  →  +{2} Melee/Speed", "БЕШЕНСТВО: {0}/{1} зарядов  ->  +{2} к ближнему бою/скорости" },
            { "No frenzy — kill enemies to stack ({0}/{1})", "Нет бешенства - убивайте врагов, чтобы копить заряды ({0}/{1})" },
            { "At max stacks → up to +{0} Melee/Speed", "При максимуме зарядов -> до +{0} к ближнему бою/скорости" },
            { "Eureka Synthesis  [Not unlocked]", "Синтез эврики  [не открыто]" },
            { "Allocate Eureka Path nodes to unlock", "Вложите очки в путь Эврики, чтобы открыть" },
            { "Eureka Synthesis  [Tier {0}/4]", "Синтез эврики  [уровень {0}/4]" },
            { "EUREKA: {0}/{1} stacks  →  +{2} TendQ, +{3} WorkSpeed", "ЭВРИКА: {0}/{1} зарядов  ->  +{2} к качеству лечения, +{3} к скорости работы" },
            { "No insight — tend patients to build ({0}/{1})", "Нет озарения - лечите пациентов, чтобы копить заряды ({0}/{1})" },
            { "At max stacks → up to +{0} TendQ", "При максимуме зарядов -> до +{0} к качеству лечения" },
            { "Pack Alpha  [Not unlocked]", "Вожак стаи  [не открыто]" },
            { "Allocate Pack Path nodes to unlock", "Вложите очки в путь Стаи, чтобы открыть" },
            { "Pack Alpha  [Tier {0}/4]", "Вожак стаи  [уровень {0}/4]" },
            { "{0} bonded animals → +{1} Taming/Training/Gather", "связанных животных: {0} -> +{1} к приручению/дрессировке/сбору" },
            { "No bonded animals on map — bond animals to empower", "На карте нет связанных животных - создавайте связи для усиления" },
            { "Max Lv (100)", "Макс. ур. (100)" },
            { "Max", "Макс" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return HardcodedStringTools.NamedMethods(typeof(ITab_IsekaiStats),
                "FillTabVanilla", "DrawVanillaGimmick", "DrawStatsContent", "DrawStatPanel");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return HardcodedStringTools.ReplaceLdstr(instructions, Replacements);
        }
    }

    [HarmonyPatch(typeof(Window_StatsAttribution))]
    public static class WindowStatsAttributionHardcodedStrings_RuPatch
    {
        private static readonly Dictionary<string, string> Replacements = new Dictionary<string, string>
        {
            { "STAT ALLOCATION", "РАСПРЕДЕЛЕНИЕ ХАРАКТЕРИСТИК" },
            { "AFFECTED STATS", "ЗАТРОНУТЫЕ ПОКАЗАТЕЛИ" },
            { "Unknown", "Неизвестно" },
        };

        public static IEnumerable<MethodBase> TargetMethods()
        {
            return HardcodedStringTools.NamedMethods(typeof(Window_StatsAttribution),
                "DoWindowContentsVanilla", "DrawAffectedStats", "DrawAffectedStatsContent", "GetStatFullName");
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return HardcodedStringTools.ReplaceLdstr(instructions, Replacements);
        }
    }
}
