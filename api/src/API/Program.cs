using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MovieSite.API.Middleware;
using MovieSite.Application.Common;
using MovieSite.Application.Franchises;
using MovieSite.Application.Home;
using MovieSite.Application.Movies;
using MovieSite.Domain;
using MovieSite.Domain.Repositories;
using MovieSite.Infrastructure;
using MovieSite.Infrastructure.Cache;
using MovieSite.Infrastructure.Persistence;
using MovieSite.Infrastructure.Storage;
using Prometheus;
using Sentry.AspNetCore;
using SqlSugar;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
    options.Dsn = builder.Configuration["Sentry:Dsn"];
    options.TracesSampleRate = 0.1;
    options.SendDefaultPii = false;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MovieSite API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input JWT bearer token in the format: Bearer {token}"
    });

});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configuredOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        var origins = configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : ["http://localhost:5173", "http://localhost:5174"];

        policy.WithOrigins(origins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MetadataAddress = builder.Configuration["Auth:JwksUri"] ?? string.Empty;
        options.RequireHttpsMetadata = builder.Configuration.GetValue("Auth:RequireHttpsMetadata", false);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidAudience = builder.Configuration["Auth:Audience"],
            ValidIssuer = builder.Configuration["Auth:Issuer"],
            ValidateIssuer = !string.IsNullOrWhiteSpace(builder.Configuration["Auth:Issuer"]),
            ValidateAudience = !string.IsNullOrWhiteSpace(builder.Configuration["Auth:Audience"]),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:Connection"]!));

builder.Services.AddScoped<ISqlSugarClient>(_ =>
    new SqlSugarClient(new ConnectionConfig
    {
        DbType = DbType.PostgreSQL,
        ConnectionString = builder.Configuration.GetConnectionString("Default")!,
        IsAutoCloseConnection = true,
        InitKeyType = InitKeyType.Attribute,
        MoreSettings = new ConnMoreSettings
        {
            PgSqlIsAutoToLower = false,
            IsAutoRemoveDataCache = true
        }
    }));

builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<ITvSeriesRepository, TvSeriesRepository>();
builder.Services.AddScoped<IAnimeRepository, AnimeRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRedisCache, RedisCache>();
builder.Services.AddScoped<HomeApplicationService>();
builder.Services.AddScoped<SimilarContentService>();
builder.Services.AddScoped<MovieApplicationService>();
builder.Services.AddScoped<FranchiseApplicationService>();
builder.Services.AddScoped<CacheInvalidationService>();
builder.Services.AddSingleton<ITencentCosClient>(_ =>
    new TencentCosClient(builder.Configuration["COS:CdnBase"] ?? string.Empty));

var app = builder.Build();

CosUrlHelper.Configure(builder.Configuration["COS:CdnBase"] ?? string.Empty);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseCors();
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/v1/health", () => Results.Ok(new { status = "ok" }));

if (builder.Configuration.GetValue("Metrics:Enabled", true))
{
    app.MapMetrics("/metrics");
}

app.MapControllers();

app.Run();
