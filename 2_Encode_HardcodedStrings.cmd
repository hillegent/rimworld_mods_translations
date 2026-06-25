@echo off
setlocal
cd /d "%~dp0"

echo.
echo === Encode editable JSON back to HardcodedStrings.tsv ===
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\HardcodedStringsTool.ps1" encode

echo.
if errorlevel 1 (
  echo Encode failed. Read the error above.
) else (
  echo Done.
  echo Now run 3_Validate_HardcodedStrings.cmd.
)
echo.
pause
