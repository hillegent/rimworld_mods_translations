param(
    [Parameter(Position = 0)]
    [ValidateSet("help", "list", "decode", "encode", "validate")]
    [string]$Action = "help",

    [string]$Mod,
    [string]$File
)

$ErrorActionPreference = "Stop"
$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")).Path
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

function Write-Info {
    param([string]$Message)
    Write-Host "[HardcodedStrings] $Message"
}

function Get-RelativePath {
    param([string]$Path)
    $slash = [char]92
    $base = $RepoRoot.TrimEnd($slash) + [string]$slash
    if ($Path.StartsWith($base, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $Path.Substring($base.Length)
    }
    return $Path
}

function Get-EditJsonPath {
    param([string]$TsvPath)
    return [System.IO.Path]::Combine([System.IO.Path]::GetDirectoryName($TsvPath), "HardcodedStrings.edit.json")
}

function ConvertFrom-Base64Utf8 {
    param(
        [string]$Value,
        [string]$Path,
        [int]$LineNumber
    )

    try {
        return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($Value))
    }
    catch {
        throw ("Cannot decode base64 at {0}:{1}" -f (Get-RelativePath $Path), $LineNumber)
    }
}

function ConvertTo-Base64Utf8 {
    param([string]$Value)
    return [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($Value))
}

