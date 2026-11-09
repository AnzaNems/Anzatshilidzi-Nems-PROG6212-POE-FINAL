using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RaceDayApi.Data;
using RaceDayApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------- database (the ONLY DbContext in the whole solution) ----------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost\\SQLEXPRESS;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// ---------- Azure Blob Storage (Part 3) ----------
builder.Services.AddScoped<BlobStorageService>();

// ---------- JWT authentication ----------
// The token carries three claims: NameIdentifier (user id), Email and Role.
// The MVC app stores the token in session and sends it as a Bearer token.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = builder.Configuration["Jwt:Key"]
            ?? "RaceDayDevOnlySigningKey_MustBeAtLeast32Chars!";    // dev fallback

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "raceday-api",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "raceday-client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ---------- Swagger: every endpoint visible, described and testable ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "Event management API for the South African road running, walking and cycling community. " +
                      "Roles: Organiser (manages events, categories, enrolments, results) and Participant " +
                      "(browses events, enrols, views personal results). Endpoints marked '(Additional endpoint)' " +
                      "extend the approved Part 1 plan where the POE required extra functionality."
    });

    // Pull in the XML comments so each endpoint shows its purpose, input and responses.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // The Authorize button: paste your JWT token after logging in, and every
    // protected endpoint becomes testable directly in Swagger UI.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste ONLY your JWT token (no 'Bearer ' prefix) and click Authorize."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddControllers();

// Permissive CORS for development - the MVC app calls the API server-to-server,
// so this only matters if you experiment with browser-side JavaScript calls.
builder.Services.AddCors(options => options.AddPolicy("Dev",
    policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Swagger stays on in every environment so it can be marked from any machine.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.DocumentTitle = "RaceDay API";
});

app.UseCors("Dev");

// Order matters: authentication MUST come before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Migrate + seed on startup so F5 always gives you a ready database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(db);
}

app.Run();

// Required so the integration tests can host the app (WebApplicationFactory<Program>).
// Must come after the top-level statements.
public partial class Program { }
