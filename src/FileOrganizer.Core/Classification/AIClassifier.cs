using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FileOrganizer.Core.Classification;

/// <summary>Результат ИИ-классификации документа.</summary>
public sealed record AIClassification(string DocumentType, string? Project, string? Client, string? Topic, int? Year, double Confidence);

/// <summary>
/// Подключаемый ИИ-классификатор. Получает только минимум данных: имя файла и короткий фрагмент текста.
/// Используется лишь когда обычных правил недостаточно и пользователь явно включил «Умный анализ содержимого».
/// </summary>
public interface IAIClassifier
{
    string Name { get; }
    /// <summary>Данные уходят за пределы компьютера (облако). Для таких классификаторов требуется отдельное согласие.</summary>
    bool IsRemote { get; }
    Task<AIClassification?> ClassifyAsync(string fileName, string? textExcerpt, CancellationToken ct);
}

/// <summary>Классификатор-заглушка: ИИ выключен.</summary>
public sealed class DisabledAIClassifier : IAIClassifier
{
    public string Name => "Выключен";
    public bool IsRemote => false;
    public Task<AIClassification?> ClassifyAsync(string fileName, string? textExcerpt, CancellationToken ct) => Task.FromResult<AIClassification?>(null);
}

/// <summary>
/// Локальная модель через Ollama (http://localhost:11434). Документы не покидают компьютер.
/// </summary>
public sealed class OllamaClassifier : IAIClassifier
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly int _maxChars;

    public OllamaClassifier(HttpClient http, string model = "llama3.1", int maxChars = 2000)
    {
        _http = http;
        _model = model;
        _maxChars = maxChars;
        if (_http.BaseAddress == null) _http.BaseAddress = new Uri("http://localhost:11434");
    }

    public string Name => $"Ollama ({_model})";
    public bool IsRemote => !(_http.BaseAddress!.IsLoopback);

    public async Task<AIClassification?> ClassifyAsync(string fileName, string? textExcerpt, CancellationToken ct)
    {
        var excerpt = textExcerpt == null ? "" : textExcerpt.Length > _maxChars ? textExcerpt[.._maxChars] : textExcerpt;
        var prompt =
            "Ты классифицируешь файлы пользователя. Ответь строго JSON без пояснений с полями: " +
            "documentType (Договор, Счёт, Акт, Отчёт, Коммерческое предложение, Протокол, Инструкция, Резюме, Презентация, Фото, Прочее), " +
            "project (строка или null), client (строка или null), topic (строка или null), year (число или null), confidence (0..1).\n" +
            $"Имя файла: {fileName}\nФрагмент текста:\n{excerpt}";
        var req = new OllamaRequest(_model, prompt, false, "json");
        using var resp = await _http.PostAsJsonAsync("/api/generate", req, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<OllamaResponse>(cancellationToken: ct).ConfigureAwait(false);
        if (body?.Response == null) return null;
        return Parse(body.Response);
    }

    internal static AIClassification? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;
            string? Str(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String && v.GetString() is { Length: > 0 } s ? s : null;
            int? year = r.TryGetProperty("year", out var y) && y.ValueKind == JsonValueKind.Number ? y.GetInt32() : null;
            double conf = r.TryGetProperty("confidence", out var c) && c.ValueKind == JsonValueKind.Number ? Math.Clamp(c.GetDouble(), 0, 1) : 0.5;
            var type = Str("documentType");
            if (type == null) return null;
            return new AIClassification(type, Str("project"), Str("client"), Str("topic"), year, conf);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record OllamaRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format);

    private sealed record OllamaResponse([property: JsonPropertyName("response")] string? Response);
}
