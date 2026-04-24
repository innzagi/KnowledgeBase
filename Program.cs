var builder = WebApplication.CreateBuilder(args); // создает приложение

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// подключает свагер 
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(); // Если проект запущен локально, показывать свагер
}

app.UseHttpsRedirection(); // перенаправление на https 
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowFrontend");

app.MapGet("/health", () =>  // создаёт endpoint /health
{
    return "ok";
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




