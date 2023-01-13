using stackoverflow.core;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

// Add services to the container.
builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/users", (IDbQueryRepository dbQueryRepo) =>
{
    var result = dbQueryRepo.QueryAsync<string>("SELECT TOP 100 DisplayName FROM dbo.Users");
    return result;
});

app.Run();