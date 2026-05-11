using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PetShopDbDemo
{
    // Маленькие record-классы для удобной передачи строк таблиц на страницу.
    // EF Core возвращает мне Product со всеми полями, а на страницу я отдаю
    // только то, что реально показываю в табличках.
    public record ProductRow(int Id, string Name, string Category, decimal Price, int Stock);
    public record OrderRow(int Id, string Customer, decimal Total, string Status, DateTime CreatedAt);
    public record UserStatRow(string Name, string Email, int OrdersCount, decimal TotalSpent);
    public record RecentOrderRow(int Id, string Customer, decimal Total, string Status, DateTime CreatedAt);
    public record LinqResult(List<UserStatRow> Stats, List<RecentOrderRow> Recent);

    // DTO для приёма JSON-тела от страницы.
    // В UpdateProductDto все поля nullable, потому что обновляю только то,
    // что реально поменялось.
    public record AddProductDto(int CategoryId, string Name, string Description, decimal Price, int Stock, string AnimalType);
    public record UpdateProductDto(int? CategoryId, string Name, decimal? Price, int? Stock);

    // Журнал CRUD-операций. Запоминаю каждое действие, чтобы потом показать
    // его на странице в табличке "что было / что стало".
    public enum CrudAction { Insert, Update, Delete }
    public record CrudLogEntry(DateTime Time, CrudAction Action, int ProductId, string Title, ProductRow Before, ProductRow After, string Comment);

    public class CrudLog
    {
        public List<CrudLogEntry> Entries { get; } = new();
        public HashSet<int> InsertedIds { get; } = new();
        public HashSet<int> UpdatedIds  { get; } = new();
        public HashSet<int> DeletedIds  { get; } = new();
        public Dictionary<int, ProductRow> SnapshotsBefore { get; } = new();
        public object Lock { get; } = new();
    }

    // Общий объект на всё приложение. Сюда складываю результаты трёх демо-блоков
    // (один раз при старте), и журнал CRUD (накапливается, пока работаю).
    public class AppState
    {
        public List<ProductRow> InitialProducts { get; set; }
        public string OdbcError { get; set; }
        public List<OrderRow> Orders { get; set; }
        public string OleError { get; set; }
        public LinqResult Linq { get; set; }
        public string LinqError { get; set; }
        public CrudLog CrudLog { get; } = new();
        public DateTime StartedAt { get; } = DateTime.Now;
    }

    // EF Core: классы под мои таблицы из БД и контекст.
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

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public List<Product> Products { get; set; } = new();
    }

    public class Product
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string AnimalType { get; set; }
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public Category Category { get; set; }
    }

    public class PetShopContext : DbContext
    {
        // Этот конструктор использует ASP.NET Core: он сам подсовывает мне
        // готовые настройки подключения, я ничего не делаю.
        public PetShopContext(DbContextOptions<PetShopContext> options) : base(options) { }

        // А этот пустой нужен мне для блока LINQ. На старте приложения, до
        // того как поднимется веб-сервер, я делаю new PetShopContext() руками,
        // и тогда настройки берутся из OnConfiguring ниже.
        public PetShopContext() { }

        public DbSet<User> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
        {
            if (o.IsConfigured) return;
            o.UseNpgsql(DefaultConnectionString);
        }

        public const string DefaultConnectionString =
            "Host=localhost;Port=5432;" +
            "Database=petshop;" +
            "Username=postgres;Password=1234";

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

            b.Entity<Category>().ToTable("categories");
            b.Entity<Category>().Property(x => x.Id).HasColumnName("id");
            b.Entity<Category>().Property(x => x.Name).HasColumnName("name");
            b.Entity<Category>().Property(x => x.Slug).HasColumnName("slug");

            b.Entity<Product>().ToTable("products");
            b.Entity<Product>().Property(x => x.Id).HasColumnName("id");
            b.Entity<Product>().Property(x => x.CategoryId).HasColumnName("category_id");
            b.Entity<Product>().Property(x => x.Name).HasColumnName("name");
            b.Entity<Product>().Property(x => x.Description).HasColumnName("description");
            b.Entity<Product>().Property(x => x.Price).HasColumnName("price");
            b.Entity<Product>().Property(x => x.Stock).HasColumnName("stock");
            b.Entity<Product>().Property(x => x.AnimalType).HasColumnName("animal_type");
            b.Entity<Product>().Property(x => x.ImageUrl).HasColumnName("image_url");
            b.Entity<Product>().Property(x => x.CreatedAt).HasColumnName("created_at");

            b.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId);
        }
    }
}
