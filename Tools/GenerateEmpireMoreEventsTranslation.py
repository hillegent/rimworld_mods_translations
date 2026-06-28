from __future__ import annotations

import json
import re
import time
import urllib.parse
import urllib.request
from pathlib import Path
from typing import Iterable
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape


REPO_ROOT = Path(__file__).resolve().parents[1]
MOD_ROOT = REPO_ROOT / "Empire_Refactored_RU_Full"
SOURCE_ROOT = Path(r"D:\steam\steamapps\workshop\content\294100\3751198980")
CACHE_PATH = REPO_ROOT / "Tools" / ".empire_more_events_translate_cache.json"

TRANSLATABLE_FIELDS = {"label", "desc", "description"}
TRANSLATABLE_LIST_FIELDS = {"patchNoteLines", "linkButtonToolTips", "additionalNotes"}
PROTECTED_RE = re.compile(r"\{[^{}]+\}|\[[^\[\]]+\]")


KEYED_TRANSLATIONS = {
    "EE_SettingsDebugLogging": "Включить отладочное логирование",
    "EE_SettingsCategory": "Empire: больше событий",
    "EE_ResourcePickerDesc": "Выберите, на каком ресурсе будут специализироваться эти опытные рабочие.",
    "EE_SelectResource": "Выбрать ресурс",
    "EE_NoActiveResources": "Нет активных ресурсов (закрыть)",
    "EE_ImmigrantsSourceLabel": "Опытные иммигранты ({0})",
    "EE_ImmigrantsAssignedTitle": "Опытные иммигранты назначены",
    "EE_ImmigrantsAssignedDesc": "Опытные иммигранты назначены на усиление производства {0} в {1}.",
    "EV_PatchTitle": "Примечания к обновлениям: больше событий",
    "EV_OpenPatchNotes": "Открыть примечания к обновлениям",
}


MANUAL_TRANSLATIONS = {
    "Join the Empire discord!": "Присоединиться к Discord Empire!",
    "Please raise any issues you find on the Github page.": "Сообщайте о найденных проблемах на странице Github.",
    "Release": "Релиз",
    "Release!": "Релиз!",
    "Event Cost Tweaks": "Правки стоимости событий",
    "Just a version bump for the official workshop release.": "Просто повышение версии для официального релиза в мастерской.",
    "Changes the cost scaling for a couple of event options": "Изменяет масштабирование стоимости у нескольких вариантов событий.",
    "Share surplus with struggling neighboring factions. Goodwill buys more than grain.": "Поделиться излишками с нуждающимися соседними фракциями. Добрая воля покупает больше, чем зерно.",
    "Scouts have discovered an abandoned outpost near one of your settlements. The structure is weathered, but intact; whoever built it left in a hurry. The site could be repurposed in one of several ways.": "Разведчики обнаружили заброшенный аванпост недалеко от одного из ваших поселений. Постройка обветшала, но осталась целой; тот, кто ее возвел, ушел в спешке. Это место можно использовать несколькими способами.",
    "Strip it for materials. Everything useful goes back to the settlement.": "Разобрать его на материалы. Все полезное отправить обратно в поселение.",
    "Strip it bare. Take everything that isn't nailed down, then take the nails.": "Разобрать его подчистую. Забрать все, что не прибито, а потом забрать и гвозди.",
    "Outpost Salvaged": "Аванпост разобран на материалы",
    "Clear-cut all of it. This land and everything on it is ours by right.": "Вырубить все подчистую. Эта земля и все, что на ней, принадлежит нам по праву.",
    "Disputed Spring": "Спорный родник",
    "Hire prospectors to find our own water source. We don't need their spring.": "Нанять старателей, чтобы найти собственный источник воды. Нам не нужен их родник.",
    "The situation at the spring has deteriorated. Armed settlers from both sides are staring each other down across a drying creek bed. Scuffles have broken out. One wrong move and this becomes a full-blown conflict. What are your orders?": "Ситуация у родника ухудшилась. Вооруженные поселенцы с обеих сторон смотрят друг на друга через русло высыхающего ручья. Уже начались стычки. Одно неверное движение — и это перерастет в полноценный конфликт. Каковы ваши приказы?",
    "Retreat from the Spring": "Отступление от родника",
    "Your settlers withdraw from the disputed spring under the jeering of the neighboring community. The settlement loses access to the water source entirely. Morale plummets as word spreads that your nation backed down from a fight.": "Ваши поселенцы отходят от спорного родника под насмешками соседней общины. Поселение полностью теряет доступ к источнику воды. Мораль падает, когда распространяется слух, что ваша нация отступила от борьбы.",
    "The spring is yours — but the violence leaves a mark.": "Родник ваш, но насилие оставляет след.",
    "Your militia overwhelms the neighboring settlers in a brief but brutal confrontation. The spring is yours — but the violence leaves a mark. The neighbors won't forget this, and your own people are unsettled by the bloodshed. Still, the water flows freely now.": "Ваше ополчение подавляет соседних поселенцев в коротком, но жестоком столкновении. Родник ваш, но насилие оставляет след. Соседи этого не забудут, а ваш народ встревожен кровопролитием. И все же вода теперь течет свободно.",
    "Your prospectors strike water deep underground — it's not as convenient as the disputed spring, but it's yours alone. During the weeks of drilling and construction, food production dips as the old source becomes unreliable. Once the wells are operational, the settlement will be self-sufficient.": "Ваши старатели находят воду глубоко под землей. Это не так удобно, как спорный родник, зато он принадлежит только вам. На время бурения и строительства производство еды падает, потому что старый источник становится ненадежным. Когда скважины заработают, поселение станет самодостаточным.",
    "They no longer need the disputed spring.": "Спорный родник им больше не нужен.",
    "Word arrives that the neighboring community has tapped into their own underground water source. They no longer need the disputed spring. The sharing agreement dissolves naturally, and your settlement regains full access. The crisis is over.": "Приходят вести, что соседняя община вышла на собственный подземный источник воды. Спорный родник им больше не нужен. Соглашение о совместном использовании распадается само собой, и ваше поселение снова получает полный доступ. Кризис закончен.",
    "Hidden Spring": "Скрытый родник",
    "The hermit reluctantly agrees to relocate, hauling cartloads of books and equipment. Their knowledge is vast and their contributions immediate — research output surges. Unfortunately, their personality is... difficult. They argue with everyone, keep bizarre hours, and their experiments occasionally produce unsettling smells.": "Отшельник неохотно соглашается на переезд, везя за собой телеги с книгами и оборудованием. Его знания огромны, а вклад чувствуется сразу — результаты исследований растут. К сожалению, характер у него... сложный. Он спорит со всеми, работает в странные часы, а его эксперименты иногда производят тревожные запахи.",
    "A skilled healer with an impressive collection of remedies and surgical tools has arrived at one of your settlements. They're willing to offer their services — for the right price. Their knowledge could benefit your medical infrastructure.": "В одно из ваших поселений прибыл опытный целитель с внушительной коллекцией лекарств и хирургических инструментов. Он готов предложить свои услуги — за разумную цену. Его знания могут принести пользу вашей медицинской инфраструктуре.",
}


