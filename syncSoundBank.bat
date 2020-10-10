@echo off
REM This script rebuilds FMOD studio project (located at ./FSUnity)
REM and then updates the sound bank in project.

where /q fmodstudio
if ERRORLEVEL 1 (
    echo Can't find fmodstudio in PATH.
) else (
    echo Building FMOD project...
    fmodstudio -build ./FMOD/ld47.fspro

    echo Syncing files...
    robocopy /s ./FMOD/Build/ ./Assets/Content/Sound/ /MIR
)

@echo on