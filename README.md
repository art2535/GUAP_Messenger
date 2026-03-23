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
[![Статус](https://img.shields.io/badge/Статус-Активная%20разработка-yellow?style=flat&logo=git)](https://github.com/art2535/GUAP_Messenger/tree/ETA-Auth)
[![Методология](https://img.shields.io/badge/Методология-Waterfall-orange)](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D1%81%D1%81%D1%8B)

**GUAP Messenger** — веб-приложение для обмена сообщениями в реальном времени, разработанное специально для сообщества Государственного университета аэрокосмического приборостроения (ГУАП).

Проект реализуется по классической каскадной модели (**Waterfall**).  
**Заказчик** — ГУАП.  
**Дата старта проекта:** 17 сентября 2025 года.

### Основной функционал (реализован / в активной разработке)

- Личные (1:1) и групповые чаты
- Отправка текстовых сообщений и файлов
- Уведомления в реальном времени через **SignalR**
- Аутентификация через **OIDC SSO ГУАП**
- Гибридный режим: fallback на JWT для разработки / тестирования
- Синхронизация профиля пользователя из SSO (claims → локальная БД)

### Технологический стек

| Компонент           | Технология                     | Описание                                          |
|---------------------|--------------------------------|---------------------------------------------------|
| Frontend            | ASP.NET Razor Pages            | Серверный рендеринг + клиентский SignalR          |
| Real-time           | ASP.NET Core SignalR           | Сообщения, уведомления, typing, online статус     |
| Backend API         | ASP.NET Core Web API           | REST API + SignalR хабы                           |
| Архитектура         | Clean Architecture             | Разделение на Core / Infrastructure / API / Web   |
| ORM / Миграции      | Entity Framework Core          | Database First подход                             |
| База данных         | PostgreSQL 17                  | Хранение чатов, сообщений, профилей               |
| Аутентификация      | OpenID Connect (OIDC)          | SSO ГУАП (Keycloak)                               |
| Авторизация         | Policy-based + claims          | Роли и права из SSO claims                        |
| Шифрование          | AES (мастер-ключ в настройках) | Защита чувствительных данных                      |
| CI/CD               | GitHub Actions                 | Автоматическая сборка и тесты                     |
| Тестирование        | xUnit + ручное UI-тестирование | Unit-тесты и проверка интерфейса                  |

### Установка и запуск (локально)

**Требования**  
- .NET SDK 9.0 или новее  
- PostgreSQL 17  
- Git

**Шаги установки**

1. Клонируйте репозиторий и перейдите на ветку

   ```bash
   git clone https://github.com/art2535/GUAP_Messenger.git
   cd GUAP_Messenger
   git checkout ETA-Auth
   ```

2. Восстановите зависимости

   ```bash
   dotnet restore
   ```

3. Настройте конфигурацию

   Используйте **User Secrets** (рекомендуется) или создайте файл `appsettings.Development.json` (не коммитьте его!).

   Пример минимально необходимых настроек:

   ```json
   {
     "AzureAd": {
       "Instance": "https://sso.guap.ru/realms/{realm}/",
       "TenantId": "{tenant-id}",
       "ClientId": "{client-id}",
       "Audience": "{audience}"
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

   > **Важно**:  
   > • Никогда не коммитьте реальные ключи, пароли и порты!  
   > • Используйте `dotnet user-secrets` или `.gitignore`-protected файлы.

4. Создайте и примените миграции

   ```bash
   dotnet ef migrations add InitialCreate --project Messenger.Infrastructure --startup-project Messenger.API
   dotnet ef database update --project Messenger.Infrastructure --startup-project Messenger.API
   ```

5. Запустите приложение

   Рекомендуется запускать через Visual Studio (multiple startup projects: API + Web).

   Или по отдельности:

   ```bash
   # API + SignalR
   cd Messenger.API
   dotnet run
   ```

   ```bash
   # Web (Razor Pages + клиент SignalR)
   cd Messenger.Web
   dotnet run
   ```

   Ориентировочные адреса в режиме разработки:  
   - Web UI → https://localhost:{web-https-port}  
   - API → https://localhost:{api-https-port}

### Документация проекта

Подробная информация — в **[GitHub Wiki](https://github.com/art2535/GUAP_Messenger/wiki)**

- [Обзор проекта](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9E%D0%B1%D0%B7%D0%BE%D1%80-%D0%BF%D1%80%D0%BE%D0%B5%D0%BA%D1%82%D0%B0)
- [Архитектура](https://github.com/art2535/GUAP_Messenger/wiki/%D0%90%D1%80%D1%85%D0%B8%D1%82%D0%B5%D0%BA%D1%82%D1%83%D1%80%D0%B0)
- [API и интеграции](https://github.com/art2535/GUAP_Messenger/wiki/API-%D0%B8-%D0%B8%D0%BD%D1%82%D0%B5%D0%B3%D1%80%D0%B0%D1%86%D0%B8%D0%B8)
- [Процессы и Waterfall](https://github.com/art2535/GUAP_Messenger/wiki/%D0%9F%D1%80%D0%BE%D1%86%D0%B5%D1%81%D1%81%D1%8B)

### Как внести вклад

1. Форкните репозиторий  
2. Создайте ветку `feature/название` или `fix/проблема`  
3. Внесите изменения  
4. Создайте **Pull Request** → предпочтительно в `ETA-Auth`

Ищите задачи с меткой `good first issue` или `help wanted`.

### Поддержка и обратная связь

- Баги, вопросы, идеи → [Issues](https://github.com/art2535/GUAP_Messenger/issues)  
- Критические проблемы → Issues с меткой `critical` или напрямую → [@art2535](https://github.com/art2535)

### Лицензия

Пока лицензия не выбрана (планируется добавить LICENSE.md).  
Использование кода регулируется внутренними правилами ГУАП.

## Ведущий разработчик

[Артём Петров (art2535)](https://github.com/art2535) — студент 4 курса ФСПО ГУАП  
Специальность: 09.02.07 «Информационные системы и программирование»

**Спасибо за интерес к проекту!**  
_Присоединяйтесь — сделаем лучший университетский мессенджер вместе!_ 🚀
