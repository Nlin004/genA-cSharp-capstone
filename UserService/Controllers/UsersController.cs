using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Enums;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserServiceContext _db;
    private readonly IReservationServiceClient _reservationClient;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        UserServiceContext db,
        IReservationServiceClient reservationClient,
        ILogger<UsersController> logger)
    {
        _db = db;
        _reservationClient = reservationClient;
        _logger = logger;
    }

    // GET /api/users/profile
    // Requires: valid JWT Bearer token
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        // Extract userId from JWT claims (set as "userId" in TokenService)
        var userIdClaim = User.FindFirstValue("userId");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "UNAUTHORIZED",
                Message = "Invalid or missing token claims."
            });
        }

        var user = await _db.Users.FindAsync(userId);
        if (user is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "USER_NOT_FOUND",
                Message = "User not found."
            });
        }

        // Call Reservation Service — gracefully degrade if unavailable
        var stats = await _reservationClient.GetStatisticsAsync(userId);

        return Ok(new UserProfileResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            MemberSince = user.MemberSince,
            ActiveReservations = stats?.ActiveReservations ?? 0,
            BorrowingHistory = stats?.BorrowingHistory ?? 0
        });
    }

    // GET /api/users/{userId}/validate
    // Internal — called by Reservation Service to verify a user before creating a reservation
    [HttpGet("{userId:guid}/validate")]
    public async Task<IActionResult> ValidateUser(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user is null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "USER_NOT_FOUND",
                Message = $"No user found with ID {userId}."
            });
        }

        if (user.MembershipStatus != MembershipStatus.Active)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "USER_SUSPENDED",
                Message = "This user's membership is suspended."
            });
        }

        // Get their current active reservation count from Reservation Service
        var stats = await _reservationClient.GetStatisticsAsync(userId);

        return Ok(new UserValidateResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            ActiveReservationsCount = stats?.ActiveReservations ?? 0
        });
    }
}
