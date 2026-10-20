using System.Diagnostics;
using Juleplanlaegning.Components;
using Juleplanlaegning.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

// A published app keeps wwwroot, settings and the database next to the .exe. Use that folder
// regardless of the working directory, so it also works from shortcuts. `dotnet run` is unaffected.
var exeDir = AppContext.BaseDirectory;
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = Directory.Exists(Path.Combine(exeDir, "wwwroot")) ? exeDir : null,
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// SQLite resolves relative paths against the working directory; anchor the database file to the content root instead.
var connection = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Planner"));
connection.DataSource = Path.Combine(builder.Environment.ContentRootPath, connection.DataSource);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connection.ToString()));
builder.Services.AddScoped<PlannerService>();
builder.Services.AddScoped<TodoService>();

var app = builder.Build();

// Create or upgrade the SQLite database on startup.
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = dbFactory.CreateDbContext();
    DatabaseSetup.BaselineLegacyDatabase(db);
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Serves the uploaded map background straight from the database.
app.MapGet("/map-image", async (PlannerService planner) =>
{
    var image = await planner.GetMapImageAsync();
    return image is null ? Results.NotFound() : Results.File(image.Data, image.ContentType);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// When run as a published app (e.g. double-clicking the .exe), open the planner in the default browser.
if (!app.Environment.IsDevelopment() && app.Configuration.GetValue("OpenBrowser", true))
{
    app.Lifetime.ApplicationStarted.Register(() =>
    {
        var url = app.Urls.FirstOrDefault();
        if (url is not null)
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    });
}

app.Run();
