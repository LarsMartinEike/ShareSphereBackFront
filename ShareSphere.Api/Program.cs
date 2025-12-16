
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShareSphere.Api.Data;
using ShareSphere.Api.Models;
using ShareSphere.Api.Services;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// ---- 0) CORS: erlaubte Origins aus Konfiguration, Fallback für lokale Dev-Server ----
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? new[] { "http://localhost:5173", "http://localhost:3000" };

// ---- 1) Datenbank ----
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// ---- 2) Identity ----
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ---- 3) JWT ----
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ---- 4) Controllers ----
builder.Services.AddControllers();

// ---- 5) Swagger mit JWT ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opt =>
{
    opt.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ShareSphere API",
        Version = "v1",
        Description = "WebAPI mit Identity + JWT"
    });

    // Bearer-JWT Schema
    var securitySchema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT im Format: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };

    opt.AddSecurityDefinition("Bearer", securitySchema);

    var securityRequirement = new OpenApiSecurityRequirement
    {
        { securitySchema, Array.Empty<string>() }
    };
    opt.AddSecurityRequirement(securityRequirement);
});

// ---- 6) CORS-Policy für React-Frontend ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
        // .AllowCredentials(); // nur nötig, wenn du Cookies (z. B. für Auth) nutzt
    });
});

// ---- 7) eigenen AuthService registrieren ----
builder.Services.AddScoped<ShareSphere.Api.Services.IAuthService, ShareSphere.Api.Services.AuthService>();
builder.Services.AddScoped<IStockExchangeService, StockExchangeService>();
var app = builder.Build();

Console.WriteLine($"API läuft auf: {string.Join(", ", app. Urls)}");
// ---- 8) Rollen seeden ----
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services. GetRequiredService<RoleManager<IdentityRole>>();
        
        await DbInitializer.SeedAdminUser(userManager, roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}


async Task SeedRolesAsync(IServiceProvider sp)
{
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
    string[] roles = ["user", "admin"];

    foreach (var r in roles)
    {
        if (!await roleManager.RoleExistsAsync(r))
        {
            var create = await roleManager.CreateAsync(new IdentityRole(r));
            if (!create.Succeeded)
            {
                throw new Exception($"Rolle '{r}' konnte nicht erstellt werden: " +
                    string.Join(", ", create.Errors.Select(e => e.Description)));
            }
        }
    }
}

using (var scope = app.Services.CreateScope())
{
    await SeedRolesAsync(scope.ServiceProvider);
}

// ---- 9) Middleware-Reihenfolge ----

// Bei Betrieb hinter Proxy (z. B. später in Azure/AppService) korrekte Weiterleitungen
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// **Wichtig**: CORS vor Auth/Authorization, damit Preflight-Requests sauber durchgehen
app.UseCors("ClientCors");

app.UseAuthentication();
app.UseAuthorization();

// API-Endpunkte
app.MapControllers();

// Optional: SPA-Hosting für Produktion (wenn du das React-Build nach wwwroot kopierst)
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
    app.MapFallbackToFile("index.html");
}

app.Run();