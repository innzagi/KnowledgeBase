using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args); // создает приложение

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// HttpClient для GigaChat. На время разработки можно отключить проверку TLS
// (флаг Gigachat:IgnoreTlsErrors=true), пока не установлен корневой сертификат
// Минцифры в системное хранилище.
builder.Services.AddHttpClient("gigachat")
    .ConfigurePrimaryHttpMessageHandler(sp =>
    {
        var cfg = sp.GetRequiredService<IConfiguration>();
        var handler = new HttpClientHandler();
        if (cfg.GetValue<bool>("Gigachat:IgnoreTlsErrors"))
        {
            handler.ServerCertificateCustomValidationCallback =
                (_, _, _, _) => true;
        }
        return handler;
    });

builder.Services.AddSingleton<GigaChatTokenCache>();

var app = builder.Build(); // сборка приложения

app.UseHttpsRedirection(); // перенаправление на https
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowFrontend");

app.MapGet("/health", () =>  // создаёт endpoint /health
{
    return "ok";
});
app.MapGet("/articles/botany/tissues", () =>
{
    return """
           Ткани растений

           Ткань — это группа клеток, сходных по строению и функциям.

           1. Образовательные ткани (меристемы)
           Обеспечивают рост растения.
           - Верхушечные (рост в длину)
           - Боковые (рост в толщину)
           - Вставочные
           - Раневые

           2. Покровные ткани
           Защищают растение.
           - Эпидерма (кожица)
           - Ризодерма
           - Пробка

           3. Проводящие ткани
           Переносят вещества.
           - Ксилема — вода и минералы вверх
           - Флоэма — органические вещества

           4. Механические ткани
           Придают прочность.
           - Колленхима (живая)
           - Склеренхима (мёртвая)

           5. Основные ткани (паренхима)
           - Фотосинтезирующая
           - Запасающая
           - Воздухоносная
           - Водоносная
           """;
});
app.MapPost("/ask", async (
    AskRequest request,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    GigaChatTokenCache tokenCache) =>
{
    var userMessages = request.Messages?
        .Where(m => !string.IsNullOrWhiteSpace(m.Content))
        .ToList() ?? new List<ChatMessage>();

    if (userMessages.Count == 0)
    {
        return Results.Text("Введите вопрос.");
    }

    var authKey = config["Gigachat:AuthKey"];
    if (string.IsNullOrWhiteSpace(authKey))
    {
        return Results.Text(
            "Авторизационные данные GigaChat не настроены. Выполните: " +
            "dotnet user-secrets set \"Gigachat:AuthKey\" \"<Authorization key из Сбер Studio>\"");
    }

    var scope = config["Gigachat:Scope"] ?? "GIGACHAT_API_PERS";
    var model = config["Gigachat:Model"] ?? "GigaChat";

    var messagesForApi = new List<object>
    {
        new
        {
            role = "system",
            content = "Ты — учебный ассистент по биологии для школьной базы знаний. " +
                      "Отвечай по-русски, кратко и понятно школьнику. " +
                      "Если вопрос не по биологии — вежливо скажи, что отвечаешь только по биологии."
        }
    };
    messagesForApi.AddRange(userMessages.Select(m => (object)new { role = m.Role, content = m.Content }));

    var http = httpFactory.CreateClient("gigachat");
    try
    {
        var token = await tokenCache.GetTokenAsync(http, authKey, scope);

        var chatReq = new HttpRequestMessage(
            HttpMethod.Post,
            "https://gigachat.devices.sberbank.ru/api/v1/chat/completions");
        chatReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        chatReq.Content = JsonContent.Create(new
        {
            model,
            messages = messagesForApi
        });

        var response = await http.SendAsync(chatReq);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return Results.Text($"Ошибка GigaChat ({(int)response.StatusCode}): {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<GigaChatResponse>();
        var text = json?.Choices?.FirstOrDefault()?.Message?.Content;
        return Results.Text(string.IsNullOrWhiteSpace(text) ? "Пустой ответ от модели." : text);
    }
    catch (Exception ex)
    {
        return Results.Text($"Не удалось обратиться к GigaChat: {ex.Message}");
    }
});

app.Run();

public class AskRequest
{
    public List<ChatMessage>? Messages { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = "";
}

// Кэширует access_token GigaChat (он живёт ~30 минут), чтобы не запрашивать его на каждый вопрос.
public class GigaChatTokenCache
{
    private string? _token;
    private DateTimeOffset _expiresAt;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<string> GetTokenAsync(HttpClient http, string authKey, string scope)
    {
        await _lock.WaitAsync();
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAt.AddMinutes(-1))
            {
                return _token;
            }

            var req = new HttpRequestMessage(
                HttpMethod.Post,
                "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
            req.Headers.Add("Authorization", $"Basic {authKey}");
            req.Headers.Add("RqUID", Guid.NewGuid().ToString());
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", scope)
            });

            var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"OAuth GigaChat не прошёл ({(int)resp.StatusCode}): {body}");
            }

            using var stream = await resp.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);
            _token = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresMs = doc.RootElement.GetProperty("expires_at").GetInt64();
            _expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresMs);
            return _token;
        }
        finally
        {
            _lock.Release();
        }
    }
}

public class GigaChatResponse
{
    [JsonPropertyName("choices")]
    public List<GigaChatChoice>? Choices { get; set; }
}

public class GigaChatChoice
{
    [JsonPropertyName("message")]
    public GigaChatMessage? Message { get; set; }
}

public class GigaChatMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
