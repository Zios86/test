#!/usr/bin/env bash
# Сборка из Linux/macOS: ядро тестируется, WPF-приложение собирается для Windows (EnableWindowsTargeting).
# Нужен .NET 8 SDK от Microsoft (в SDK из репозиториев дистрибутивов может не быть Microsoft.NET.Sdk.WindowsDesktop).
set -euo pipefail
cd "$(dirname "$0")/.."

dotnet test tests/FileOrganizer.Core.Tests -c Release --filter "Category!=Stress"

dotnet publish src/FileOrganizer.App -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o artifacts/FileOrganizer-win-x64

dotnet publish src/FileOrganizer.App -c Release -r win-x64 --self-contained false -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:PublishSingleFile=true -o artifacts/FileOrganizer-win-x64-fx

dotnet publish src/FileOrganizer.Cli -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o artifacts/fo-cli-win-x64

echo "Готово: $(pwd)/artifacts"
