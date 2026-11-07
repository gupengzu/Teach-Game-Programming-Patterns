@echo off
chcp 65001 >nul
setlocal

cd /d "%~dp0"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [错误] 没有找到 dotnet 命令。
    echo.
    echo 请安装 .NET 8 SDK：https://dotnet.microsoft.com/download
    echo 或者到仓库 Releases 页面下载对应平台的可执行文件，解压后双击运行，无需安装。
    echo.
    pause
    exit /b 1
)

echo 正在编译并运行...
dotnet run -c Release
if errorlevel 1 pause
