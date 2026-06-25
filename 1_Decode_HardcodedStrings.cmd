@echo off
setlocal
cd /d "%~dp0"

echo.
echo === Decode HardcodedStrings.tsv to editable JSON ===
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\HardcodedStringsTool.ps1" decode

echo.
if errorlevel 1 (
  echo Decode failed. Read the error above.
) else (
  echo Done.
  echo Now edit HardcodedStrings.edit.json files in VS Code.
  echo When finished, run 2_Encode_HardcodedStrings.cmd.
)
echo.
pause
