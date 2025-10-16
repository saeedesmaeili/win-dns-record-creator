namespace WinDnsRecordCreator.Models;

public sealed class ARecordRequest
{
    public string Subdomain { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
}
