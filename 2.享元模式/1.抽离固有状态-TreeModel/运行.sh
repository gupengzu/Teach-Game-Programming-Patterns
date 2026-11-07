#!/usr/bin/env bash
set -e

if ! command -v dotnet >/dev/null 2>&1; then
  echo "[错误] 没有找到 dotnet 命令。"
  echo
  echo "请安装 .NET 8 SDK：https://dotnet.microsoft.com/download"
  echo "或者到仓库 Releases 页面下载对应平台的可执行文件，解压后运行，无需安装。"
  exit 1
fi

cd "$(dirname "$0")"
dotnet run -c Release
