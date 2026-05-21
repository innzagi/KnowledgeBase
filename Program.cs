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
app.MapPost("/ask", (AskRequest request) =>
{
    var knowledgeBase = new Dictionary<string, string>
    {
        { "митоз", "Митоз — это процесс деления клетки, при котором из одной клетки образуются две генетически одинаковые дочерние клетки." },
        { "мейоз", "Мейоз — это деление клетки, при котором число хромосом уменьшается вдвое." },
        { "клетка", "Клетка — это элементарная структурная и функциональная единица живого организма." }
    };

    var question = request.Question.ToLower();

    foreach (var item in knowledgeBase)
    {
        if (question.Contains(item.Key))
        {
            return Results.Ok(item.Value);
        }
    }

    return Results.Ok("Я пока не нашёл ответ в базе знаний.");
});

app.Run();

public class AskRequest
{
    public string Question { get; set; } = "";
}




