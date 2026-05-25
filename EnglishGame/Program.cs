// Program.cs
using System.Security.Claims;
using System.Text;
using EnglishGame.Data;
using EnglishGame.Services;
using EnglishGame.Services.Ai;
using EnglishGame.Services.Vocabulary;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<GameService>();

// Vocabulary / Free Dictionary API layer
builder.Services.AddHttpClient<FreeDictionaryClient>();
builder.Services.AddScoped<VocabularyService>();

// AI provider switch: Mock hoặc OpenAI-compatible
var aiProvider = builder.Configuration["Ai:Provider"] ?? "Mock";

if (aiProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
{
    // ✅ Tăng timeout lên 5 phút vì single-call AI generate có thể mất ~2 phút
    builder.Services.AddHttpClient<OpenAiCompatibleGenerator>(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(10);
    });
    builder.Services.AddScoped<IAiGenerator, OpenAiCompatibleGenerator>();
}
else
{
    builder.Services.AddScoped<IAiGenerator, MockAiGenerator>();
}

var app = builder.Build();

// Seed DB
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();