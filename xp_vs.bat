@echo off
REM Living Dev Agent XP System - Visual Studio Integration
set PYTHON_CMD=python
set XP_SCRIPT=.\template\src\DeveloperExperience\dev_experience.py

if "%1"=="profile" (
    %PYTHON_CMD% "%XP_SCRIPT%" --profile %USERNAME%
) else if "%1"=="debug" (
    %PYTHON_CMD% "%XP_SCRIPT%" --record %USERNAME% debugging_session excellent "Visual Studio debugging session" --metrics "ide:visual_studio"
) else if "%1"=="leaderboard" (
    %PYTHON_CMD% "%XP_SCRIPT%" --leaderboard
) else (
    echo Usage: xp_vs.bat [profile^|debug^|leaderboard]
)
