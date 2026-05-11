using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PetShopDbDemo
{
    public record ProductRow(int Id, string Name, string Category, decimal Price, int Stock);
    public record OrderRow(int Id, string Customer, decimal Total, string Status, DateTime CreatedAt);
    public record UserStatRow(string Name, string Email, int OrdersCount, decimal TotalSpent);
    public record RecentOrderRow(int Id, string Customer, decimal Total, string Status, DateTime CreatedAt);
    public record LinqResult(List<UserStatRow> Stats, List<RecentOrderRow> Recent);

    // Лаб. 2. Зоомагазин: подключение к БД через ODBC, OleDb и LINQ.
    // База petshop (PostgreSQL). Байдавлетова А.У., 241-336.
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.Title = "Лаб. работа № 2. Зоомагазин: ODBC + OleDb + LINQ";

            List<ProductRow> products = null;
            string odbcError = null;
            PrintHeader("1. ODBC -> PostgreSQL: список товаров");
            try { products = OdbcDemo.Run(); }
            catch (Exception ex) { odbcError = ex.Message; Console.WriteLine("ODBC error: " + ex.Message); }

            List<OrderRow> orders = null;
            string oleError = null;
            PrintHeader("2. OleDb -> PostgreSQL: список заказов");
            try { orders = OleDbDemo.Run(); }
            catch (Exception ex) { oleError = ex.Message; Console.WriteLine("OleDb error: " + ex.Message); }

            LinqResult linq = null;
            string linqError = null;
            PrintHeader("3. LINQ (EF Core) -> PostgreSQL: статистика по пользователям");
            try { linq = LinqDemo.Run(); }
            catch (Exception ex) { linqError = ex.Message; Console.WriteLine("LINQ error: " + ex.Message); }

            string htmlPath = Path.Combine(AppContext.BaseDirectory, "report.html");
            HtmlReport.Save(htmlPath, products, odbcError, orders, oleError, linq, linqError);
            Console.WriteLine();
            Console.WriteLine($"HTML-отчёт сохранён: {htmlPath}");
            HtmlReport.OpenInBrowser(htmlPath);

            Console.WriteLine();
            Console.WriteLine("Готово. Нажмите любую клавишу...");
            if (!Console.IsInputRedirected) Console.ReadKey();
        }

        static void PrintHeader(string s)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 70));
            Console.WriteLine(s);
            Console.WriteLine(new string('=', 70));
        }

        public static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max - 1) + "…";
        }
    }

    // 1. ODBC. Беру список товаров и через JOIN подтягиваю название категории.
    // Нужен PostgreSQL ODBC Driver (psqlODBC).
    static class OdbcDemo
    {
        public const string ConnectionString =
            "Driver={PostgreSQL Unicode(x64)};" +
            "Server=localhost;Port=5432;" +
            "Database=petshop;" +
            "Uid=postgres;Pwd=1234;";

        public static List<ProductRow> Run()
        {
            using var conn = new OdbcConnection(ConnectionString);
            conn.Open();
            Console.WriteLine($"Соединение установлено. Сервер: {conn.DataSource}");

            const string sql = @"
                SELECT p.id, p.name, c.name AS category, p.price, p.stock
                FROM products p
                JOIN categories c ON c.id = p.category_id
                ORDER BY p.id
                LIMIT 10";

            using var cmd = new OdbcCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine();
            Console.WriteLine($"{"Id",-3} {"Название",-40} {"Категория",-12} {"Цена",10} {"Ост.",5}");
            Console.WriteLine(new string('-', 75));

            var result = new List<ProductRow>();
            while (reader.Read())
            {
                var row = new ProductRow(
                    reader.GetInt32(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDecimal(3),
                    reader.GetInt32(4));
                result.Add(row);
                Console.WriteLine(
                    $"{row.Id,-3} " +
                    $"{Program.Truncate(row.Name, 40),-40} " +
                    $"{row.Category,-12} " +
                    $"{row.Price,10:N2} " +
                    $"{row.Stock,5}");
            }
            return result;
        }
    }

    // 2. OleDb. С PostgreSQL напрямую через OleDb связаться не получается:
    // в новом .NET нет нативного OleDb-провайдера для Postgres, а мост
    // MSDASQL поверх ODBC выпилили. Поэтому сначала выгружаю заказы из
    // Postgres в CSV (через ODBC), а потом читаю этот CSV провайдером
    // Microsoft.ACE.OLEDB.16.0 (Text-драйвер)
    static class OleDbDemo
    {
        public static List<OrderRow> Run()
        {
            string csvDir = Path.Combine(AppContext.BaseDirectory, "data");
            Directory.CreateDirectory(csvDir);
            string csvFile = "orders.csv";
            string csvPath = Path.Combine(csvDir, csvFile);

            int exported = ExportOrdersToCsv(csvPath);
            Console.WriteLine($"Выгружено заказов из PostgreSQL в CSV: {exported} ({csvPath})");

            WriteSchemaIni(csvDir, csvFile);

            string connectionString =
                "Provider=Microsoft.ACE.OLEDB.16.0;" +
                $"Data Source={csvDir};" +
                "Extended Properties=\"text;HDR=Yes;FMT=Delimited\";";

            using var conn = new OleDbConnection(connectionString);
            conn.Open();
            Console.WriteLine($"Соединение установлено. Провайдер: {conn.Provider}");

            string sql = $"SELECT [id], [name], [total], [status], [created_at] FROM [{csvFile}] ORDER BY [created_at] DESC";

            using var cmd = new OleDbCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            Console.WriteLine();
            Console.WriteLine($"{"#",-3} {"Покупатель",-25} {"Сумма",10} {"Статус",-12} {"Дата",-12}");
            Console.WriteLine(new string('-', 65));

            var result = new List<OrderRow>();
            while (reader.Read())
            {
                int id = int.Parse((string)reader["id"], CultureInfo.InvariantCulture);
                string name = (string)reader["name"];
                decimal total = decimal.Parse((string)reader["total"], CultureInfo.InvariantCulture);
                string status = (string)reader["status"];
                DateTime created = DateTime.ParseExact(
                    (string)reader["created_at"],
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture);

                result.Add(new OrderRow(id, name, total, status, created));
                Console.WriteLine(
                    $"{id,-3} " +
                    $"{name,-25} " +
                    $"{total,10:N2} " +
                    $"{status,-12} " +
                    $"{created,-12:yyyy-MM-dd}");
            }
            return result;
        }

        static void WriteSchemaIni(string folder, string csvFile)
        {
            string schemaPath = Path.Combine(folder, "schema.ini");
            string contents =
                $"[{csvFile}]\r\n" +
                "ColNameHeader=True\r\n" +
                "Format=Delimited(;)\r\n" +
                "CharacterSet=65001\r\n" +
                "Col1=id Char Width 20\r\n" +
                "Col2=name Char Width 255\r\n" +
                "Col3=total Char Width 30\r\n" +
                "Col4=status Char Width 50\r\n" +
                "Col5=created_at Char Width 30\r\n";
            File.WriteAllText(schemaPath, contents, System.Text.Encoding.ASCII);
        }

        static int ExportOrdersToCsv(string path)
        {
            using var conn = new OdbcConnection(OdbcDemo.ConnectionString);
            conn.Open();

            const string sql = @"
                SELECT o.id, u.name, o.total, o.status, o.created_at
                FROM orders o
                JOIN users u ON u.id = o.user_id
                ORDER BY o.created_at DESC";

            using var cmd = new OdbcCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            using var writer = new StreamWriter(path, false, new System.Text.UTF8Encoding(false));
            writer.WriteLine("id;name;total;status;created_at");

            int rows = 0;
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1).Replace(";", ",");
                decimal total = reader.GetDecimal(2);
                string status = reader.GetString(3);
                DateTime created = reader.GetDateTime(4);

                writer.WriteLine(
                    $"{id};" +
                    $"{name};" +
                    $"{total.ToString(CultureInfo.InvariantCulture)};" +
                    $"{status};" +
                    $"{created.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}");
                rows++;
            }
            return rows;
        }
    }

    // 3. LINQ через EF Core. Связь users -> orders, считаю количество
    // заказов и сумму трат по каждому пользователю.
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public List<Order> Orders { get; set; } = new();
    }

    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public User User { get; set; }
    }

    public class PetShopContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            const string cs =
                "Host=localhost;Port=5432;" +
                "Database=petshop;" +
                "Username=postgres;Password=1234";
            o.UseNpgsql(cs);
        }

        protected override void OnModelCreating(ModelBuilder b)
        {
            b.Entity<User>().ToTable("users");
            b.Entity<User>().Property(x => x.Id).HasColumnName("id");
            b.Entity<User>().Property(x => x.Email).HasColumnName("email");
            b.Entity<User>().Property(x => x.Name).HasColumnName("name");

            b.Entity<Order>().ToTable("orders");
            b.Entity<Order>().Property(x => x.Id).HasColumnName("id");
            b.Entity<Order>().Property(x => x.UserId).HasColumnName("user_id");
            b.Entity<Order>().Property(x => x.Total).HasColumnName("total");
            b.Entity<Order>().Property(x => x.Status).HasColumnName("status");
            b.Entity<Order>().Property(x => x.CreatedAt).HasColumnName("created_at");

            b.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId);
        }
    }

    static class LinqDemo
    {
        public static LinqResult Run()
        {
            using var db = new PetShopContext();
            Console.WriteLine($"Соединение установлено. БД: {db.Database.GetDbConnection().Database}");

            var statsRaw = db.Users
                .Select(u => new
                {
                    u.Name,
                    u.Email,
                    OrdersCount = u.Orders.Count(),
                    TotalSpent = u.Orders.Sum(o => (decimal?)o.Total) ?? 0m
                })
                .OrderByDescending(x => x.TotalSpent)
                .ToList();

            var stats = statsRaw
                .Select(x => new UserStatRow(x.Name, x.Email, x.OrdersCount, x.TotalSpent))
                .ToList();

            Console.WriteLine();
            Console.WriteLine("LINQ: статистика покупок по пользователям");
            Console.WriteLine();
            Console.WriteLine($"{"Имя",-25} {"Email",-25} {"Заказов",-8} {"Потрачено",12}");
            Console.WriteLine(new string('-', 73));
            foreach (var s in stats)
            {
                Console.WriteLine($"{s.Name,-25} {s.Email,-25} {s.OrdersCount,-8} {s.TotalSpent,12:N2}");
            }

            Console.WriteLine();
            Console.WriteLine("LINQ: последние 3 заказа");
            Console.WriteLine();
            var latestEntities = db.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .ToList();

            var latest = latestEntities
                .Select(o => new RecentOrderRow(o.Id, o.User.Name, o.Total, o.Status, o.CreatedAt))
                .ToList();

            foreach (var o in latest)
            {
                Console.WriteLine($"  Заказ #{o.Id} от {o.Customer}: {o.Total:N2} руб., статус: {o.Status}");
            }

            return new LinqResult(stats, latest);
        }
    }

}
