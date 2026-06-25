@echo off
setlocal
cd /d "%~dp0"

echo.
echo === Validate HardcodedStrings files ===
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\HardcodedStringsTool.ps1" validate

echo.
if errorlevel 1 (
  echo Validation failed. Read the error above.
) else (
  echo All checks passed.
)
echo.
pause
