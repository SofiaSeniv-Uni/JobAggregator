using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobScraper.Web.Data;
using JobScraper.Web.Entities;
using JobScraper.Web.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JobScraper.Web.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email, ct))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var user = new User
        {
            Username     = request.Username,
            Email        = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(GenerateToken(user), user.Username);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        return !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash) 
            ? throw new UnauthorizedAccessException("Invalid credentials") 
            : new AuthResponse(GenerateToken(user), user.Username);
    }

    private string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                _config["Jwt:Secret"] ?? "super-secret-key-min-32-chars!!"));
        var creds = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier,
                    user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            },
            expires:           DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}