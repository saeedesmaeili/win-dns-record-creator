using System.Management;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace WinDnsRecordCreator.Services;

public class DnsRecordService : IDnsRecordService
{
    private const string ZoneName = "mafpars.local";
    private const string WmiNamespace = @"root\\MicrosoftDNS";
    private const string RecordClassName = "MicrosoftDNS_AType";

    private readonly ILogger<DnsRecordService> _logger;

    public DnsRecordService(ILogger<DnsRecordService> logger)
    {
        _logger = logger;
    }

    public Task<string> CreateARecordAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
        {
            throw new ArgumentException("Subdomain cannot be null or whitespace.", nameof(subdomain));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var sanitizedSubdomain = SanitizeSubdomain(subdomain);
        var ownerName = $"{sanitizedSubdomain}.{ZoneName}";
        var dnsServerName = Environment.MachineName;
        var ipv4Address = ResolveLocalIPv4Address();

        var managementScope = new ManagementScope($"\\\\{dnsServerName}\\{WmiNamespace}");

        try
        {
            managementScope.Connect();
        }
        catch (ManagementException ex)
        {
            _logger.LogError(ex, "Failed to connect to the DNS WMI provider on server {Server}", dnsServerName);
            throw new InvalidOperationException($"Failed to connect to the DNS server '{dnsServerName}'.", ex);
        }

        using var recordClass = new ManagementClass(managementScope, new ManagementPath(RecordClassName), null);
        using var methodParameters = recordClass.GetMethodParameters("CreateInstanceFromPropertyData");

        methodParameters["DnsServerName"] = dnsServerName;
        methodParameters["ContainerName"] = ZoneName;
        methodParameters["OwnerName"] = ownerName;
        methodParameters["IPAddress"] = ipv4Address.ToString();
        methodParameters["TTL"] = 3600u;

        _logger.LogInformation("Creating DNS A record {OwnerName} -> {IPAddress} on server {Server}", ownerName, ipv4Address, dnsServerName);

        try
        {
            recordClass.InvokeMethod("CreateInstanceFromPropertyData", methodParameters, null);
            _logger.LogInformation("Successfully created DNS A record {OwnerName}", ownerName);
        }
        catch (ManagementException ex)
        {
            _logger.LogError(ex, "Failed to create DNS A record {OwnerName}", ownerName);
            throw new InvalidOperationException($"Failed to create DNS A record '{ownerName}'.", ex);
        }

        return Task.FromResult(ownerName);
    }

    private static string SanitizeSubdomain(string value)
    {
        var trimmed = value.Trim().TrimEnd('.');

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Subdomain cannot be empty after trimming whitespace and trailing dots.", nameof(value));
        }

        if (trimmed.Any(char.IsWhiteSpace))
        {
            throw new ArgumentException("Subdomain cannot contain whitespace characters.", nameof(value));
        }

        return trimmed.ToLowerInvariant();
    }

    private static IPAddress ResolveLocalIPv4Address()
    {
        var addresses = Dns.GetHostEntry(Dns.GetHostName())
            .AddressList
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork)
            .ToArray();

        if (addresses.Length == 0)
        {
            throw new InvalidOperationException("No IPv4 address was found for the current host.");
        }

        return addresses[0];
    }
}
