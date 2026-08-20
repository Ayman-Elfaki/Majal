using System.Globalization;
using System.Text.Json.Serialization;
using EShop.Persistence;
using EShop.Services;
using FluentValidation;
using JasperFx;
using MicroElements.AspNetCore.OpenApi.FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILocaleProvider<CultureInfo>, HttpLocaleProvider>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<EShopDbContext>(option =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

    // SQLite opens the database file but won't create a missing containing directory.
    var dataSource = new SqliteConnectionStringBuilder(connectionString).DataSource;
    var directory = Path.GetDirectoryName(dataSource);
    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

    option.UseSqlite(connectionString);
});

builder.Host.UseWolverine(options =>
{
    options.Policies.AutoApplyTransactions();
    options.Policies.UseDurableLocalQueues();
    options.Discovery.IncludeAssembly(typeof(EShopDbContext).Assembly);
});

builder.Services.AddWolverineHttp();

builder.Services.AddFluentValidationRulesToOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<EShopDbContext>();

builder.Services.AddValidation();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<EShopDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapWolverineEndpoints(opt => { opt.UseFluentValidationProblemDetailMiddleware(); });

return await app.RunJasperFxCommands(args);
