using Microsoft.EntityFrameworkCore;
using CatalogService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Milestone 1: in-memory provider for local development.
// Swapped for Npgsql/PostgreSQL (RDS) in the deployment milestone.
builder.Services.AddDbContext<CatalogServiceContext>(options =>
    options.UseInMemoryDatabase("CatalogServiceDb"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();