function Find-HardcodedFiles {
    if ($File) {
        $resolved = (Resolve-Path -LiteralPath $File).Path
        if (-not $resolved.EndsWith("HardcodedStrings.tsv", [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "File must be named HardcodedStrings.tsv: $resolved"
        }
        return @($resolved)
    }

    $files = Get-ChildItem -LiteralPath $RepoRoot -Recurse -File -Filter "HardcodedStrings.tsv" |
        Where-Object { $_.FullName -like "*\Languages\Russian\Strings\HardcodedStrings.tsv" }

    if ($Mod) {
        $modRoot = Join-Path $RepoRoot $Mod
        $files = $files | Where-Object { $_.FullName.StartsWith($modRoot, [System.StringComparison]::OrdinalIgnoreCase) }
    }

    return @($files | Sort-Object FullName | ForEach-Object { $_.FullName })
}

function Read-HardcodedTsv {
    param([string]$Path)

    $rows = New-Object System.Collections.Generic.List[object]
    $lines = [System.IO.File]::ReadAllLines($Path, [System.Text.Encoding]::UTF8)
    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        $lineNumber = $i + 1
        if ([string]::IsNullOrWhiteSpace($line) -or $line.StartsWith("#")) {
            continue
        }

        $parts = $line.Split("`t")
        if ($parts.Count -lt 2) {
            throw ("Expected two tab-separated base64 columns at {0}:{1}" -f (Get-RelativePath $Path), $lineNumber)
        }

        $original = ConvertFrom-Base64Utf8 -Value $parts[0] -Path $Path -LineNumber $lineNumber
        $translation = ConvertFrom-Base64Utf8 -Value $parts[1] -Path $Path -LineNumber $lineNumber
        $rows.Add([PSCustomObject]@{
            original = $original
            translation = $translation
        })
    }

    return $rows.ToArray()
}

function Read-EditableJson {
    param([string]$Path)

    $json = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $data = $json | ConvertFrom-Json
    if ($data.PSObject.Properties.Name -contains "entries") {
        return @($data.entries)
    }

    return @($data)
}

function Write-EditableJson {
    param(
        [string]$TsvPath,
        [object[]]$Rows
    )

    $jsonPath = Get-EditJsonPath -TsvPath $TsvPath
    $payload = [PSCustomObject]@{
        help = "Правьте только поле translation. Поле original нужно оставлять как есть, иначе игра не найдет исходную C# строку."
        source = "HardcodedStrings.tsv"
        entries = $Rows
    }

    $json = $payload | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText($jsonPath, $json + [Environment]::NewLine, $Utf8NoBom)
    Write-Info ("decoded: {0} -> {1}" -f (Get-RelativePath $TsvPath), (Get-RelativePath $jsonPath))
}

function Write-HardcodedTsv {
    param(
        [string]$TsvPath,
        [object[]]$Rows
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# base64_utf8_original`tbase64_utf8_russian")
    foreach ($row in $Rows) {
        if ($null -eq $row.original -or $null -eq $row.translation) {
            throw "Every JSON entry must have original and translation fields."
        }

        $original = [string]$row.original
        $translation = [string]$row.translation
        if ([string]::IsNullOrEmpty($original)) {
            throw "JSON entry has an empty original field."
        }

        $lines.Add((ConvertTo-Base64Utf8 $original) + "`t" + (ConvertTo-Base64Utf8 $translation))
    }

    [System.IO.File]::WriteAllLines($TsvPath, [string[]]$lines, $Utf8NoBom)
    Write-Info ("encoded: {0} -> {1}" -f (Get-RelativePath (Get-EditJsonPath $TsvPath)), (Get-RelativePath $TsvPath))
}

function Test-Rows {
    param(
        [string]$Label,
        [object[]]$Rows
    )

    $errors = New-Object System.Collections.Generic.List[string]
    for ($i = 0; $i -lt $Rows.Count; $i++) {
        $row = $Rows[$i]
        $rowNumber = $i + 1
        if ($null -eq $row.original -or [string]::IsNullOrEmpty([string]$row.original)) {
            $errors.Add("$Label entry $rowNumber has empty original.")
            continue
        }
        if ($null -eq $row.translation) {
            $errors.Add("$Label entry $rowNumber has no translation.")
        }
    }

    return $errors.ToArray()
}

function Show-Help {
    Write-Host ''
    Write-Host 'HardcodedStringsTool.ps1'
    Write-Host ''
    Write-Host 'Простая схема:'
    Write-Host '  1. decode   - создать читаемый HardcodedStrings.edit.json'
    Write-Host '  2. править translation в JSON через VS Code'
    Write-Host '  3. encode   - собрать обратно HardcodedStrings.tsv для игры'
    Write-Host '  4. validate - проверить файлы'
    Write-Host ''
    Write-Host 'Команды:'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 list'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 decode'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 encode'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 validate'
    Write-Host ''
    Write-Host 'Только один мод:'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 decode -Mod Nanotech_Overpower_RU_Full'
    Write-Host '  .\Tools\HardcodedStringsTool.ps1 encode -Mod Nanotech_Overpower_RU_Full'
    Write-Host ''
}

if ($Action -eq "help") {
    Show-Help
    exit 0
}

$files = Find-HardcodedFiles
if ($files.Count -eq 0) {
    Write-Info "No HardcodedStrings.tsv files found."
    exit 0
}

$allErrors = New-Object System.Collections.Generic.List[string]

foreach ($tsvPath in $files) {
    $jsonPath = Get-EditJsonPath -TsvPath $tsvPath

    if ($Action -eq "list") {
        $status = if (Test-Path -LiteralPath $jsonPath) { "edit json exists" } else { "no edit json yet" }
        Write-Info ("{0} ({1})" -f (Get-RelativePath $tsvPath), $status)
        continue
    }

    if ($Action -eq "decode") {
        $rows = Read-HardcodedTsv -Path $tsvPath
        Write-EditableJson -TsvPath $tsvPath -Rows $rows
        continue
    }

    if ($Action -eq "encode") {
        if (-not (Test-Path -LiteralPath $jsonPath)) {
            throw ("Missing editable JSON: {0}. Run decode first." -f (Get-RelativePath $jsonPath))
        }
        $rows = Read-EditableJson -Path $jsonPath
        $errors = Test-Rows -Label (Get-RelativePath $jsonPath) -Rows $rows
        if ($errors.Count -gt 0) {
            foreach ($err in $errors) { $allErrors.Add($err) }
            continue
        }
        Write-HardcodedTsv -TsvPath $tsvPath -Rows $rows
        continue
    }

    if ($Action -eq "validate") {
        try {
            $tsvRows = Read-HardcodedTsv -Path $tsvPath
            foreach ($err in (Test-Rows -Label (Get-RelativePath $tsvPath) -Rows $tsvRows)) {
                $allErrors.Add($err)
            }
            $relativeTsv = Get-RelativePath $tsvPath
            Write-Info ("valid tsv: " + $relativeTsv + " - " + $tsvRows.Count + " entries")
        }
        catch {
            $allErrors.Add($_.Exception.Message)
        }

        if (Test-Path -LiteralPath $jsonPath) {
            try {
                $jsonRows = Read-EditableJson -Path $jsonPath
                foreach ($err in (Test-Rows -Label (Get-RelativePath $jsonPath) -Rows $jsonRows)) {
                    $allErrors.Add($err)
                }
                $relativeJson = Get-RelativePath $jsonPath
                Write-Info ("valid json: " + $relativeJson + " - " + $jsonRows.Count + " entries")
            }
            catch {
                $allErrors.Add($_.Exception.Message)
            }
        }
    }
}

if ($allErrors.Count -gt 0) {
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($err in $allErrors) {
        Write-Host "  - $err" -ForegroundColor Red
    }
    exit 1
}

if ($Action -eq "validate") {
    Write-Info "All checks passed."
}
