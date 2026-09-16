using Microsoft.EntityFrameworkCore;
using CatalogService.Data;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers ───────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<CatalogServiceContext>(options =>
        options.UseInMemoryDatabase("CatalogServiceDb"));
}

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-migrate and seed on startup (production only) ───────────────────────
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogServiceContext>();
    db.Database.Migrate();
    CatalogServiceSeeder.Seed(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();