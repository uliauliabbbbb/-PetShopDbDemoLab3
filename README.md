# PetShopDbDemo

Лабораторная работа № 2 по курсу баз данных. Консольное приложение на C# (.NET 8), которое подключается к БД `petshop` (PostgreSQL) тремя разными способами: через **ODBC**, **OleDb** и **LINQ** (EF Core). Делает выборки, выводит результат в консоль и заодно собирает HTML-отчёт `report.html`, который открывается в браузере.

Предметная область: интернет-магазин товаров для животных (зоомагазин).

## Что выводит программа

Программа последовательно выполняет три блока:

1. **ODBC.** Список товаров: id, название, категория, цена, остаток. Запрос с JOIN по таблице категорий, выборка первых 10 строк.
2. **OleDb.** Список заказов с именами покупателей. Подробности про CSV-обходку ниже.
3. **LINQ.** Статистика по пользователям: сколько у каждого заказов и на какую сумму. Плюс три последних заказа.

В самом конце дополнительно открывается `report.html` со всеми тремя таблицами в браузере (для наглядности и скриншотов).

## Стек

- .NET 8, C#
- `System.Data.Odbc`, `System.Data.OleDb`
- `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`
- PostgreSQL 15+
- PostgreSQL ODBC Driver (psqlODBC)
- Microsoft.ACE.OLEDB.16.0 (Access Database Engine)

## Подготовка

### 1. База `petshop` в PostgreSQL

В pgAdmin: ПКМ по серверу PostgreSQL > Create > Database, имя `petshop`.

Затем ПКМ по базе `petshop` > Query Tool, открыть файл `petshop_schema.sql` и выполнить (F5). После этого в базе появятся таблицы `categories`, `products`, `users`, `carts`, `cart_items`, `orders`, `order_items` и тестовые данные (20 товаров, 3 пользователя, 3 заказа).

### 2. Драйверы

**Для ODBC нужен PostgreSQL ODBC Driver (psqlODBC):**

1. Скачать MSI: https://www.postgresql.org/ftp/odbc/versions/msi/
2. Установить. После установки в «ODBC Data Sources (64-bit)» появится драйвер «PostgreSQL Unicode(x64)». Этим же драйвером пользуется блок OleDb (для выгрузки данных в CSV).

**Для OleDb нужен Microsoft Access Database Engine** (провайдер `Microsoft.ACE.OLEDB.16.0`). Если на машине стоит Microsoft Office, обычно уже установлен. Иначе ставим «Microsoft Access Database Engine 2016 Redistributable (x64)» с сайта Microsoft.

**Для LINQ драйверы ставить не нужно:** пакет `Npgsql.EntityFrameworkCore.PostgreSQL` подтянется при `dotnet restore`.

### 3. Пароль

В `Program.cs` пароль пользователя `postgres` указан как `1234` в трёх местах. Если у вас другой, замените.

## Запуск

```
dotnet restore
dotnet run
```

Или открыть `PetShopDbDemo.csproj` в Visual Studio и нажать F5.

После прогона в `bin/Debug/net8.0/` появится `report.html` и папка `data/` с промежуточным CSV.

## Про OleDb и CSV

В современном .NET `System.Data.OleDb` больше не поддерживает мост MSDASQL поверх ODBC, а нативного OleDb-провайдера для PostgreSQL не существует. Поэтому блок OleDb работает через CSV-мост: программа сначала выгружает заказы из PostgreSQL в `data/orders.csv` (через ODBC-соединение), затем читает тот же CSV провайдером `Microsoft.ACE.OLEDB.16.0` с Text-драйвером. По условиям задания разные блоки могут работать с разными источниками данных.

## Структура проекта

```
PetShopDbDemo/
  PetShopDbDemo.csproj    зависимости и таргет .NET 8
  Program.cs              три блока (ODBC, OleDb, LINQ) + HTML-отчёт
  petshop_schema.sql      создание таблиц и тестовые данные
  screenshots/            скриншоты запуска
  README.md
```

## Связь с веб-проектом

База `petshop` используется и в итоговом проекте по веб-программированию (интернет-магазин товаров для животных). Backend на ASP.NET Core Web API будет работать с теми же таблицами через EF Core, фронтенд на React + TypeScript будет получать данные через REST API.
