using System;
using System.Collections.Generic;
using System.IO;

namespace PetShopDbDemo
{
    // Чтобы было нагляднее, складываю результаты всех трёх блоков
    // в report.html и открываю в браузере.
    static class HtmlReport
    {
        public static void Save(
            string path,
            List<ProductRow> products, string odbcError,
            List<OrderRow> orders, string oleError,
            LinqResult linq, string linqError)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"ru\"><head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<title>Лаб. 2. Зоомагазин</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:-apple-system,Segoe UI,Roboto,sans-serif;margin:1.2rem 1.5rem;background:#f4f5f7;color:#222}");
            sb.AppendLine("h1{font-size:1.35rem;margin:0 0 .15rem}");
            sb.AppendLine(".meta{color:#777;margin-bottom:1rem;font-size:.82rem}");
            sb.AppendLine("section{background:#fff;border-radius:6px;padding:.7rem 1rem .8rem;margin-bottom:.9rem;box-shadow:0 1px 2px rgba(0,0,0,.06);border-top:4px solid #ccc}");
            sb.AppendLine("section.odbc{border-top-color:#1a73e8}");
            sb.AppendLine("section.oledb{border-top-color:#d97706}");
            sb.AppendLine("section.linq{border-top-color:#0d9488}");
            sb.AppendLine("h2{font-size:.98rem;margin:0 0 .55rem;color:#333;font-weight:600}");
            sb.AppendLine("h2 .badge{color:#fff;font-size:.68rem;padding:2px 7px;border-radius:3px;margin-right:.5rem;vertical-align:middle;letter-spacing:.3px;background:#888}");
            sb.AppendLine("section.odbc h2 .badge{background:#1a73e8}");
            sb.AppendLine("section.oledb h2 .badge{background:#d97706}");
            sb.AppendLine("section.linq h2 .badge{background:#0d9488}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;font-size:.82rem}");
            sb.AppendLine("th,td{padding:.28rem .55rem;text-align:left;border-bottom:1px solid #eee}");
            sb.AppendLine("th{background:#fafbfc;font-weight:600;color:#666;font-size:.72rem;text-transform:uppercase;letter-spacing:.4px}");
            sb.AppendLine("tr:hover td{background:#f7faff}");
            sb.AppendLine(".num{text-align:right;font-variant-numeric:tabular-nums}");
            sb.AppendLine(".error{color:#b00020;background:#fff0f0;padding:.55rem .7rem;border-radius:4px;border-left:3px solid #b00020;font-size:.85rem}");
            sb.AppendLine(".empty{color:#999;font-style:italic;font-size:.85rem}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head><body>");
            sb.AppendLine("<h1>Лабораторная работа № 2 - База магазина товаров для котиков</h1>");
            sb.AppendLine($"<div class=\"meta\">База <b>petshop</b> (PostgreSQL) через ODBC, OleDb и LINQ. Создано: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</div>");

            sb.AppendLine("<section class=\"odbc\">");
            sb.AppendLine("<h2><span class=\"badge\">ODBC</span>Список товаров (JOIN с категориями)</h2>");
            if (odbcError != null)
                sb.AppendLine($"<div class=\"error\">Ошибка ODBC: {Esc(odbcError)}</div>");
            else if (products == null || products.Count == 0)
                sb.AppendLine("<div class=\"empty\">Нет данных.</div>");
            else
            {
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th>Id</th><th>Название</th><th>Категория</th><th class=\"num\">Цена</th><th class=\"num\">Остаток</th></tr></thead><tbody>");
                foreach (var p in products)
                    sb.AppendLine($"<tr><td>{p.Id}</td><td>{Esc(p.Name)}</td><td>{Esc(p.Category)}</td><td class=\"num\">{p.Price:N2}</td><td class=\"num\">{p.Stock}</td></tr>");
                sb.AppendLine("</tbody></table>");
            }
            sb.AppendLine("</section>");

            sb.AppendLine("<section class=\"oledb\">");
            sb.AppendLine("<h2><span class=\"badge\">OleDb</span>Список заказов (через CSV-мост)</h2>");
            if (oleError != null)
                sb.AppendLine($"<div class=\"error\">Ошибка OleDb: {Esc(oleError)}</div>");
            else if (orders == null || orders.Count == 0)
                sb.AppendLine("<div class=\"empty\">Нет данных.</div>");
            else
            {
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th>#</th><th>Покупатель</th><th class=\"num\">Сумма</th><th>Статус</th><th>Дата</th></tr></thead><tbody>");
                foreach (var o in orders)
                    sb.AppendLine($"<tr><td>{o.Id}</td><td>{Esc(o.Customer)}</td><td class=\"num\">{o.Total:N2}</td><td>{Esc(o.Status)}</td><td>{o.CreatedAt:yyyy-MM-dd}</td></tr>");
                sb.AppendLine("</tbody></table>");
            }
            sb.AppendLine("</section>");

            sb.AppendLine("<section class=\"linq\">");
            sb.AppendLine("<h2><span class=\"badge\">LINQ</span>Статистика по пользователям</h2>");
            if (linqError != null)
                sb.AppendLine($"<div class=\"error\">Ошибка LINQ: {Esc(linqError)}</div>");
            else if (linq == null || linq.Stats == null || linq.Stats.Count == 0)
                sb.AppendLine("<div class=\"empty\">Нет данных.</div>");
            else
            {
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th>Имя</th><th>Email</th><th class=\"num\">Заказов</th><th class=\"num\">Потрачено</th></tr></thead><tbody>");
                foreach (var s in linq.Stats)
                    sb.AppendLine($"<tr><td>{Esc(s.Name)}</td><td>{Esc(s.Email)}</td><td class=\"num\">{s.OrdersCount}</td><td class=\"num\">{s.TotalSpent:N2}</td></tr>");
                sb.AppendLine("</tbody></table>");
            }
            sb.AppendLine("</section>");

            if (linqError == null && linq != null && linq.Recent != null && linq.Recent.Count > 0)
            {
                sb.AppendLine("<section class=\"linq\">");
                sb.AppendLine("<h2><span class=\"badge\">LINQ</span>Последние 3 заказа</h2>");
                sb.AppendLine("<table>");
                sb.AppendLine("<thead><tr><th>#</th><th>Покупатель</th><th class=\"num\">Сумма</th><th>Статус</th><th>Дата</th></tr></thead><tbody>");
                foreach (var o in linq.Recent)
                    sb.AppendLine($"<tr><td>{o.Id}</td><td>{Esc(o.Customer)}</td><td class=\"num\">{o.Total:N2}</td><td>{Esc(o.Status)}</td><td>{o.CreatedAt:yyyy-MM-dd HH:mm}</td></tr>");
                sb.AppendLine("</tbody></table>");
                sb.AppendLine("</section>");
            }

            sb.AppendLine("</body></html>");
            File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(false));
        }

        static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        public static void OpenInBrowser(string path)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось открыть браузер: " + ex.Message);
            }
        }
    }
}
