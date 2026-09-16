// filename: CatalogService/Controllers/PingController.cs
using Microsoft.AspNetCore.Mvc;
using CatalogService.Data;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    private readonly CatalogServiceContext _context;

    public PingController(CatalogServiceContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var canConnect = _context.Database.IsInMemory();

        return Ok(new
        {
            service = "CatalogService",
            status = "healthy",
            port = 5002,
            database = "CatalogServiceDb (InMemory)",
            inMemory = canConnect,
            timestampUtc = DateTime.UtcNow
        });
    }
}