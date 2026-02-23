using MovieSite.Domain.Repositories;
using MovieSite.Infrastructure;
using MovieSite.Infrastructure.Persistence;
using SqlSugar;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
