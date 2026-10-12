namespace FileOrganizer.Core.Data;

public enum AppTheme { System, Light, Dark }

public enum DeleteMode
{
    /// <summary>В папку FileOrganizer_Recovery (по умолчанию, полностью обратимо из программы).</summary>
    RecoveryFolder,
    /// <summary>В Корзину Windows (восстановление — средствами Windows).</summary>
    RecycleBin,
}

/// <summary>Настройки приложения (хранятся в SQLite в виде JSON).</summary>
public sealed class AppSettings
{
    /// <summary>Режим «Только посмотреть»: программа не изменяет файловую систему. Включён при первом запуске.</summary>
    public bool SimulationMode { get; set; } = true;
    public AppTheme Theme { get; set; } = AppTheme.System;
    public DeleteMode DeleteMode { get; set; } = DeleteMode.RecoveryFolder;
    public bool IncludeHidden { get; set; }
    public int OldFilesDays { get; set; } = 1095;
    public int LargeFileMb { get; set; } = 1024;
    /// <summary>Минимальная уверенность, при которой предложение отмечается автоматически.</summary>
    public double AutoSelectThreshold { get; set; } = 0.85;
    public bool TopLevelOnly { get; set; } = true;
    public string? LastFolder { get; set; }
    public List<string> RecentFolders { get; set; } = new();

    /// <summary>Умный анализ содержимого документов (локально).</summary>
    public bool ContentAnalysisEnabled { get; set; }
    /// <summary>Использовать ИИ (только если правил недостаточно).</summary>
    public bool AIEnabled { get; set; }
    public string AIEndpoint { get; set; } = "http://localhost:11434";
    public string AIModel { get; set; } = "llama3.1";
    /// <summary>Согласие на передачу фрагментов текста во внешний (не локальный) ИИ-сервис.</summary>
    public bool AllowRemoteAI { get; set; }
    public int InboxCheckMinutes { get; set; } = 60;

    /// <summary>Пользовательская таблица типов: категория → расширения. Пусто — встроенная таблица.</summary>
    public Dictionary<string, string[]>? CustomCategories { get; set; }
}
