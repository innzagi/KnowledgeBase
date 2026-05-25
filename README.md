# KnowledgeBase

Школьная база знаний по биологии с ИИ-ассистентом на GigaChat.

## Ключ GigaChat
Создать проект **GigaChat API (Freemium)** на https://developers.sber.ru → скопировать **Authorization key**.

## Запуск (выполнять в той же сессии терминала, где запускаете `dotnet run`)

**macOS / Linux (bash/zsh):**
```bash
export Gigachat__AuthKey="AUTHORIZATION_KEY" Gigachat__IgnoreTlsErrors=true
dotnet run
```

**Windows (PowerShell):**
```powershell
$env:Gigachat__AuthKey="AUTHORIZATION_KEY"; $env:Gigachat__IgnoreTlsErrors="true"
dotnet run
```
