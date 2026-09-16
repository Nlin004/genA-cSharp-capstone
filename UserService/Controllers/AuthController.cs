using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.DTOs;
using UserService.Enums;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserServiceContext _db;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;

    public AuthController(
        UserServiceContext db,
        IPasswordService passwordService,
        ITokenService tokenService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        ILogger<AuthController> logger,
        IConfiguration config)
    {
        _db = db;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _logger = logger;
        _config = config;
    }

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // 1. FluentValidation
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "VALIDATION_ERROR",
                Message = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))
            });
        }

        // 2. Email uniqueness check
        var emailExists = await _db.Users
            .AnyAsync(u => u.Email.ToLower() == request.Email.ToLower());
        if (emailExists)
        {
            return BadRequest(new ErrorResponse
            {
                Error = "VALIDATION_ERROR",
                Message = "An account with this email address already exists."
            });
        }

        // 3. Build and persist the user
        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = request.Email.Trim().ToLower(),
            PasswordHash = _passwordService.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("New user registered: {Email} ({UserId})", user.Email, user.UserId);

        return StatusCode(StatusCodes.Status201Created, new RegisterResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role,
            MembershipStatus = user.MembershipStatus,
            CreatedAt = user.CreatedAt,
            Message = "Registration successful."
        });
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // 1. Basic field validation
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Unauthorized(new ErrorResponse
            {
                Error = "AUTHENTICATION_FAILED",
                Message = "Invalid email or password."
            });
        }

        // 2. Look up user — intentionally vague on which field failed (security best practice)
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower());

        if (user is null || !_passwordService.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
            return Unauthorized(new ErrorResponse
            {
                Error = "AUTHENTICATION_FAILED",
                Message = "Invalid email or password."
            });
        }

        // 3. Generate token
        var token = _tokenService.GenerateToken(user);
        var expiresIn = int.Parse(_config["Jwt:ExpiresInSeconds"] ?? "86400");

        _logger.LogInformation("Successful login for user {UserId}", user.UserId);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = expiresIn,
            User = new LoginUserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                MembershipStatus = user.MembershipStatus
            }
        });
    }
}
