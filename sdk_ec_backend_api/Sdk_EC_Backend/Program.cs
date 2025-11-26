using Sdk_EC_Backend.Configuration;
using Sdk_EC_Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure URLs based on USE_HTTPS environment variable
var useHttps = Environment.GetEnvironmentVariable("USE_HTTPS")?.ToLower() == "true";
if (useHttps)
{
    builder.WebHost.UseUrls("https://+:7129", "http://+:5139");
}
else
{
    builder.WebHost.UseUrls("http://+:5139");
}

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://localhost:4200", "https://localhost:4443")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Configure Supabase
builder.Services.Configure<SupabaseSettings>(
    builder.Configuration.GetSection(SupabaseSettings.SectionName));
// If the Key is not present in configuration, allow providing it via environment variable SUPABASE_KEY
builder.Services.PostConfigure<SupabaseSettings>(opts =>
{
    if (string.IsNullOrWhiteSpace(opts.Key))
    {
        var envKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            opts.Key = envKey;
        }
    }
    if (string.IsNullOrWhiteSpace(opts.JwtSecret))
    {
        var envJwtSecret = Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");
        if (!string.IsNullOrWhiteSpace(envJwtSecret))
        {
            opts.JwtSecret = envJwtSecret;
        }
    }
});

builder.Services.AddScoped<SupabaseService>();

// Configure JWT Authentication for Supabase tokens
var supabaseSettings = builder.Configuration.GetSection(SupabaseSettings.SectionName).Get<SupabaseSettings>();
var jwtSecret = supabaseSettings?.JwtSecret ?? Environment.GetEnvironmentVariable("SUPABASE_JWT_SECRET");

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    var key = Encoding.UTF8.GetBytes(jwtSecret);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // Set to true in production with HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false, // Supabase tokens don't have standard issuer
            ValidateAudience = false, // Supabase tokens don't have standard audience
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });
    builder.Services.AddAuthorization();
}

// builder.Services.AddScoped<ISupabaseService, SupabaseService>();
// SupabaseService removed; register raw Supabase client instead.
// builder.Services.AddSingleton<Supabase.Client>(sp => {
//     var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SupabaseSettings>>().Value;
//     var options = new Supabase.SupabaseOptions { AutoRefreshToken = false, AutoConnectRealtime = false };
//     return new Supabase.Client(cfg.Url, cfg.Key, options);
// });

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add controllers (migrate minimal endpoints to controllers)
builder.Services.AddControllers();

var app = builder.Build();

// Enable CORS - add this before other middleware
app.UseRouting();  
app.UseCors("AllowAngularDev");

// Add Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger JSON and the Swagger UI in development
    app.UseSwagger();
    app.UseSwaggerUI();

    // Keep existing OpenAPI mapping (if used by other tooling)
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map controllers (ProductController handles /api/products)
app.MapControllers();

app.Run();
