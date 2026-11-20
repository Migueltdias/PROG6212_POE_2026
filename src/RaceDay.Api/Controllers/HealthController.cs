using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>System status.</summary>
[Tags("System")]
[Route("api/health")]
public class HealthController(RaceDayDbContext db) : ApiControllerBase
{
    private static readonly string Version =
        typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0]
        ?? "unknown";

    /// <summary>Reports API and database status.</summary>
    /// <remarks>Opens a connection to SQL Server. Use it to check the API is up and can reach its database.</remarks>
    /// <response code="200">The API is running and the database answered.</response>
    /// <response code="503">The API is running but the database cannot be reached.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        bool connected;
        try
        {
            connected = await db.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            connected = false;
        }

        var body = new HealthResponse
        {
            Status = connected ? "Healthy" : "Unhealthy",
            Database = connected ? "Connected" : "Unreachable",
            Version = Version
        };

        return connected ? Ok(body) : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
