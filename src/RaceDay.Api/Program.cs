using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Infrastructure;
using RaceDay.Api.Infrastructure.Swagger;
using RaceDay.Api.Security;
using RaceDay.Api.Services;
using RaceDay.Api.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers(options =>
        options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider()));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddRaceDaySwagger();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddRaceDayAuthentication();
builder.Services.AddRaceDayServices();
builder.Services.AddRaceDayStorage();

builder.Services.AddDbContext<RaceDayDbContext>((services, options) =>
    options.UseSqlServer(
        services.GetRequiredService<IConfiguration>().GetConnectionString("RaceDay"),
        sql => sql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseRaceDaySwagger();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(app.Configuration["Jwt:SigningKey"]))
{
    app.Logger.LogWarning("Jwt:SigningKey is not set, so a temporary key was generated. Tokens stop working when the API restarts.");
}

await app.InitialiseDatabaseAsync();

app.Run();

public partial class Program { }
