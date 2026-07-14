param(
    [string]$Platform = "all"
)

# 1. 设置基础发布参数
$Config = "Release"
$SingleFile = "true"
$SelfContained = "false"
$NativeLibs = "true"
$OutputBase = "./publish"

# 2. 定义需要发布的目标平台列表 (RID)
$AllPlatforms = @("win-x64", "win-x86", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64")

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  .NET 10 单文件发布脚本 (依赖框架版)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# 3. 判断目标平台
if ($Platform -eq "all") {
    Write-Host "[INFO] 将发布所有平台..." -ForegroundColor Yellow
    $TargetPlatforms = $AllPlatforms
} else {
    Write-Host "[INFO] 仅发布指定平台: $Platform" -ForegroundColor Yellow
    $TargetPlatforms = @($Platform)
}

# 4. 循环遍历目标平台并执行发布
foreach ($p in $TargetPlatforms) {
    Write-Host ""
    Write-Host "[INFO] 正在发布平台: $p ..." -ForegroundColor Green
    
    dotnet publish -c $Config -r $p --self-contained $SelfContained `
        /p:PublishSingleFile=$SingleFile `
        /p:IncludeNativeLibrariesForSelfExtract=$NativeLibs `
        -o "$OutputBase/$p"
        
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] 平台 $p 发布失败！" -ForegroundColor Red
        exit 1
    } else {
        Write-Host "[SUCCESS] 平台 $p 发布成功！" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  所有指定平台发布完成！" -ForegroundColor Cyan
Write-Host "  输出目录: $OutputBase" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan