#!/bin/bash

# 1. 设置基础发布参数
CONFIG="Release"
SINGLE_FILE="true"
SELF_CONTAINED="false"
NATIVE_LIBS="true"
OUTPUT_BASE="./publish"

# 2. 定义需要发布的目标平台列表 (RID)
ALL_PLATFORMS="win-x64 win-x86 win-arm64"

echo "========================================"
echo "  .NET 10 单文件发布脚本 (依赖框架版)"
echo "========================================"

# 3. 判断是否有命令行传参
if [ -z "$1" ] || [ "$1" == "all" ]; then
    echo "[INFO] 将发布所有平台..."
    TARGET_PLATFORMS=$ALL_PLATFORMS
else
    echo "[INFO] 仅发布指定平台: $1"
    TARGET_PLATFORMS="$1"
fi

# 4. 循环遍历目标平台并执行发布
for PLATFORM in $TARGET_PLATFORMS; do
    echo ""
    echo "[INFO] 正在发布平台: $PLATFORM ..."
    
    dotnet publish -c $CONFIG -r $PLATFORM --self-contained $SELF_CONTAINED \
        /p:PublishSingleFile=$SINGLE_FILE \
        /p:IncludeNativeLibrariesForSelfExtract=$NATIVE_LIBS \
        -o $OUTPUT_BASE/$PLATFORM
        
    if [ $? -ne 0 ]; then
        echo "[ERROR] 平台 $PLATFORM 发布失败！"
        exit 1
    else
        echo "[SUCCESS] 平台 $PLATFORM 发布成功！"
    fi
done

echo ""
echo "========================================"
echo "  所有指定平台发布完成！"
echo "  输出目录: $OUTPUT_BASE"
echo "========================================"