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

    public Task<string> CreateARecordAsync(string subdomain, string ipAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(subdomain))
            throw new ArgumentException("Subdomain cannot be null or whitespace.", nameof(subdomain));
        cancellationToken.ThrowIfCancellationRequested();
        string zoneName = "mafpars.local";
        string fqdn = subdomain + "." + zoneName;
        var sanitizedSubdomain = SanitizeSubdomain(subdomain);
        string dnsServer = Environment.MachineName;
        ManagementScope scope = new ManagementScope($@"\\{dnsServer}\root\MicrosoftDNS");
        scope.Connect();

        string query = $"SELECT * FROM MicrosoftDNS_AType WHERE OwnerName = '{fqdn}'";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query));
        ManagementObjectCollection results = searcher.Get();

        if (results.Count == 0)
        {
            ManagementClass dnsAClass = new ManagementClass(scope, new ManagementPath("MicrosoftDNS_AType"), null);
            ManagementBaseObject inParams = dnsAClass.GetMethodParameters("CreateInstanceFromPropertyData");
            inParams["DnsServerName"] = dnsServer;                         // DNS server name (FQDN or hostname):contentReference[oaicite:6]{index=6}
            inParams["ContainerName"] = zoneName;                          // DNS zone name (container):contentReference[oaicite:7]{index=7}
            inParams["OwnerName"] = $"{subdomain}.{zoneName}";           // FQDN of the new record (host.zone):contentReference[oaicite:8]{index=8}
            inParams["IPAddress"] = ipAddress;                          // IPv4 address for the host record
            ManagementBaseObject outParams = dnsAClass.InvokeMethod(
                "CreateInstanceFromPropertyData", inParams, null);
        }
        else if (results.Count > 1)
        {
            throw new Exception($"Multiple A records for {fqdn} found; update not performed.");
        }
        else
        {
            foreach (ManagementObject record in results)  // there will be exactly one
            {
                string currentIp = record["RecordData"]?.ToString();
                if (currentIp == null)
                    throw new Exception("Failed to retrieve current IP from DNS record.");
                if (!currentIp.Equals(ipAddress, StringComparison.OrdinalIgnoreCase))
                {
                    ManagementBaseObject inParams = record.GetMethodParameters("Modify");
                    inParams["IPAddress"] = ipAddress;
                    record.InvokeMethod("Modify", inParams, null);
                }
                break; 
            }
        }
        return Task.FromResult($"{subdomain}.{zoneName}");
    }

    private static string SanitizeSubdomain(string value)
    {
        var trimmed = value.Trim().TrimEnd('.');
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new ArgumentException("Subdomain cannot be empty after trimming whitespace and trailing dots.", nameof(value));
        if (trimmed.Any(char.IsWhiteSpace))
            throw new ArgumentException("Subdomain cannot contain whitespace characters.", nameof(value));
        return trimmed.ToLowerInvariant();
    }
}
