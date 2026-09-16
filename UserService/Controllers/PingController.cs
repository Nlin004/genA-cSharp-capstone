using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    private readonly UserServiceContext _context;

    public PingController(UserServiceContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        // Touch the context so a misconfigured DbContext surfaces here.
        var canConnect = _context.Database.IsInMemory();

        return Ok(new
        {
            service = "UserService",
            status = "healthy",
            port = 5001,
            database = "UserServiceDb (InMemory)",
            inMemory = canConnect,
            timestampUtc = DateTime.UtcNow
        });
    }
}