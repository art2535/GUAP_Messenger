## Команда для миграции схемы базы данных из PostgreSQL в C# классы-сущности

```bash
dotnet ef dbcontext scaffold "<Connection_String>" <Provider> \
--output-dir <Путь_к_папке_моделей> \
--context-dir <Путь_к_папке_контекста> \
--context <Имя_класса_контекста> \
--schema <Схема_БД> \
--data-annotations \
--project <Проект_для_выполнения_команды> \
--force \
--namespace <Пространство_имён_для_моделей> \
--context-namespace <Пространство_имён_для_контекста> \
--no-pluralize \
--use-database-names \
--tables <Имя_таблицы1> <Имя_таблицы2> \
--schemas <Имя_схемы1> <Имя_схемы2>
```

### Описание параметров:

1. `<Connection_String>` — `"Host=localhost;Port=<Port>;Database=<Database>;Username=<username>;Password=<Password>"` — строка подключения.
2. `<Provider>` — `Npgsql.EntityFrameworkCore.PostgreSQL` — провайдер для PostgreSQL.
3. `--output-dir <Путь_к_папке_моделей>` — папка для классов моделей в проекте Messenger.Core.
4. `--context-dir <Путь_к_папке_контекста>` — папка для контекста в проекте Messenger.Infrastructure.
5. `--context <Имя_класса_контекста>` — имя класса контекста.
6. `--schema <Схема_БД>` — `public` — схема базы данных.
7. `--data-annotations` — использовать атрибуты Data Annotations для моделей.
8. `--project <Проект_для_выполнения_команды>` — проект, где выполняется команда.
9. `--force` — перезаписывает существующие файлы.
10. `--namespace <Пространство_имён_для_моделей>` — пространство имён для моделей.
11. `--context-namespace <Пространство_имён_для_контекста>` — пространство имён для DbContext.
12. `--no-pluralize` — отключает автоматическое множественное число для имён DbSet.
13. `--use-database-names` — использовать точные имена таблиц и колонок из БД.
14. `--tables <Имя_таблицы1> <Имя_таблицы2>` — (опционально) генерировать модели только для указанных таблиц.
15. `--schemas <Имя_схемы1> <Имя_схемы2>` — (опционально) для работы с несколькими схемами.