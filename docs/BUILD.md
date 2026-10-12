# Сборка и выпуск

## Требования

* .NET 8 SDK (8.0.100 и новее) — https://dotnet.microsoft.com/download/dotnet/8.0
* Windows 10/11 для запуска приложения. Собрать можно и в Linux/macOS: WPF‑проект собирается для Windows
  благодаря `EnableWindowsTargeting` (нужен SDK от Microsoft — в пакетах некоторых дистрибутивов, собранных из исходников,
  нет `Microsoft.NET.Sdk.WindowsDesktop`).

## Команды

```powershell
dotnet restore
dotnet build FileOrganizer.sln -c Release
dotnet test tests/FileOrganizer.Core.Tests -c Release --filter "Category!=Stress"   # быстрые тесты
dotnet test tests/FileOrganizer.Core.Tests -c Release --filter "Category=Stress"    # 100 000+ файлов, файл 11 ГБ

# Один exe без установки .NET:
dotnet publish src/FileOrganizer.App -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true `
  -o artifacts/FileOrganizer-win-x64
```

Или целиком: `build\build.ps1` (Windows) / `build/build.sh` (Linux, macOS).

| Артефакт | Размер | Требования |
|---|---|---|
| `artifacts/FileOrganizer-win-x64/FileOrganizer.exe` | ≈ 70 МБ | ничего |
| `artifacts/FileOrganizer-win-x64-fx/FileOrganizer.exe` | ≈ 8 МБ | .NET 8 Desktop Runtime |
| `artifacts/fo-cli-win-x64/fo.exe` | ≈ 37 МБ | ничего |

Для ARM‑устройств замените `win-x64` на `win-arm64`.

## CI

`.github/workflows/build.yml`: на каждый push — тесты на `windows-latest` и публикация сборок (артефакт
`FileOrganizer-win-x64`). Нагрузочные тесты — вручную (`workflow_dispatch`).

## Запуск из исходников

```powershell
dotnet run --project src/FileOrganizer.App
dotnet run --project src/FileOrganizer.Cli -- analyze "C:\Users\me\Downloads"
```

Переменная окружения `FILEORGANIZER_DATA` задаёт отдельную папку данных (удобно для экспериментов).

## Длинные пути

Приложение объявляет `longPathAware` в манифесте. Чтобы Windows разрешила пути длиннее 260 символов,
должна быть включена политика «Включить длинные пути Win32» (`HKLM\SYSTEM\CurrentControlSet\Control\FileSystem\LongPathsEnabled = 1`;
в Windows 11 обычно уже включена).
