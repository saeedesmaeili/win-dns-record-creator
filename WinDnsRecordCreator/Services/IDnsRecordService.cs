using System.Threading;
using System.Threading.Tasks;

namespace WinDnsRecordCreator.Services;

public interface IDnsRecordService
{
    Task<string> CreateARecordAsync(string subdomain, CancellationToken cancellationToken = default);
}
