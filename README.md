# Мессенджер для ГУАП

[![.NET](https://img.shields.io/badge/.NET-9.0-blueviolet)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-blue)](https://www.postgresql.org/)

## Описание

**GUAP_Messenger** — это веб-приложение для обмена сообщениями в реальном времени, разработанное для Государственного университета аэрокосмического приборостроения (ГУАП). Проект реализует функционал для личных и групповых чатов с поддержкой отправки текстовых сообщений и файлов, уведомлений в реальном времени и интеграции с внутренними системами аутентификации ГУАП.

Разработка ведется по методологии Waterfall. Проект организован как единое пространство документации [GitHub Wiki](https://github.com/art2535/GUAP_Messenger/wiki) с иерархической структурой страниц.

**Технический стек:**
- **Frontend:** ASP.NET Razor Pages.
- **Real-time:** SignalR для обеспечения реального времени.
- **Backend:** ASP.NET Core API с Entity Framework Core.
- **База данных (СУБД):** PostgreSQL.
- **CI/CD:** GitHub Actions.
- **Тестирование:** xUnit и ручное тестирование UI.

Проект находится в стадии активной разработки, и на данный момент отсутствуют выпущенные релизы или опубликованные пакеты.

## Установка

### Требования
- .NET SDK 9.0.
- PostgreSQL 17.
- Git для клонирования репозитория.

### Шаги установки
1. Клонируйте репозиторий:
   ```
   git clone https://github.com/art2535/GUAP_Messenger.git
   cd GUAP_Messenger
   ```
2. Настройте строку подключения к БД в `appsettings.json`:
   ```
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=guap_messenger;Username=postgres;Password=yourpassword"
   }
   ```
3. Установите зависимости:
   ```
   dotnet restore
   ```
4. Примените миграции БД (если миграции настроены):
   ```
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
5. Запустите приложение:
   ```
   dotnet run
   ```
   Приложение будет доступно по адресу `https://localhost:5001` (или аналогичному).

## Документация

Полная документация проекта доступна в [GitHub Wiki](https://github.com/art2535/GUAP_Messenger/wiki). Основная структура включает:
- [Home (Главная)](https://github.com/art2535/GUAP_Messenger/wiki/Home)
	- [Обзор проекта](https://github.com/art2535/GUAP_Messenger/wiki/Обзор-проекта)
	- [Стиль документации (Style Guide)](https://github.com/art2535/GUAP_Messenger/wiki/Стиль-документации)
	- [Ответственные/Роли](https://github.com/art2535/GUAP_Messenger/wiki/Ответственные-и-роли)
	- [Процессы](https://github.com/art2535/GUAP_Messenger/wiki/Процессы)
	- [Архитектура](https://github.com/art2535/GUAP_Messenger/wiki/Архитектура)
	- [API / Интеграции](https://github.com/art2535/GUAP_Messenger/wiki/API-и-интеграции)
	- [Инструкции (Guides)](https://github.com/art2535/GUAP_Messenger/wiki/Инструкции)
	- [Проекты/Аутсорс](https://github.com/art2535/GUAP_Messenger/wiki/Проекты-и-аутсорс)
	- [Поддержка / Maintenance](https://github.com/art2535/GUAP_Messenger/wiki/Поддержка-и-Maintenance)
	- [Архив / История версий](https://github.com/art2535/GUAP_Messenger/wiki/Архив-и-история-версий)

Документация следует структуре Waterfall, включая фазы Requirements и Design с соответствующими документами (BRD, SRS, HLD, LLD и др.).

## Участие в разработке

Проект открыт для вклада. Следуйте этим шагам:
1. Форкните репозиторий.
2. Создайте ветку: `git checkout -b feature/YourFeature`.
3. Сделайте коммиты: `git commit -m 'Add some feature'`.
4. Пушьте изменения: `git push origin feature/YourFeature`.
5. Откройте Pull Request.

Код-ревью обязательно через Pull Requests. Подробности процессов доступны в [Процессы](https://github.com/art2535/GUAP_Messenger/wiki/Процессы).

**Ответственные лица:**
- Project Manager: [Артем Петров](https://github.com/art2535) — общая координация.
- Lead Developer: [Артем Петров](https://github.com/art2535) — разработка fullstack.

## Поддержка и Maintenance

- **Issues:** Создавайте в GitHub Issues.
- **Критические проблемы:** Свяжитесь с Project Manager.
- Руководства по устранению ошибок: См. [Поддержка / Maintenance](https://github.com/art2535/GUAP_Messenger/wiki/Поддержка-и-Maintenance).

ChangeLog доступен в [Архив / История версий](https://github.com/art2535/GUAP_Messenger/wiki/Архив-и-история-версий).

## Лицензия

Пока нет лицензии. Подробности будут добавлены в файл LICENSE.md, если он появится.

---
- Заказчик: [ГУАП (Государственный университет аэрокосмического приборостроения)](https://guap.ru)
- Дата создания: 17 сентября 2025 г.
