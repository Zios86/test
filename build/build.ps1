# Сборка на Windows: тесты + готовые исполняемые файлы в папке artifacts\
# Требуется .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

dotnet test tests/FileOrganizer.Core.Tests -c Release --filter "Category!=Stress"

# 1) Один .exe, не требует установленного .NET (≈ 70 МБ)
dotnet publish src/FileOrganizer.App -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
    -o artifacts/FileOrganizer-win-x64

# 2) Компактная версия: нужен .NET 8 Desktop Runtime
dotnet publish src/FileOrganizer.App -c Release -r win-x64 --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishSingleFile=true -o artifacts/FileOrganizer-win-x64-fx

# 3) Консольная версия
dotnet publish src/FileOrganizer.Cli -c Release -r win-x64 --self-contained `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/fo-cli-win-x64

Write-Host "Готово: $root\artifacts"
