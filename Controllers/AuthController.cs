using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BookMateHub.Api.Data;
using BookMateHub.Api.Models;
using BookMateHub.Api.Services;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly EmailService _emailService;

    public AuthController(ApplicationDbContext context, IConfiguration configuration, EmailService emailService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            return BadRequest(new { message = "Email already registered" });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash 
            ?? throw new ArgumentNullException(nameof(user.PasswordHash)));
        user.AuthProvider = "email";
        user.IsEmailConfirmed = false;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var confirmationLink = $"https://yourdomain.com/api/auth/confirm-email?email={user.Email}";
        _emailService.SendEmail(
            user.Email ?? throw new ArgumentNullException(nameof(user.Email)),
            "Confirm your email",
            $"Please confirm your email by clicking <a href='{confirmationLink}'>here</a>.");

        return Ok(new { message = "User registered. Please confirm your email." });
    }

    [HttpGet("confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return NotFound(new { message = "User not found" });

        user.IsEmailConfirmed = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Email confirmed!" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User user)
    {
        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
        if (dbUser == null || !BCrypt.Net.BCrypt.Verify(user.PasswordHash ?? "", dbUser.PasswordHash))
            return Unauthorized(new { message = "Invalid credentials" });

        if (!dbUser.IsEmailConfirmed)
            return BadRequest(new { message = "Email not confirmed" });

        var token = GenerateJwtToken(dbUser);
        return Ok(new { Token = token });
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email 
                ?? throw new ArgumentNullException(nameof(user.Email))),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] 
            ?? throw new ArgumentNullException("Jwt:Key")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            _configuration["Jwt:Issuer"],
            _configuration["Jwt:Audience"],
            claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
