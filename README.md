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
[![Branch](https://img.shields.io/badge/Ветка-ETA--Auth-success)](https://github.com/art2535/GUAP_Messenger/tree/ETA-Auth)
[![Статус](https://img.shields.io/badge/Статус-Активная%20разработка-yellow?style=flat&logo=git)](https://github.com/art2535/GUAP_Messenger/tree/ETA-Auth)
[![Методология](https://img.shields.io/badge/Методология-Waterfall-orange)](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D1%81%D1%81%D1%8B)

**GUAP Messenger** — современное веб-приложение для обмена сообщениями в реальном времени, разработанное специально для сообщества **Государственного университета аэрокосмического приборостроения (ГУАП)**.

Проект реализуется по классической каскадной модели (**Waterfall**).  
**Заказчик** — ГУАП.  
**Дата старта проекта:** 17 сентября 2025 года.

## Основной функционал

### ✅ Реализовано
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
| **CI/CD**          | GitHub Actions                      | Сборка и тесты |
| **Тестирование**   | xUnit + ручное UI-тестирование      | — |

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
   git checkout ETA-Auth
   ```

2. **Восстановление пакетов**
   ```bash
   dotnet restore
   ```

3. **Настройка конфигурации**

   Рекомендуется использовать **User Secrets**:
   ```bash
   dotnet user-secrets set "AzureAd:Instance" "https://sso.guap.ru/realms/{realm}/"
   # ... и остальные параметры
   ```

   Или создайте `appsettings.Development.json` (добавлен в `.gitignore`).

   **Минимально необходимые настройки** (пример):
    ```json
    {
      "AzureAd": {
        "Instance": "https://sso.guap.ru/realms/{realm}/",
        "TenantId": "{tenant-id}",
        "ClientId": "{client-id}",
        "Audience": "{audience}"
      },
      "RabbitMQ": {
        "Host": "localhost",
        "Port": "5672",
        "Username": "{your-username}",
        "Password": "{your-password}"
      },
      "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=GUAP_Messenger;Username=postgres;Password={your-db-password}"
      },
      "Jwt": {
        "Key": "{your-very-long-random-base64-key-min-256-bit}",
        "Issuer": "https://your-api-domain/",
        "Audience": "https://your-web-domain/"
      },
      "Encryption": {
        "MasterKeyBase64": "{your-random-32-byte-key-in-base64}"
      },
      "URL": {
        "API": {
          "HTTPS": "https://localhost:{api-https-port}",
          "HTTP": "http://localhost:{api-http-port}"
        },
        "Web": {
          "HTTPS": "https://localhost:{web-https-port}",
          "HTTP": "http://localhost:{web-http-port}"
        }
      }
    }
    ```

   > **Важно**: Никогда не коммитьте реальные секреты!

4. **Применение миграций**
   ```bash
   dotnet ef migrations add InitialCreate --project Messenger.Infrastructure --startup-project Messenger.API
   dotnet ef database update --project Messenger.Infrastructure --startup-project Messenger.API
   ```

5. **Запуск**

   Лучше через **Visual Studio** (Multiple startup projects: `Messenger.API` + `Messenger.Web`).

   Или вручную:
   ```bash
   # Терминал 1 — API + SignalR
   cd Messenger.API
   dotnet run

   # Терминал 2 — Web (Razor Pages)
   cd Messenger.Web
   dotnet run
   ```

## Структура проекта

- `Messenger.Core` — доменные модели и бизнес-логика  
- `Messenger.Infrastructure` — EF Core, репозитории, миграции  
- `Messenger.API` — Web API и SignalR хабы  
- `Messenger.Web` — Razor Pages + клиентская часть  

## Документация

Подробная информация находится в **[GitHub Wiki](https://github.com/art2535/GUAP_Messenger/wiki)**:
- [Обзор проекта](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9E%D0%B1%D0%B7%D0%BE%D1%80-%D0%BF%D1%80%D0%BE%D0%B5%D0%BA%D1%82%D0%B0)
- [Архитектура](https://github.com/art2535/GUAP_Messenger/wiki/%D0%90%D1%80%D1%85%D0%B8%D1%82%D0%B5%D0%BA%D1%82%D1%83%D1%80%D0%B0)
- [Процессы и Waterfall](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D1%81%D1%81%D1%8B)

## Как внести вклад

1. Форкните репозиторий
2. Создайте ветку (`feature/название-функции` или `fix/проблема`)
3. Внесите изменения
4. Откройте **Pull Request** в ветку `ETA-Auth`

Ищите задачи с метками `good first issue` или `help wanted`.

## Поддержка

- Баги и предложения → [Issues](https://github.com/art2535/GUAP_Messenger/issues)
- Критические проблемы → Issues с меткой `critical` или напрямую автору

## Ведущий разработчик

**[Артём Петров (art2535)](https://github.com/art2535)** — студент 4 курса ФСПО ГУАП  
Специальность: 09.02.07 «Информационные системы и программирование»

---

**Спасибо за интерес к проекту!**  
Присоединяйтесь — вместе сделаем лучший университетский мессенджер в России 🚀
