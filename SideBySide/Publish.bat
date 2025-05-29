@echo off
setlocal enabledelayedexpansion

REM --- CONFIGURATION ---
set BASENAME=SideBySide
set PROJECT=%BASENAME%.csproj
set RUNTIMES=win-x64 linux-x64 linux-arm64 osx-arm64 osx-x64
set CONFIG=Release
set OUTPUT=Publish
set EXENAME=SideBySide

REM --- GET VERSION FROM .csproj ---
findstr /i "<Version>" "%PROJECT%" > _ver.tmp
set /p fullline=<_ver.tmp
del _ver.tmp
set "str1=%fullline:*<Version>=%"
set "version_raw=%str1:</Version>=%"

for /f "tokens=1,2,3,4 delims=." %%a in ("%version_raw%") do (
    set "major=%%a"
    set "minor=%%b"
    set "build=%%c"
    set "revision=%%d"
)

set "version=%major%.%minor%.%revision%"
set /a buildCheck=%build% + 0
if !buildCheck! gtr 0 (
    set "version=%version%-pre%build%"
)

if not defined version (
    echo.
    echo Error: Could not extract version from %PROJECT%.
    pause
    exit /b 1
)

echo.
echo Publishing version: %version%

REM --- PUBLISH LOOP ---
for %%r in (%RUNTIMES%) do (
    echo.
    echo Publishing for runtime: %%r
    echo.

    dotnet publish "%PROJECT%" -c %CONFIG% -r %%r --self-contained false ^
        /p:PublishSingleFile=true /p:PublishTrimmed=false /p:IncludeNativeLibrariesForSelfExtract=false ^
        -o "%OUTPUT%\%%r"
    if errorlevel 1 (
        echo Error during publishing for %%r
        exit /b 1
    )

    pushd "%OUTPUT%\%%r"

    REM --- CLEANUP ---
    del *.pdb >nul 2>&1

    REM --- RENAME OUTPUT ---
    if exist "%BASENAME%.exe" (
        move "%BASENAME%.exe" "..\%EXENAME%-%version%-%%r.exe"
    ) else (
        move "%BASENAME%" "..\%EXENAME%-%version%-%%r"
    )

	REM --- PACKAGE NON-WINDOWS ---
	if /i not "%%r"=="win-x64" (
		for %%f in (*.so *.dylib) do (
			set "ZNAME=%EXENAME%-%version%-%%r.zip"
			copy /y "%%f" "..\%%f" >nul
			pushd ..
			powershell -Command "Compress-Archive -Path '%EXENAME%-%version%-%%r','%%f' -DestinationPath '!ZNAME!' -CompressionLevel Optimal -Force"
			del "%%f" >nul
			del "%EXENAME%-%version%-%%r" >nul
			popd
		)
	)

    popd
)

echo.
echo All runtimes published and packaged.
timeout 30
