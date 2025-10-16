using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.Threading;
using WinDnsRecordCreator.Models;
using WinDnsRecordCreator.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Any, 8089);
});

builder.Services.AddSingleton<IDnsRecordService, DnsRecordService>();

var app = builder.Build();

app.MapPost("/dns/a-record", async (ARecordRequest? request, IDnsRecordService service, CancellationToken cancellationToken) =>
{
    if (request is null || string.IsNullOrWhiteSpace(request.Subdomain))
    {
        return Results.BadRequest(new { error = "The 'subdomain' value is required." });
    }

    var fqdn = await service.CreateARecordAsync(request.Subdomain, cancellationToken);

    return Results.Ok(new { message = $"DNS A record created for {fqdn}." });
})
.WithName("CreateARecord")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.Run();
