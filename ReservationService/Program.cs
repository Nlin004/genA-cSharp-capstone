using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ReservationService.Data;
using ReservationService.Infrastructure.JsonConverters;
using ReservationService.Jobs;
using ReservationService.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers + JSON ────────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new ScreamingSnakeEnumConverterFactory());
    });

// ── Swagger ───────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<ReservationServiceContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<ReservationServiceContext>(options =>
        options.UseInMemoryDatabase("ReservationServiceDb"));
}

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret must be configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer must be configured.");
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience must be configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── HttpClient: UserService ───────────────────────────────────────────────────
var userServiceUrl = builder.Configuration["ServiceUrls:UserService"]
    ?? "http://localhost:5001";

builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(userServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ── HttpClient: CatalogService ────────────────────────────────────────────────
var catalogServiceUrl = builder.Configuration["ServiceUrls:CatalogService"]
    ?? "http://localhost:5002";

builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(catalogServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IWaitlistCascadeService, WaitlistCascadeService>();

// ── Background Job ────────────────────────────────────────────────────────────
builder.Services.AddHostedService<WaitlistExpiryJob>();

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-migrate on startup (production only) ─────────────────────────────────
if (!string.IsNullOrWhiteSpace(connectionString))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReservationServiceContext>();
    db.Database.Migrate();
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

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();