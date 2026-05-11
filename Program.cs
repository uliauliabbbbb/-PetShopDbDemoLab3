using Microsoft.EntityFrameworkCore;
using PetShopDbDemo;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// Гашу стандартные логи Kestrel, иначе в консоли некрасиво,
// и не видно мои демо-блоки из прошлой лабораторной.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => { o.SingleLine = true; });
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// Регистрирую сервисы. PetShopContext будет создаваться на каждый запрос,
// AppState один на всё приложение (там у меня результаты демо и журнал).
builder.Services.AddDbContext<PetShopContext>(opts =>
    opts.UseNpgsql(PetShopContext.DefaultConnectionString));
builder.Services.AddSingleton<AppState>();

// Привязываюсь к фиксированному адресу. Так удобнее открывать вручную
// и делать скриншоты для отчёта.
const string Url = "http://localhost:5050";
builder.WebHost.UseUrls(Url);

var app = builder.Build();

// Сначала прогоняю три блока из прошлой лабораторной (ODBC, OleDb, LINQ).
// Это нужно сделать до app.Run(), иначе сервер заблокирует поток.
var state = app.Services.GetRequiredService<AppState>();
Demos.RunAll(state);

// Эти две строчки нужны, чтобы по адресу "/" показывалась моя страничка
// из wwwroot/index.html, а CSS и JS подтягивались автоматически.
app.UseDefaultFiles();
app.UseStaticFiles();

// Текущая лабораторная: подключаю маршруты для CRUD по товарам.
ProductEndpoints.Map(app);

Console.WriteLine();
Console.WriteLine("=".PadRight(70, '='));
Console.WriteLine("4. Веб-интерфейс CRUD (EF Core + ASP.NET Core)");
Console.WriteLine("=".PadRight(70, '='));
Console.WriteLine($"Откройте в браузере: {Url}/");

try
{
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Url + "/") { UseShellExecute = true });
}
catch (Exception ex)
{
    Console.WriteLine("Не удалось открыть браузер автоматически: " + ex.Message);
}

app.Run();
