using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace IsekaiRPGLevelingRUFull
{
    public static class GuildFactionAddonRuPatch
    {
        private const string GuildFactionDefName = "GuildFaction_AdventurersGuild";
        private const string GuildHallName = "Зал гильдии";
        private const string ExcaliburRu = "Экскалибур";

        private static readonly Dictionary<string, string> StringReplacements = new Dictionary<string, string>
        {
            { "creature", "существо" },
            { "the Hero", "герой" },
            { "the adventurer", "авантюрист" },
            { "Unknown", "Неизвестно" },
        };

        private static readonly (string first, string nick, string last, string flavor)[] LocalizedIdentities =
        {
            ("Артур", "Экскалибур", "Экскалибур", "герой Святого меча"),
            ("Магнус", "Мудрец", "Пендрак", "нестареющий волшебник, основавший Гильдию"),

            ("Рейнхард", "Святой меча", "Астрея", "Святой меча, непревзойдённый рыцарь"),
            ("Фрирена", "Истребительница", "Элфхарт", "древняя эльфийская архимагиня, сразившая Короля демонов"),
            ("Айзель", "Принцесса меча", "Валленхейм", "Принцесса меча из подземелья"),
            ("Эрзия", "Титания", "Скарлетта", "королева в доспехах, владеющая сотней мечей"),
            ("Рудеус", "Трясина", "Грейрат", "талантливый архимаг со второй жизнью"),

            ("Беллрик", "Маленький новичок", "Крейнхарт", "мечник-новичок, взлетевший быстрее остальных"),
            ("Наофу", "Герой щита", "Шилдварден", "Герой щита, который никогда не обнажает клинок"),
            ("Хадрик", "Истребитель гоблинов", "Хелмоак", "воин в шлеме, охотящийся на гоблинов"),
            ("Тания", "Дьявол", "Дегуреч", "маленькая тактическая магиня с холодным умом"),
            ("Мегрим", "Багровый демон", "Кримсонбласт", "магиня взрывов и фанатичка с повязкой на глазу"),
            ("Субарн", "Петлитель", "Нацумэ", "авантюрист в капюшоне, возвращающийся из смерти"),

            ("Казма", "Хитрец", "Сатох", "ленивый интриган, который каким-то образом продолжает побеждать"),
            ("Стерк", "Железное сердце", "Айзенхарт", "молодой секирщик, сильнее, чем сам думает"),
            ("Ферн", "Спокойная", "Голденлиф", "тихая ученица мага у древней наставницы"),
            ("Линна", "Убийца бандитов", "Инверс", "пылкая волшебница с соответствующим нравом"),
            ("Холо", "Мудрая волчица", "Йойцу", "странствующая торговка с острым умом"),
            ("Эрис", "Дикая кошка", "Бореас", "юная дворянка-мечница, живущая инстинктами"),
            ("Рокса", "Мудрец воды", "Мигурд", "получеловеческая специалистка по водной магии"),
            ("Милена", "Средняя", "Фалор", "авантюристка, называющая себя средней, но это не так"),

            ("Катарин", "Злодейка", "Клаес", "дворянка, изгнанная в авантюризм"),
            ("Машел", "Железный кулак", "Бёрндаль", "боец одной лишь мышечной силы, ненавидящий магию"),
            ("Аникс", "Читательница", "Форгар", "маленькая девочка-псионик, скрывающая талант"),
            ("Беатрис", "Дух", "Розвааль", "магиня-контрактор, привязанная к библиотеке"),
            ("Рема", "Демоническая служанка", "Перлсворт", "служанка-мечница с кровью они"),
            ("Мейпл", "Стена", "Танкхарт", "танк в полных доспехах, отказывающийся получать урон"),
            ("Ирума", "Демонический мальчик", "Судзаки", "добросердечный мальчик со скрытым происхождением"),
            ("Аквалин", "Бесполезная богиня", "Фэй", "самопровозглашённая жрица, в основном бесполезная"),

            ("Ллойд", "Неведающий", "Бельведер", "называет себя слабейшим, хотя это не так"),
            ("Даркнесса", "Крестоносец", "Холм", "рыцарь в доспехах, которому нравится принимать удары"),
            ("Широ", "Стратег", "Иезекииль", "тактик в очках из задней линии"),
            ("Тоя", "Иномирец", "Мочидзуки", "призванный юноша с обычным набором навыков"),
            ("Хадзиро", "Синергист", "Татибана", "наполовину изменённый странник нижних глубин"),
            ("Юна", "Медвежий рыцарь", "Биртуф", "девушка в медвежьем капюшоне, обманчиво опасная"),

            ("Луг", "Тихий шаг", "Туат", "ученик-ассасин, едва прошедший испытания"),
            ("Кайн", "Полукровка", "Яширо", "полувампир-странник, ищущий предназначение"),
            ("Катто", "Бродяга", "Хисаши", "призван с кошачьим навыком и сожалеет об этом"),

            ("Слаймо", "Хлюпик", "Темпен", "новичок слабейшего класса, мечтающий эволюционировать"),
            ("Кобато", "Новенькая", "Ийо", "первый день на работе"),
        };

        public static void Apply(Harmony harmony)
        {
            try
            {
                Type identitiesType = AccessTools.TypeByName("GuildFactionAddon.GuildIdentities");
                if (identitiesType == null)
                    return;

                LocalizeGuildIdentities(identitiesType);

                PatchPostfix(harmony, "GuildFactionAddon.GuildRosterWorldComponent", "RenameGuildSettlements", nameof(RenameGuildSettlementsPostfix));
                PatchPostfix(harmony, "GuildFactionAddon.GuildFactionMidsaveSpawner", "SpawnFactionAndSettlement", nameof(SpawnFactionAndSettlementPostfix));
                PatchPostfix(harmony, "GuildFactionAddon.GuildRosterGenerator", "EquipExcalibur", nameof(EquipExcaliburPostfix));

                PatchTranspiler(harmony, "GuildFactionAddon.GuildQuestBoardEntry", "get_ShortDescription");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildRecruitmentDuel", "OnDuelistSurrendered");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildRecruitmentDuel", "OnDuelistDied");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildRecruitmentDuel", "OnDuelistFled");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildHeroVisit", "get_DescriptionPart");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildHeroVisit", "OnVisitConcluded");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildHeroVisit", "OnHeroMoodFail");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildHeroVisit", "OnHeroAttacked");
                PatchTranspiler(harmony, "GuildFactionAddon.QuestPart_GuildHeroVisit", "OnHeroKilled");
                PatchTranspiler(harmony, "GuildFactionAddon.GuildMemberRecord", "ExposeData");

                Type recordType = AccessTools.TypeByName("GuildFactionAddon.GuildMemberRecord");
                if (recordType != null)
                {
                    foreach (ConstructorInfo ctor in AccessTools.GetDeclaredConstructors(recordType))
                    {
                        harmony.Patch(ctor, transpiler: new HarmonyMethod(typeof(GuildFactionAddonRuPatch), nameof(ReplaceAddonStringsTranspiler)));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[ISEKAI RPG LEVELING RU] Guild Faction Add-on Russian patches skipped: " + ex.Message);
            }
        }

        private static void LocalizeGuildIdentities(Type identitiesType)
        {
            FieldInfo allField = AccessTools.Field(identitiesType, "All");
            if (!(allField?.GetValue(null) is Array identities))
                return;

            Type identityType = AccessTools.TypeByName("GuildFactionAddon.GuildIdentity");
            if (identityType == null)
                return;

            FieldInfo firstName = AccessTools.Field(identityType, "firstName");
            FieldInfo nickName = AccessTools.Field(identityType, "nickName");
            FieldInfo lastName = AccessTools.Field(identityType, "lastName");
            FieldInfo flavor = AccessTools.Field(identityType, "flavor");

            int count = Math.Min(identities.Length, LocalizedIdentities.Length);
            for (int i = 0; i < count; i++)
            {
                object identity = identities.GetValue(i);
                if (identity == null)
                    continue;

                (string first, string nick, string last, string text) = LocalizedIdentities[i];
                firstName?.SetValue(identity, first);
                nickName?.SetValue(identity, nick);
                lastName?.SetValue(identity, last);
                flavor?.SetValue(identity, text);
            }
        }

        private static void PatchPostfix(Harmony harmony, string typeName, string methodName, string postfixName)
        {
            MethodInfo method = AccessTools.Method(AccessTools.TypeByName(typeName), methodName);
            MethodInfo postfix = AccessTools.Method(typeof(GuildFactionAddonRuPatch), postfixName);
            if (method != null && postfix != null)
                harmony.Patch(method, postfix: new HarmonyMethod(postfix));
        }

        private static void PatchTranspiler(Harmony harmony, string typeName, string methodName)
        {
            MethodInfo method = AccessTools.Method(AccessTools.TypeByName(typeName), methodName);
            if (method != null)
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(GuildFactionAddonRuPatch), nameof(ReplaceAddonStringsTranspiler)));
        }

        public static IEnumerable<CodeInstruction> ReplaceAddonStringsTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            return HardcodedStringTools.ReplaceLdstr(instructions, StringReplacements);
        }

        public static void RenameGuildSettlementsPostfix(Faction guildFaction)
        {
            SetGuildSettlementNames(guildFaction);
        }

        public static void SpawnFactionAndSettlementPostfix(FactionDef factionDef)
        {
            if (factionDef?.defName != GuildFactionDefName)
                return;

            Faction guildFaction = Find.FactionManager?.FirstFactionOfDef(factionDef);
            SetGuildSettlementNames(guildFaction);
        }

        private static void SetGuildSettlementNames(Faction guildFaction)
        {
            if (guildFaction == null)
                return;

            List<Settlement> settlements = Find.WorldObjects?.Settlements;
            if (settlements == null)
                return;

            for (int i = 0; i < settlements.Count; i++)
            {
                Settlement settlement = settlements[i];
                if (settlement?.Faction != guildFaction)
                    continue;

                try
                {
                    settlement.Name = GuildHallName;
                }
                catch
                {
                    // Keep this silent: the original addon already logs rename failures.
                }
            }
        }

        public static void EquipExcaliburPostfix(Pawn arthur)
        {
            ThingWithComps primary = arthur?.equipment?.Primary;
            if (primary == null)
                return;

            RegisterNamedWeapon(primary, ExcaliburRu);
        }

        private static void RegisterNamedWeapon(Thing thing, string name)
        {
            Type registryType = AccessTools.TypeByName("GuildFactionAddon.NamedWeaponsRegistry");
            MethodInfo getMethod = AccessTools.Method(registryType, "Get");
            MethodInfo registerMethod = AccessTools.Method(registryType, "Register");
            object registry = getMethod?.Invoke(null, null);
            if (registry == null)
                return;

            registerMethod?.Invoke(registry, new object[] { thing, name });
        }
    }

    [HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.LabelNoCount), MethodType.Getter)]
    [HarmonyAfter("JellyCreative.GuildFactionAddon")]
    [HarmonyPriority(Priority.Last)]
    public static class GuildNamedWeaponLabel_RuPatch
    {
        public static void Postfix(ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
                return;

            if (__result == "Excalibur")
                __result = "Экскалибур";
            else if (__result.StartsWith("Excalibur +", StringComparison.Ordinal))
                __result = "Экскалибур" + __result.Substring("Excalibur".Length);
        }
    }
}
