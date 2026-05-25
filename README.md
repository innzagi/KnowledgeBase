# KnowledgeBase

Школьная база знаний по биологии с ИИ-ассистентом на основе GigaChat.

## Настройка ИИ-ассистента

Ассистент работает через API GigaChat (Сбер). Чтобы он начал отвечать на
вопросы, нужно зарегистрировать приложение в Сбер Studio, получить
авторизационный ключ и положить его в User Secrets проекта.

### 1. Получить Authorization key в Сбер Studio

1. Открыть https://developers.sber.ru и войти по Сбер ID.
2. Создать рабочее пространство → проект **GigaChat API**.
3. Выбрать тариф **Freemium для физических лиц** — карта не нужна, выдаётся
   бесплатный лимит токенов.
4. На странице проекта скопировать **Authorization key** — длинная строка
   вида `NjE2ZDQ4...ZGI=`. Это уже Base64 от `client_id:client_secret`,
   собирать его руками не нужно.

### 2. Положить ключ в User Secrets

В терминале, из корня проекта (`/Users/pigeonization/RiderProjects/KnowledgeBase`):

```bash
dotnet user-secrets set "Gigachat:AuthKey" "СЮДА_AUTHORIZATION_KEY"
dotnet user-secrets set "Gigachat:IgnoreTlsErrors" "true"
```

Если `dotnet` не найден в PATH, используйте полный путь:
`/usr/local/share/dotnet/dotnet`.

Проверить, что ключ сохранился:

```bash
dotnet user-secrets list
```

User Secrets хранятся в `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`
и **не попадают в git**.

### 3. Запустить проект

```bash
dotnet run
```

Открыть фронтенд, задать вопрос. На первый запрос будет небольшая задержка —
бэкенд получает OAuth-токен GigaChat. Токен кэшируется в памяти приложения
на ~30 минут и обновляется автоматически.

### 4. (Опционально) Убрать `IgnoreTlsErrors`

`Gigachat:IgnoreTlsErrors=true` отключает проверку TLS-сертификата сервера
GigaChat. Это нужно потому, что сервер использует сертификат российского УЦ
Минцифры, которого нет в стандартном trust store macOS.

Для разработки этого достаточно. В продакшене сертификат нужно установить
правильно:

1. Скачать `russiantrustedca.pem` с https://www.gosuslugi.ru/crt
2. В **Keychain Access** → **File → Import Items** → выбрать файл.
3. Дважды кликнуть по импортированному сертификату → раскрыть **Trust** →
   поставить **Always Trust**.
4. Удалить флаг:
   ```bash
   dotnet user-secrets remove "Gigachat:IgnoreTlsErrors"
   ```

## Конфигурационные ключи

| Ключ                       | Назначение                                                  | По умолчанию          |
|----------------------------|-------------------------------------------------------------|-----------------------|
| `Gigachat:AuthKey`         | Authorization key из Сбер Studio (обязательно).             | —                     |
| `Gigachat:Scope`           | Область доступа: `GIGACHAT_API_PERS` для физлиц.            | `GIGACHAT_API_PERS`   |
| `Gigachat:Model`           | Имя модели GigaChat.                                        | `GigaChat`            |
| `Gigachat:IgnoreTlsErrors` | Отключить проверку TLS (только для локальной разработки).   | `false`               |

Все ключи можно задавать как через User Secrets, так и через
`appsettings.json` / переменные окружения. **Не коммитьте `AuthKey` в git** —
храните его только в User Secrets или в переменных окружения сервера.

## Системный промпт

Поведение ассистента (тон, тематика) задаётся system-промптом в `Program.cs`
в обработчике `POST /ask`. По умолчанию ассистент представляется учебным
помощником по биологии и отказывается отвечать на вопросы вне темы.
Поправьте текст промпта, если хотите изменить характер ассистента.
