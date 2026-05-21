using System.Globalization;
using System.Text.Json.Serialization;
using FluentValidation;
using JasperFx;
using Majal;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Common.Services;
using MicroElements.AspNetCore.OpenApi.FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILocaleProvider<CultureInfo>, HttpLocaleProvider>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(option =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    option.UseSqlite(connectionString);
});

builder.Host.UseWolverine(options =>
{
    options.Policies.AutoApplyTransactions();
    options.Policies.UseDurableLocalQueues();
    options.Discovery.IncludeAssembly(typeof(AppDbContext).Assembly);
});

builder.Services.AddWolverineHttp();

builder.Services.AddFluentValidationRulesToOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<AppDbContext>();

builder.Services.AddValidation();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapWolverineEndpoints(opt =>
{
    opt.UseFluentValidationProblemDetailMiddleware();
});

return await app.RunJasperFxCommands(args);