using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace PetShopDbDemo
{
    // Здесь живут все мои "ручки" CRUD для таблицы products.
    // ASP.NET Core сам передаёт мне PetShopContext в каждый метод
    // (создаёт новый на каждый запрос), поэтому руками new PetShopContext()
    // я тут уже не пишу.
    static class ProductEndpoints
    {
        public static void Map(IEndpointRouteBuilder app)
        {
            var api = app.MapGroup("/api");

            api.MapGet("/demo", (AppState state) => Results.Ok(new
            {
                products = state.InitialProducts,
                productsError = state.OdbcError,
                orders = state.Orders,
                ordersError = state.OleError,
                linq = state.Linq,
                linqError = state.LinqError,
                startedAt = state.StartedAt
            }));

            api.MapGet("/categories", (PetShopContext db) =>
                Results.Ok(db.Categories
                    .OrderBy(c => c.Id)
                    .Select(c => new { id = c.Id, name = c.Name })
                    .ToList()));

            api.MapGet("/log", (AppState state) =>
            {
                lock (state.CrudLog.Lock)
                {
                    return Results.Ok(new
                    {
                        entries = state.CrudLog.Entries,
                        insertedIds = state.CrudLog.InsertedIds.ToArray(),
                        updatedIds  = state.CrudLog.UpdatedIds.ToArray(),
                        deletedIds  = state.CrudLog.DeletedIds.ToArray()
                    });
                }
            });

            api.MapGet("/products", (PetShopContext db, AppState state) =>
            {
                var items = db.Products
                    .Include(p => p.Category)
                    .OrderBy(p => p.Id)
                    .ToList()
                    .Select(p => new ProductRow(p.Id, p.Name, p.Category?.Name ?? "?", p.Price, p.Stock))
                    .ToList();
                lock (state.CrudLog.Lock)
                {
                    return Results.Ok(new
                    {
                        items,
                        insertedIds = state.CrudLog.InsertedIds.ToArray(),
                        updatedIds  = state.CrudLog.UpdatedIds.ToArray()
                    });
                }
            });

            api.MapPost("/products", (AddProductDto dto, PetShopContext db, AppState state) =>
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
                    return Results.BadRequest(new { error = "Название не может быть пустым" });
                if (dto.Price < 0)
                    return Results.BadRequest(new { error = "Цена не может быть отрицательной" });

                var cat = db.Categories.FirstOrDefault(c => c.Id == dto.CategoryId);
                if (cat == null)
                    return Results.BadRequest(new { error = "Категория не найдена" });

                var product = new Product
                {
                    CategoryId = dto.CategoryId,
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    Price = dto.Price,
                    Stock = dto.Stock,
                    AnimalType = string.IsNullOrWhiteSpace(dto.AnimalType) ? "universal" : dto.AnimalType.Trim(),
                    ImageUrl = null,
                    CreatedAt = DateTime.UtcNow
                };
                db.Products.Add(product);
                db.SaveChanges();

                var row = new ProductRow(product.Id, product.Name, cat.Name, product.Price, product.Stock);
                lock (state.CrudLog.Lock)
                {
                    state.CrudLog.Entries.Add(new CrudLogEntry(
                        DateTime.Now, CrudAction.Insert, product.Id,
                        $"Добавлен товар «{product.Name}»",
                        null, row,
                        $"тип: {product.AnimalType}, описание: {product.Description ?? "(нет)"}"));
                    state.CrudLog.InsertedIds.Add(product.Id);
                }
                return Results.Ok(new { ok = true, product = row });
            });

            api.MapPut("/products/{id:int}", (int id, UpdateProductDto dto, PetShopContext db, AppState state) =>
            {
                if (dto == null) return Results.BadRequest(new { error = "Пустое тело запроса" });

                var product = db.Products
                    .Include(p => p.Category)
                    .FirstOrDefault(p => p.Id == id);
                if (product == null)
                    return Results.NotFound(new { error = $"Товар Id={id} не найден" });

                var before = new ProductRow(product.Id, product.Name, product.Category?.Name ?? "?", product.Price, product.Stock);
                var changes = new List<string>();

                if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name.Trim() != product.Name)
                {
                    changes.Add($"название \"{product.Name}\" → \"{dto.Name.Trim()}\"");
                    product.Name = dto.Name.Trim();
                }
                if (dto.Price.HasValue && dto.Price.Value != product.Price)
                {
                    if (dto.Price.Value < 0)
                        return Results.BadRequest(new { error = "Цена не может быть отрицательной" });
                    changes.Add($"цена {product.Price:N2} → {dto.Price.Value:N2}");
                    product.Price = dto.Price.Value;
                }
                if (dto.Stock.HasValue && dto.Stock.Value != product.Stock)
                {
                    changes.Add($"остаток {product.Stock} → {dto.Stock.Value}");
                    product.Stock = dto.Stock.Value;
                }
                if (dto.CategoryId.HasValue && dto.CategoryId.Value != product.CategoryId)
                {
                    var newCat = db.Categories.FirstOrDefault(c => c.Id == dto.CategoryId.Value);
                    if (newCat == null)
                        return Results.BadRequest(new { error = "Категория не найдена" });
                    changes.Add($"категория \"{product.Category?.Name}\" → \"{newCat.Name}\"");
                    product.CategoryId = dto.CategoryId.Value;
                    product.Category = newCat;
                }

                if (changes.Count == 0)
                    return Results.Ok(new { ok = true, noop = true });

                db.SaveChanges();
                var after = new ProductRow(product.Id, product.Name, product.Category?.Name ?? "?", product.Price, product.Stock);

                lock (state.CrudLog.Lock)
                {
                    if (!state.CrudLog.SnapshotsBefore.ContainsKey(id))
                        state.CrudLog.SnapshotsBefore[id] = before;
                    state.CrudLog.Entries.Add(new CrudLogEntry(
                        DateTime.Now, CrudAction.Update, id,
                        $"Изменён товар «{product.Name}»",
                        before, after,
                        string.Join("; ", changes)));
                    state.CrudLog.UpdatedIds.Add(id);
                }
                return Results.Ok(new { ok = true, product = after });
            });

            api.MapDelete("/products/{id:int}", (int id, PetShopContext db, AppState state) =>
            {
                var product = db.Products
                    .Include(p => p.Category)
                    .FirstOrDefault(p => p.Id == id);
                if (product == null)
                    return Results.NotFound(new { error = $"Товар Id={id} не найден" });

                var before = new ProductRow(product.Id, product.Name, product.Category?.Name ?? "?", product.Price, product.Stock);
                db.Products.Remove(product);
                try
                {
                    db.SaveChanges();
                    lock (state.CrudLog.Lock)
                    {
                        state.CrudLog.Entries.Add(new CrudLogEntry(
                            DateTime.Now, CrudAction.Delete, id,
                            $"Удалён товар «{before.Name}»",
                            before, null, "DELETE"));
                        state.CrudLog.DeletedIds.Add(id);
                        state.CrudLog.InsertedIds.Remove(id);
                        state.CrudLog.UpdatedIds.Remove(id);
                    }
                    return Results.Ok(new { ok = true });
                }
                catch (DbUpdateException)
                {
                    lock (state.CrudLog.Lock)
                    {
                        state.CrudLog.Entries.Add(new CrudLogEntry(
                            DateTime.Now, CrudAction.Delete, id,
                            $"Попытка удалить «{before.Name}» отклонена БД",
                            before, before,
                            "FK-ограничение: товар используется в order_items"));
                    }
                    return Results.Conflict(new { error = "Не удалось удалить: товар уже встречается в заказах (FK)." });
                }
            });
        }
    }
}
