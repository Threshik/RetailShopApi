using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RetailShopApi.Data;
using RetailShopApi.Services;
using RetailShopApi.Services.Implementation;
using RetailShopApi.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// ---------------------- Services ---------------------- //

// Controllers
builder.Services.AddControllers();

// Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Retail Shop API",
        Version = "v1"
    });

    // JWT Bearer security scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter JWT Bearer token. Example: `Bearer <token>`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "MyAppRedisInstance:";
});

// ---------------------- JWT Keycloak ---------------------- //

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "http://localhost:8080/realms/demo";
        options.Audience = "retail-app";
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = false,
            NameClaimType = "preferred_username",
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine("Authentication Failed: " + ctx.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = ctx =>
            {
                var identity = ctx.Principal?.Identity as System.Security.Claims.ClaimsIdentity;

                // Extract realm roles
                var realmAccess = ctx.Principal?.FindFirst("realm_access")?.Value;
                if (!string.IsNullOrEmpty(realmAccess))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(realmAccess);
                        if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    identity?.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing realm_access: " + ex.Message);
                    }
                }

                // Extract client roles (retail-app)
                var resourceAccess = ctx.Principal?.FindFirst("resource_access")?.Value;
                if (!string.IsNullOrEmpty(resourceAccess))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(resourceAccess);
                        if (doc.RootElement.TryGetProperty("retail-app", out var clientAccess) &&
                            clientAccess.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                var roleName = role.GetString();
                                if (!string.IsNullOrEmpty(roleName))
                                {
                                    identity?.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, roleName));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error parsing resource_access: " + ex.Message);
                    }
                }

                // Debug output
                Console.WriteLine($"Token validated for user: {ctx.Principal.Identity?.Name}");
                var roles = identity?.Claims
                    .Where(c => c.Type == System.Security.Claims.ClaimTypes.Role)
                    .Select(c => c.Value);
                Console.WriteLine("➡️ Roles: " + string.Join(", ", roles ?? Array.Empty<string>()));

                return Task.CompletedTask;
            }
        };
    });

// ---------------------- Authorization ---------------------- //

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
});

// ---------------------- Application Services ---------------------- //

builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<KeycloakService>();



// ---------------------- App Pipeline ---------------------- //

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Retail Shop API v1");
    });
}

// Middleware
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication(); // Must come before Authorization
app.UseAuthorization();
app.MapControllers();

app.Run();
