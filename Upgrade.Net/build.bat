@echo off
chcp 65001 >nul
echo ========================================
echo   .NET 10 单文件发布脚本 (依赖框架版)
echo ========================================

:: 1. 设置基础发布参数
set CONFIG=Release
set SINGLE_FILE=true
set SELF_CONTAINED=false
set NATIVE_LIBS=true
set OUTPUT_BASE=./publish

:: 2. 定义需要发布的目标平台列表 (RID)
set "ALL_PLATFORMS=win-x64 win-x86 win-arm64 linux-x64 linux-arm64 osx-x64 osx-arm64"

:: 3. 判断是否有命令行传参
if "%~1"=="" (
    echo [INFO] 未指定平台，将发布所有平台...
    set "TARGET_PLATFORMS=%ALL_PLATFORMS%"
) else if /i "%~1"=="all" (
    echo [INFO] 收到 all 参数，将发布所有平台...
    set "TARGET_PLATFORMS=%ALL_PLATFORMS%"
) else (
    echo [INFO] 仅发布指定平台: %~1
    set "TARGET_PLATFORMS=%~1"
)

:: 4. 循环遍历目标平台并执行发布
for %%P in (%TARGET_PLATFORMS%) do (
    echo.
    echo [INFO] 正在发布平台: %%P ...
    
    dotnet publish -c %CONFIG% -r %%P --self-contained %SELF_CONTAINED% ^
        /p:PublishSingleFile=%SINGLE_FILE% ^
        /p:IncludeNativeLibrariesForSelfExtract=%NATIVE_LIBS% ^
        -o %OUTPUT_BASE%/%%P
        
    if %errorlevel% neq 0 (
        echo [ERROR] 平台 %%P 发布失败！
        pause
        exit /b 1
    ) else (
        echo [SUCCESS] 平台 %%P 发布成功！
    )
)

echo.
echo ========================================
echo   所有指定平台发布完成！
echo   输出目录: %OUTPUT_BASE%
echo ========================================
pause