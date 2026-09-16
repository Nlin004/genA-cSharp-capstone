// filename: ReservationService/Controllers/PingController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationService.Data;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PingController : ControllerBase
{
    private readonly ReservationServiceContext _context;

    public PingController(ReservationServiceContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var canConnect = _context.Database.IsInMemory();

        return Ok(new
        {
            service = "ReservationService",
            status = "healthy",
            port = 5003,
            database = "ReservationServiceDb (InMemory)",
            inMemory = canConnect,
            reservationCount = _context.Reservations.Count(),
            waitlistCount = _context.Waitlists.Count(),
            timestampUtc = DateTime.UtcNow
        });
    }
}