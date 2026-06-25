# Как править HardcodedStrings.tsv без base64

Некоторые строки модов сидят прямо в C# коде оригинального мода. Для них обычный RimWorld XML-перевод не работает, поэтому в переводах есть файл:

```text
Languages/Russian/Strings/HardcodedStrings.tsv
```

Внутри он выглядит страшно, потому что обе колонки закодированы в base64:

```text
TmFub2NoZXN0    0J3QsNC90L7RgdGD0L3QtNGD0Lo=
```

Этот файл нужен игре. Его руками редактировать не надо.

## Самый простой способ: двойной клик

В корне репозитория есть три файла:

```text
1_Decode_HardcodedStrings.cmd
2_Encode_HardcodedStrings.cmd
3_Validate_HardcodedStrings.cmd
```

Порядок работы:

1. Дважды кликни `1_Decode_HardcodedStrings.cmd`.
2. Открой нужный `HardcodedStrings.edit.json` в VS Code.
3. Правь только поле `translation`.
4. Дважды кликни `2_Encode_HardcodedStrings.cmd`.
5. Дважды кликни `3_Validate_HardcodedStrings.cmd`.

Если окно пишет `All checks passed`, всё готово.

## Способ через VS Code tasks

1. Открой репозиторий в VS Code.
2. Нажми `Terminal -> Run Task...`.
3. Запусти задачу:

```text
Hardcoded strings: Decode TSV to editable JSON
```

4. Найди рядом с нужным `HardcodedStrings.tsv` файл:

```text
HardcodedStrings.edit.json
```

5. Открой его и правь только поле `translation`.

Пример:

```json
{
  "original": "Scan Interval (Ticks):",
  "translation": "Интервал сканирования (тики):"
}
```

`original` лучше не менять. Это точная английская строка из C# кода. Если ее изменить, патч может перестать находить строку в игре.

6. Когда закончил правки, снова нажми `Terminal -> Run Task...` и запусти:

```text
Hardcoded strings: Encode editable JSON to TSV
```

7. Потом запусти проверку:

```text
Hardcoded strings: Validate
```

Если ошибок нет, файл готов для коммита.

## Команды вручную через терминал

Из корня репозитория:

```powershell
.\Tools\HardcodedStringsTool.ps1 list
.\Tools\HardcodedStringsTool.ps1 decode
.\Tools\HardcodedStringsTool.ps1 encode
.\Tools\HardcodedStringsTool.ps1 validate
```

Только один мод:

```powershell
.\Tools\HardcodedStringsTool.ps1 decode -Mod Nanotech_Overpower_RU_Full
.\Tools\HardcodedStringsTool.ps1 encode -Mod Nanotech_Overpower_RU_Full
.\Tools\HardcodedStringsTool.ps1 validate -Mod Nanotech_Overpower_RU_Full
```

## Важно

- Правь `translation`, не `original`.
- Не удаляй фигурные скобки вроде `{0}`, `{1}`. Это подстановки игры.
- Не удаляй XML/GUI теги вроде `<b>...</b>`, если они есть в оригинале.
- После `encode` игра читает уже обновленный `HardcodedStrings.tsv`.
- Если менялись C# исходники в `Source`, DLL сама не пересоберется. Ее потом соберет тот, кто занимается сборкой.
