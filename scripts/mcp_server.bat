@echo off
setlocal
where py >nul 2>nul
if %ERRORLEVEL%==0 (
  set "PYLAUNCHER=py -3"
) else (
  where python >nul 2>nul
  if %ERRORLEVEL% NEQ 0 (
    echo Python not found on PATH. Install Python 3 and try again.
    exit /b 1
  )
  set "PYLAUNCHER=python"
)
set "WD=%~dp0.."
pushd "%WD%"
%PYLAUNCHER% -m scripts.mcp_server
set ERR=%ERRORLEVEL%
popd
exit /b %ERR%

