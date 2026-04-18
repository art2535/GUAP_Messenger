# Мессенджер для ГУАП

<div align="center">
  <img src="https://upload.wikimedia.org/wikipedia/commons/3/3e/GUAP_logo.svg" alt="Логотип ГУАП" width="180">
  <br><br>
  <strong>GUAP Messenger</strong> — корпоративный мессенджер<br>
  для студентов, преподавателей и сотрудников ГУАП
  <br><br>
  <strong>Реальное время · Защищённая аутентификация · Только для университета</strong>
  <br><br>
</div>

[![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-blue)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/License-Не%20определена-lightgrey)](LICENSE)
[![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-2088FF)](https://github.com/art2535/GUAP_Messenger/actions)
[![Методология](https://img.shields.io/badge/Методология-Waterfall-orange)](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D1%81%D1%81%D1%8B)

**GUAP Messenger** — современное веб-приложение для обмена сообщениями в реальном времени, разработанное специально для сообщества **Государственного университета аэрокосмического приборостроения (ГУАП)**.

Проект реализуется по классической каскадной модели (**Waterfall**).  
**Заказчик** — ГУАП.  
**Дата старта проекта:** 17 сентября 2025 года.

## Основной функционал

### Реализовано
- Личные (1:1) и групповые чаты
- Отправка текстовых сообщений и файлов
- Уведомления и обновления в реальном времени через **SignalR**
- Индикатор «Печатает…» (typing)
- Онлайн-статус пользователей
- Аутентификация через **OIDC SSO ГУАП** (Keycloak)
- Гибридный режим: fallback на **JWT** для локальной разработки и тестирования
- Синхронизация профиля пользователя из SSO-claims в локальную БД
- Политика авторизации на основе claims и ролей
- Шифрование чувствительных данных (AES)

### В активной разработке
- Дальнейшее улучшение UI/UX
- Расширение функционала чатов и уведомлений
- Автоматизация тестирования и развёртывания

## Технологический стек

| Компонент          | Технология                          | Описание |
|--------------------|-------------------------------------|----------|
| **Frontend**       | ASP.NET Razor Pages + SignalR Client | Серверный рендеринг + клиент реального времени |
| **Real-time**      | ASP.NET Core SignalR                | Сообщения, typing, online, уведомления |
| **Backend**        | ASP.NET Core Web API                | REST API + SignalR Hubs |
| **Архитектура**    | **Clean Architecture**              | Core / Infrastructure / API / Web |
| **ORM**            | Entity Framework Core               | Database First |
| **БД**             | PostgreSQL 17                       | Хранение пользователей, чатов, сообщений |
| **Аутентификация** | OpenID Connect (OIDC) + JWT fallback| SSO ГУАП (Keycloak) |
| **Авторизация**    | Policy-based + Claims               | Роли и права из SSO |
| **Шифрование**     | AES (мастер-ключ)                   | Защита чувствительных данных |
| **CI/CD**          | **GitHub Actions**                  | Автоматическая сборка, запуск тестов |
| **Тестирование**   | xUnit + ручное UI-тестирование      | Юнит-тесты в пайплайне |

## Установка и запуск (локально)

### Требования
- .NET SDK 9.0+
- PostgreSQL 17
- Git

### Шаги

1. **Клонирование и переход на ветку**
   ```bash
   git clone https://github.com/art2535/GUAP_Messenger.git
   cd GUAP_Messenger
   ```

2. **Восстановление пакетов**
   ```bash
   dotnet restore
   ```

3. **Настройка конфигурации**

   Рекомендуется использовать **User Secrets** (для локальной разработки) или создать файл `appsettings.Development.json` (он уже добавлен в `.gitignore`).
  
    **Пример минимально необходимой конфигурации** (`appsettings.Development.json`):
    
    ```json
    {
      "DetailedErrors": true,
      "Logging": {
        "LogLevel": {
          "Default": "Information",
          "Microsoft.AspNetCore": "Warning"
        }
      },
      "Vapid": {
        "Subject": "mailto:your-email@example.com",
        "PublicKey": "{YOUR_VAPID_PUBLIC_KEY}",
        "PrivateKey": "{YOUR_VAPID_PRIVATE_KEY}"
      },
      "RabbitMQ": {
        "Host": "localhost",
        "Port": "5672",
        "Username": "{RABBITMQ_USERNAME}",
        "Password": "{RABBITMQ_PASSWORD}"
      },
      "AzureAd": {
        "Instance": "https://sso.guap.ru/realms/",
        "TenantId": "master",
        "ClientId": "messager",
        "ClientSecret": "{KEYCLOAK_CLIENT_SECRET}",
        "CallbackPath": "/signin-oidc",
        "SignedOutCallbackPath": "/signout-callback-oidc",
        "Domain": "guap.ru",
        "Audience": "messager"
      },
      "URL": {
        "API": {
          "HTTPS": "https://localhost:7001",
          "HTTP": "http://localhost:5245"
        },
        "Web": {
          "HTTPS": "https://localhost:7010",
          "HTTP": "http://localhost:5207"
        }
      },
      "Jwt": {
        "Key": "{YOUR_LONG_JWT_SIGNING_KEY_BASE64_MIN_256_BIT}",
        "Issuer": "https://localhost:7001",
        "Audience": "https://localhost:7010"
      },
      "Encryption": {
        "MasterKeyBase64": "{YOUR_32_BYTE_ENCRYPTION_MASTER_KEY_BASE64}"
      }
    }
    ```
    
    > **Важно**:  
    > - Никогда не коммитьте реальные секреты в репозиторий.  
    > - Используйте `dotnet user-secrets set "AzureAd:ClientSecret" "ваш_секрет"` для локальной разработки.  
    > - Для production окружения секреты должны задаваться через переменные окружения или Secrets Manager.

5. **Применение миграций**
   ```bash
   cd Messenger.Infrastructure
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```

6. **Запуск**

   Рекомендуется через **Visual Studio** (Multiple startup projects: `Messenger.API` + `Messenger.Web`).

   Или вручную:
   ```bash
   # Терминал 1 — API + SignalR
   cd Messenger.API
   dotnet run

   # Терминал 2 — Web (Razor Pages)
   cd Messenger.Web
   dotnet run
   ```

## CI/CD

В репозитории настроены **GitHub Actions**.  
Пайплайн автоматически:
- восстанавливает зависимости,
- собирает решение,
- запускает юнит-тесты (`Messenger.Tests`).

Статус последних запусков можно посмотреть по бейджу выше или в разделе **[Actions](https://github.com/art2535/GUAP_Messenger/actions)**.

## Структура проекта
- `Messenger.Core` — доменные модели и бизнес-логика
- `Messenger.Infrastructure` — EF Core, репозитории, миграции
- `Messenger.API` — Web API и SignalR хабы
- `Messenger.Web` — Razor Pages + клиентская часть
- `Messenger.Tests` — юнит-тесты

## Документация
Подробная информация находится в **[GitHub Wiki](https://github.com/art2535/GUAP_Messenger/wiki)**.

## Как внести вклад
1. Форкните репозиторий
2. Создайте ветку от `main` (`feature/название-функции` или `fix/проблема`)
3. Внесите изменения
4. Откройте **Pull Request** в ветку `main`

Ищите задачи с метками `good first issue` или `help wanted`.

## Поддержка
- Баги и предложения → [Issues](https://github.com/art2535/GUAP_Messenger/issues)
- Критические проблемы → Issues с меткой `critical`

## Ведущий разработчик
**[Артём Петров (art2535)](https://github.com/art2535)** — студент 4 курса ФСПО ГУАП  
Специальность: 09.02.07 «Информационные системы и программирование»

---

**Спасибо за интерес к проекту!**  
Присоединяйтесь — вместе сделаем лучший университетский мессенджер в России 🚀