TERM_FIXES = (
    ("Github", "GitHub"),
    ("Римворлд", "RimWorld"),
    ("Rimworld", "RimWorld"),
    ("Дискорд", "Discord"),
    ("Империя Refactored", "Empire Refactored"),
    ("Империи Refactored", "Empire Refactored"),
    ("Empire Refactored: больше событий", "Empire Refactored: More Events"),
    ("Больше событий", "больше событий"),
    ("персона Core", "персона-ядро"),
    ("Персона Core", "Персона-ядро"),
)


def read_xml(path: Path) -> ET.Element:
    return ET.parse(path).getroot()


def read_key_order(path: Path) -> list[str]:
    root = read_xml(path)
    return [child.tag for child in root if isinstance(child.tag, str)]


def write_language_data(path: Path, entries: Iterable[tuple[str, str]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<LanguageData>"]
    for key, value in entries:
        lines.append(f"  <{key}>{escape(value)}</{key}>")
    lines.append("</LanguageData>")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_keyed(source_path: Path, dest_path: Path) -> None:
    keys = read_key_order(source_path)
    missing = [key for key in keys if key not in KEYED_TRANSLATIONS]
    extra = [key for key in KEYED_TRANSLATIONS if key not in keys]
    if missing or extra:
        raise RuntimeError(f"{dest_path.name}: missing={missing}, extra={extra}")
    write_language_data(dest_path, [(key, KEYED_TRANSLATIONS[key]) for key in keys])


def load_cache() -> dict[str, str]:
    if not CACHE_PATH.exists():
        return {}
    return json.loads(CACHE_PATH.read_text(encoding="utf-8"))


def save_cache(cache: dict[str, str]) -> None:
    CACHE_PATH.write_text(json.dumps(cache, ensure_ascii=False, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def protect_text(text: str) -> tuple[str, dict[str, str]]:
    replacements: dict[str, str] = {}

    def repl(match: re.Match[str]) -> str:
        token = f"__PH{len(replacements)}__"
        replacements[token] = match.group(0)
        return token

    return PROTECTED_RE.sub(repl, text), replacements


def restore_text(text: str, replacements: dict[str, str]) -> str:
    for token, value in replacements.items():
        text = text.replace(token, value)
        text = text.replace(token.lower(), value)
    return text


def apply_term_fixes(text: str) -> str:
    for old, new in TERM_FIXES:
        text = text.replace(old, new)
    text = text.replace("\u200b", "").replace("\u200c", "").replace("\u200d", "")
    text = re.sub(r"\s+([.,:;!?])", r"\1", text)
    text = text.replace(" ,", ",").replace(" .", ".")
    text = "\n".join(line.rstrip() for line in text.splitlines())
    return text


def google_translate(text: str) -> str:
    protected, replacements = protect_text(text)
    query = urllib.parse.urlencode(
        {
            "client": "gtx",
            "sl": "en",
            "tl": "ru",
            "dt": "t",
            "q": protected,
        }
    )
    url = f"https://translate.googleapis.com/translate_a/single?{query}"
    with urllib.request.urlopen(url, timeout=30) as response:
        data = json.loads(response.read().decode("utf-8"))
    translated = "".join(part[0] for part in data[0] if part and part[0])
    translated = restore_text(translated, replacements)
    return apply_term_fixes(translated)


def translate(text: str, cache: dict[str, str]) -> str:
    text = text.strip()
    if not text:
        return text
    if text in MANUAL_TRANSLATIONS:
        return MANUAL_TRANSLATIONS[text]
    if text in cache:
        return cache[text]

    last_error: Exception | None = None
    for attempt in range(5):
        try:
            value = google_translate(text)
            cache[text] = value
            if len(cache) % 25 == 0:
                save_cache(cache)
            time.sleep(0.12)
            return value
        except Exception as exc:  # pragma: no cover - helper script
            last_error = exc
            time.sleep(1 + attempt)
    raise RuntimeError(f"Could not translate: {text}") from last_error


def extract_entries(path: Path, class_name: str, cache: dict[str, str]) -> list[tuple[str, str]]:
    entries: list[tuple[str, str]] = []
    root = read_xml(path)
    for element in root:
        if element.tag != class_name:
            continue
        def_name = element.findtext("defName")
        if not def_name:
            continue
        for child in element:
            if child.tag in TRANSLATABLE_FIELDS and (child.text or "").strip():
                key = f"{def_name}.{child.tag}"
                entries.append((key, translate(child.text or "", cache)))
            if child.tag in TRANSLATABLE_LIST_FIELDS:
                for idx, item in enumerate(child.findall("li")):
                    if (item.text or "").strip():
                        key = f"{def_name}.{child.tag}.{idx}"
                        entries.append((key, translate(item.text or "", cache)))
    return entries


def write_event_and_option_files(cache: dict[str, str]) -> None:
    source_dir = SOURCE_ROOT / "1.6" / "Defs" / "FCEventDefs"
    event_dest = MOD_ROOT / "Languages" / "Russian" / "DefInjected" / "FactionColonies.FCEventDef"
    option_dest = MOD_ROOT / "Languages" / "Russian" / "DefInjected" / "FactionColonies.FCOptionDef"

    for source_path in sorted(source_dir.glob("*.xml")):
        event_entries = extract_entries(source_path, "FactionColonies.FCEventDef", cache)
        option_entries = extract_entries(source_path, "FactionColonies.FCOptionDef", cache)
        if event_entries:
            write_language_data(event_dest / source_path.name, event_entries)
        if option_entries:
            write_language_data(option_dest / source_path.name, option_entries)


def write_patch_notes(cache: dict[str, str]) -> None:
    source_dir = SOURCE_ROOT / "1.6" / "Defs" / "FCPatchNoteDefs"
    entries: list[tuple[str, str]] = []

    for source_path in sorted(source_dir.glob("*.xml")):
        entries.extend(extract_entries(source_path, "FactionColonies.PatchNoteDef", cache))

    write_language_data(
        MOD_ROOT / "Languages" / "Russian" / "DefInjected" / "FactionColonies.PatchNoteDef" / "MoreEvents.xml",
        entries,
    )


def main() -> None:
    if not SOURCE_ROOT.exists():
        raise RuntimeError(f"Source mod is missing: {SOURCE_ROOT}")

    cache = load_cache()
    write_keyed(
        SOURCE_ROOT / "Languages" / "English" / "Keyed" / "EmpireEvents.xml",
        MOD_ROOT / "Languages" / "Russian" / "Keyed" / "EmpireEvents.xml",
    )
    write_event_and_option_files(cache)
    write_patch_notes(cache)
    save_cache(cache)


if __name__ == "__main__":
    main()
