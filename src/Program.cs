using IggPlayer.Components;
using IggPlayer.Data;
using IggPlayer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<MusicDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("MusicDb") ?? "Data Source=music.db"));

builder.Services.AddDbContextFactory<PlayLogDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("PlayLogDb") ?? "Data Source=playlog.db"));

builder.Services.AddSingleton<FileStreamService>();
builder.Services.AddScoped<MusicService>();
builder.Services.AddScoped<PlayLogService>();
builder.Services.AddScoped<LibraryScanService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    EnsureDatabaseCurrent<MusicDbContext>(scope);
    EnsureDatabaseCurrent<PlayLogDbContext>(scope);
}

static void EnsureDatabaseCurrent<T>(IServiceScope scope) where T : DbContext
{
    var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<T>>().CreateDbContext();
    try
    {
        db.Database.EnsureCreated();
        foreach (var entityType in db.Model.GetEntityTypes())
        {
            db.Database.ExecuteSqlRaw($"SELECT * FROM \"{entityType.GetTableName()}\" LIMIT 0");
        }
    }
    catch
    {
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapGet("/api/stream/{id:int}", async (int id, HttpContext httpContext, IDbContextFactory<MusicDbContext> dbFactory, FileStreamService fileService) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var track = await db.Tracks.FindAsync(id);
    if (track is null)
        return Results.NotFound();

    string fullPath;
    try
    {
        fullPath = fileService.ResolvePath(track.RelativePath);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }

    if (!File.Exists(fullPath))
        return Results.NotFound("File not found on disk.");

    var fileInfo = new FileInfo(fullPath);
    var fileLength = fileInfo.Length;
    var lastModified = fileInfo.LastWriteTimeUtc;

    httpContext.Response.Headers[HeaderNames.AcceptRanges] = "bytes";
    httpContext.Response.Headers[HeaderNames.LastModified] = lastModified.ToString("R");

    var rangeHeader = httpContext.Request.Headers[HeaderNames.Range].FirstOrDefault();
    if (rangeHeader is not null && rangeHeader.StartsWith("bytes="))
    {
        var rangeSpec = rangeHeader["bytes=".Length..];
        var parts = rangeSpec.Split('-');
        var start = long.TryParse(parts[0], out var s) ? s : 0;
        var end = parts.Length > 1 && long.TryParse(parts[1], out var e) ? e : fileLength - 1;

        if (start >= fileLength || end >= fileLength || start > end)
            return Results.StatusCode(416);

        var length = end - start + 1;
        httpContext.Response.StatusCode = 206;
        httpContext.Response.Headers[HeaderNames.ContentRange] = $"bytes {start}-{end}/{fileLength}";
        httpContext.Response.ContentLength = length;
        httpContext.Response.ContentType = "audio/mpeg";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
        stream.Seek(start, SeekOrigin.Begin);
        return Results.Stream(stream, "audio/mpeg", enableRangeProcessing: false);
    }

    var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, useAsync: true);
    return Results.Stream(fileStream, "audio/mpeg", enableRangeProcessing: true);
});

app.MapPost("/api/scan", async (LibraryScanService scanService) =>
{
    var (added, skipped, failed) = await scanService.ScanAsync();
    return Results.Ok(new { added, skipped, failed });
});

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
