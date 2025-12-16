
// File: Services/AuthService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShareSphere.Api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ShareSphere.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _config;

        public AuthService(UserManager<ApplicationUser> userManager,
                           RoleManager<IdentityRole> roleManager,
                           IConfiguration config)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _config = config;
        }

        public async Task<RegisterResult> RegisterAsync(string userName, string displayName, string password, string[] roles)
        {
            // 1) Existiert der Benutzer schon?
            var existing = await _userManager.FindByNameAsync(userName);
            if (existing is not null)
            {
                return new RegisterResult { Succeeded = false, Errors = new[] { "UserNameAlreadyExists" } };
            }

            // 2) Benutzer anlegen
            var user = new ApplicationUser
            {
                UserName = userName,
                DisplayName = displayName
            };

            var create = await _userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                return new RegisterResult
                {
                    Succeeded = false,
                    Errors = create.Errors.Select(e => e.Description).ToArray()
                };
            }

            // 3) Rollen normalisieren und prüfen
            var normalizedRoles = (roles ?? Array.Empty<string>())
                                  .Select(r => r.Trim().ToLowerInvariant())
                                  .Where(r => !string.IsNullOrWhiteSpace(r))
                                  .DefaultIfEmpty("user") // Defaultrolle, falls nichts übergeben
                                  .ToArray();

            System.Console.WriteLine(normalizedRoles);

            foreach (var role in normalizedRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    // Falls du fehlende Rollen automatisch erstellen willst, könntest du hier:
                    // await _roleManager.CreateAsync(new IdentityRole(role));
                    // Ich empfehle für klare Kontrolle: Fehler zurückgeben.
                    return new RegisterResult { Succeeded = false, Errors = new[] { $"RoleNotFound:{role}" } };
                }
            }

            var addRoles = await _userManager.AddToRolesAsync(user, normalizedRoles);
            if (!addRoles.Succeeded)
            {
                return new RegisterResult
                {
                    Succeeded = false,
                    Errors = addRoles.Errors.Select(e => e.Description).ToArray()
                };
            }

            // 4) JWT erstellen
            var token = await CreateTokenAsync(user);
            return new RegisterResult { Succeeded = true, Token = token };
        }

        public async Task<string?> LoginAsync(string userName, string password)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user is null) return null;

            var valid = await _userManager.CheckPasswordAsync(user, password);
            if (!valid) return null;

            return await CreateTokenAsync(user);
        }

        private async Task<string> CreateTokenAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),               // User-ID
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName), // Username
                new Claim("displayName", user.DisplayName)                     // eigener Claim
            };

            foreach (var r in roles)
                claims.Add(new Claim("role", r)); // wichtig für [Authorize(Roles="…")]

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var jwt = new JwtSecurityToken(
                issuer: "ShareSphere",
                audience: "ShareSphereClient",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
