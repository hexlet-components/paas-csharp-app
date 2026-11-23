<div align="center">

<a href="https://hexlet.io/?utm_source=github&utm_medium=link&utm_campaign=paas-csharp-app">
    <picture>
        <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/Hexlet/brand-assets/master/images/svg/hexlet_wordmark_white_rus.svg">
        <img src="https://raw.githubusercontent.com/Hexlet/brand-assets/master/images/svg/hexlet_wordmark_primary_rus.svg" alt="Хекслет" height="64">
    </picture>
</a>

# C#-приложение для курса «Деплой на PaaS»

[![CI](https://github.com/hexlet-components/paas-csharp-app/actions/workflows/main.yml/badge.svg)](https://github.com/hexlet-components/paas-csharp-app/actions/workflows/main.yml)
[![Check](https://github.com/hexlet-components/paas-csharp-app/actions/workflows/check.yml/badge.svg)](https://github.com/hexlet-components/paas-csharp-app/actions/workflows/check.yml)

</div>

Компонент создаёт и поддерживает команда [Хекслета](https://hexlet.io/?utm_source=github&utm_medium=link&utm_campaign=paas-csharp-app), образовательного проекта.

Учебное приложение курса [«Деплой на PaaS»](https://ru.hexlet.io/courses/paas-deploy) для тех, кто проходит курс на C#. Студент делает fork, публикует приложение на Render и учится на нём конфигурации, обновлениям и диагностике. Такие же приложения есть для других стеков курса, у всех одинаковое поведение по HTTP. Изменение маршрутов, ответов или формата лога ломает шаги курса, поэтому такие правки сначала сверяются с уроками.

## Поведение

| Запрос | Ответ |
| --- | --- |
| `GET /` | `200`, заголовок «Привет от Хекслета!», значение `SERVER_MESSAGE`, с `DATABASE_URL` ещё счётчик посещений |
| `GET /error` | `500`, событие уходит в Sentry, процесс продолжает работать |
| другой путь | `404` |

Сервер слушает `0.0.0.0` на порту из `PORT` (по умолчанию `3000`). При старте в логе появляется `Now listening on: http://0.0.0.0:3000`, на каждый запрос печатается строка `GET / 200`. Sentry SDK включается переменной `SENTRY_DSN`, без неё события не отправляются.

## Публикация на Render

| Поле | Значение |
| --- | --- |
| Language | Docker |
| Dockerfile Path | `./Dockerfile` |
| Docker Command | пусто |
| Environment | `PORT=3000`, `SERVER_MESSAGE`, по желанию `SENTRY_DSN` и `DATABASE_URL` |

У Render нет готовой среды .NET, поэтому приложение публикуется через Dockerfile. Он собирает только приложение, версия .NET задана в его образах.

## База данных

С переменной `DATABASE_URL` главная страница считает посещения в таблице `page_visits` и показывает строку «Посещений: N». Адрес имеет вид `postgresql://user:password@host:port/name`, порт необязателен. На Render это Internal Database URL базы из того же региона. Без переменной приложение работает без базы, и страница остаётся прежней.

Миграции EF Core лежат в `src/PaasCsharpApp/Migrations`. При запуске приложение сначала применяет новые миграции вызовом `MigrateAsync()` и только потом открывает порт. EF Core перед применением блокирует таблицу истории `__EFMigrationsHistory`, поэтому из двух экземпляров, запущенных разом, миграцию применяет один, а второй ждёт и видит её уже применённой. Новая миграция создаётся по модели в `AppDbContext` командой `dnx dotnet-ef migrations add <Имя> --project src/PaasCsharpApp`.

Тесты с базой идут, когда задан `TEST_DATABASE_URL`, и очищают эту базу. Без переменной они пропускаются. В CI базу даёт сервис PostgreSQL из `main.yml`.

## Запуск

```sh
mise run setup
mise run test
PORT=3000 SERVER_MESSAGE="Первая публикация" mise run start
```

`make build` повторяет стадию сборки `Dockerfile`, `make start` запускает приложение командой из его `CMD` и перед этим сам выполняет `build`. Эти две цели работают без mise, нужен только .NET SDK 11, его установка описана в [инструкции Хекслета](https://github.com/Hexlet/ru-instructions/blob/main/dotnet.md).

`make <задача>` вызывает ту же задачу mise.

## Проверки

`mise run check` гоняет тот же набор, что CI: формат и анализаторы .NET, NuGet Audit, разметку, опечатки, ссылки, workflows и секреты.
