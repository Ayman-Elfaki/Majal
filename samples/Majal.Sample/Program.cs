using System.Globalization;
using Majal;
using Majal.Sample.Common.Persistence;
using Majal.Sample.Common.Services;
using Majal.Sample.Modules.Issues.Endpoints;
using Majal.Sample.Modules.Projects.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<ILocaleProvider<CultureInfo>, HttpLocaleProvider>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<AppDbContext>(option =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    option.UseSqlite(connectionString);
});

builder.Services.AddValidation();

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapCreateIssueEndpoint();
app.MapResolveIssueEndpoint();
app.MapListProjectsEndpoint();
app.MapCreateProjectEndpoint();


app.Run();