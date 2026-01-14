using System.Security.Claims;
using System.Text;
using AvitoBackend.Data;
using AvitoBackend.GraphQL.Queries;
using AvitoBackend.GraphQL.Types;
using AvitoBackend.Models.Core;
using AvitoBackend.Models.NoSQL;
using AvitoBackend.Services;
using AvitoBackend.Services.Cache;
using AvitoBackend.Services.Payment;
using AvitoBackend.Services.Storage;
using AvitoLite.HostedServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// === DbContext ===
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// === Identity ===
builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(opt =>
{
    opt.Password.RequireDigit = false;
    opt.Password.RequireLowercase = false;
    opt.Password.RequireUppercase = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.Password.RequiredLength = 4;
})

.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// === Authentication (КРИТИЧЕСКИ ВАЖНО: правильный порядок) ===
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var jwtKey = builder.Configuration["Jwt:Key"]!;
    var keyBytes = Encoding.ASCII.GetBytes(jwtKey);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true
    };
})
.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
{
    var googleSection = builder.Configuration.GetSection("Google");
    options.ClientId = googleSection["ClientId"]!;
    options.ClientSecret = googleSection["ClientSecret"]!;
    options.CallbackPath = "/signin-google";
});

// === Redis ===
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(builder.Configuration["Redis"] ?? "localhost"));
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// === MongoDB ===
var mongoClient = new MongoClient(builder.Configuration.GetConnectionString("Mongo"));
var mongoDb = mongoClient.GetDatabase("avito");
builder.Services.AddSingleton(mongoDb.GetCollection<AdvertisementDescription>("ad_descriptions"));

// === MinIO ===
builder.Services.AddScoped<IFileStorageService, MinioFileStorageService>();

// === Payment ===
builder.Services.AddScoped<IPromotionService, PromotionService>();

// === AuthService ===
builder.Services.AddScoped<IAuthService, AuthService>();

// === GraphQL ===
builder.Services.AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true)
    .AddTypeExtension<AdvertisementQuery>()
    .AddType<AdvertisementType>()
    .AddType<UserType>();

// === Controllers ===
builder.Services.AddControllers();

// === Swagger ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Avito Clone API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {your token}'"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// === Hosted Services ===
builder.Services.AddHostedService<AdminSeedService>();


var app = builder.Build();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Google OAuth endpoints
app.MapGet("/login-google", () => Results.Challenge(
    new AuthenticationProperties { RedirectUri = "/google-callback" },
    new[] { GoogleDefaults.AuthenticationScheme }
));

app.MapGet("/google-callback", async (
    HttpContext ctx,
    UserManager<AppUser> userManager,
    IAuthService authService) =>
{
    Console.WriteLine("\n=== GOOGLE AUTH DIAGNOSTICS START ===");

    var authResult = await ctx.AuthenticateAsync(
        IdentityConstants.ExternalScheme
    );

    if (!authResult.Succeeded || authResult.Principal == null)
    {
        Console.WriteLine(" EXTERNAL AUTH FAILED");
        Console.WriteLine("=== GOOGLE AUTH DIAGNOSTICS END ===\n");
        return Results.Redirect("/login.html?error=google_auth_failed");
    }

    var principal = authResult.Principal;

    foreach (var id in principal.Identities)
    {
        Console.WriteLine($"Identity: {id.AuthenticationType}, Auth={id.IsAuthenticated}");
    }

    var email = principal.FindFirstValue(ClaimTypes.Email);

    if (string.IsNullOrEmpty(email))
    {
        Console.WriteLine("EMAIL NOT FOUND");
        return Results.Redirect("/login.html?error=no_email");
    }

    Console.WriteLine($"Google auth OK: {email}");

    var user = await userManager.FindByEmailAsync(email);
    if (user == null)
    {
        user = new AppUser
        {
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            return Results.Redirect("/login.html?error=user_create_failed");
        }
    }

    var tokens = await authService.LoginWithGoogleAsync(email);

    await ctx.SignOutAsync(IdentityConstants.ExternalScheme);

    Console.WriteLine("JWT ISSUED");
    Console.WriteLine("=== GOOGLE AUTH DIAGNOSTICS END ===\n");

    return Results.Redirect(
        $"/login.html?token={Uri.EscapeDataString(tokens.JwtToken)}&refreshToken={Uri.EscapeDataString(tokens.RefreshToken)}"
    );
});



app.MapPost("/api/auth/refresh", async (HttpContext ctx, IAuthService authService) =>
{
    var refreshToken = ctx.Request.Headers["RefreshToken"].ToString();
    if (string.IsNullOrEmpty(refreshToken))
        return Results.BadRequest("RefreshToken header is required");

    try
    {
        var tokens = await authService.RefreshTokenAsync(refreshToken);
        return Results.Json(new { token = tokens.JwtToken, refreshToken = tokens.RefreshToken });
    }
    catch (Exception ex)
    {
        return Results.Unauthorized();
    }
});

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Avito Clone API v1");
        c.RoutePrefix = string.Empty; 
    });
}

app.MapGraphQL();

app.Run();