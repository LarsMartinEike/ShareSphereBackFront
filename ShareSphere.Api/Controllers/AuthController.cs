
// File: Controllers/AuthController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShareSphere.Api.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ShareSphere.Api.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth) => _auth = auth;

        // DTOs mit Validierung
        public record RegisterRequest(
            [Required, MinLength(3)] string UserName,
            [Required, MinLength(3), MaxLength(64)] string DisplayName,
            [Required, MinLength(6)] string Password
        );

        public record LoginRequest(
            [Required] string UserName,
            [Required] string Password
        );

        /// <summary>
        /// Registriert einen Benutzer und weist (optionale) Rollen zu.
        /// Gibt ein JWT zurück, das u.a. die Rollen als Claims enthält.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            // Falls keine Rollen übergeben wurden, Standard-Rolle "user" setzen

            var result = await _auth.RegisterAsync(req.UserName, req.DisplayName, req.Password, new[] {"user"});
            if (!result.Succeeded)
            {
                // Einheitliches Fehlerformat (Identity-Fehler, doppelte Usernames, falsche Rollen)
                return BadRequest(new { errors = result.Errors });
            }

            return Ok(new { token = result.Token });
        }

        /// <summary>
        /// Login, gibt JWT zurück.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var token = await _auth.LoginAsync(req.UserName, req.Password);
            if (token is null) return Unauthorized();
            return Ok(new { token });
        }

        // Beispiel: geschützter Endpunkt für beliebige authentifizierte User

            [Authorize]
            [HttpGet("me")]
            public IActionResult Me()
            {
                var roles = User.Claims
                    .Where(c => c.Type == ClaimTypes.Role 
                            || c.Type == "role" 
                            || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToArray(); 

                return Ok(new {
                    name = User.Identity?.Name,
                    displayName = User.Claims.FirstOrDefault(c => c.Type == "displayName")?.Value,
                    roles
                });
            }


        // Beispiel: nur Admins
        [Authorize(Roles = "admin")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly() => Ok("Nur Admins sehen das.");
    }
}
