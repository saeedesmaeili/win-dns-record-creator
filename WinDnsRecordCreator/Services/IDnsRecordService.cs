using System.Threading;
using System.Threading.Tasks;

namespace WinDnsRecordCreator.Services;

public interface IDnsRecordService
{
    Task<string> CreateARecordAsync(string subdomain, string ipAddress, CancellationToken cancellationToken = default);
}
