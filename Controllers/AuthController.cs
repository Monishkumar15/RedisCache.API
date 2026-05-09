using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RedisCache.API.Models;

namespace RedisCache.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController(IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Login with username and password to receive a JWT Bearer token.
    /// Use the token in the Authorize button (top right) to access protected endpoints.
    /// Default credentials — username: admin | password: admin123
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var validUsername = config["AdminCredentials:Username"];
        var validPassword = config["AdminCredentials:Password"];

        if (request.Username != validUsername || request.Password != validPassword)
            return Unauthorized("Invalid username or password.");

        var token = GenerateToken(request.Username);
        return Ok(new { token });
    }

    private string GenerateToken(string username)
    {
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var signing = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer:             config["Jwt:Issuer"],
            audience:           config["Jwt:Audience"],
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:ExpiryMinutes"]!)),
            signingCredentials: signing
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
