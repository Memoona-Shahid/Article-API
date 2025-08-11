using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WebApi1.Data;
using WebApi1.Helper;
using WebApi1.Model;

namespace WebApi1.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ArticleDbContext _context;
    private readonly IConfiguration _config;
    //CONSTRUCTOR TO INJECT DEPENDENCIES
    public AuthController(ArticleDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    //METHODS TO HANDLE USER REGISTRATION 
    [HttpPost("register")]
    public async Task<IActionResult> Register(User request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            return BadRequest("User already exists");

        using var sha = SHA256.Create();
        var hash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(request.PasswordHash)));

        var user = new User
        {
            Username = request.Username,
            PasswordHash = hash
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return Ok("User Created successfully");
    }

    //METHODS TO HANDLE USER LOGIN
    [HttpPost("login")]
    public async Task<IActionResult> Login(User request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null) return Unauthorized("Invalid username");

        using var sha = SHA256.Create();
        var incomingHash = Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(request.PasswordHash)));

        if (user.PasswordHash != incomingHash)
            return Unauthorized("Invalid password");

        var accessToken = JwtHelper.GenerateToken(user.Username, _config);
        var refreshToken = GenerateRefreshToken();

        user.token = accessToken;
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); //7days_expiry for refresh token
        await _context.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            Token = accessToken,
            RefreshToken = refreshToken
        });

    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(TokenRequest tokenRequest)
    {
        var principal = GetPrincipalFromExpiredToken(tokenRequest.AccessToken);
        var username = principal.Identity?.Name;

        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.Username == username &&
            u.RefreshToken == tokenRequest.RefreshToken &&
            u.RefreshTokenExpiryTime > DateTime.UtcNow);

        if (user == null)
            return BadRequest("Invalid refresh token");

        var newAccessToken = JwtHelper.GenerateToken(user.Username, _config);
        var newRefreshToken = GenerateRefreshToken();

        user.token = newAccessToken;
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        });
    }
    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }


    private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JwtSettings:Key"]!)),
            ValidateLifetime = false // Ignore expiry
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;
    }


}
