# Windows DNS A Record Creator Service

This project contains a Windows Service built with ASP.NET Core. The service hosts a minimal web API that exposes a single endpoint for creating DNS A records within the `myco.local` zone on a Windows Server that is running the DNS role.

## Features

- Runs as a native Windows Service by using the `UseWindowsService` hosting extension.
- Hosts an HTTP API with Kestrel listening on port `8089`.
- Provides a `POST /dns/a-record` endpoint that accepts a JSON payload containing a `subdomain` value.
- Automatically creates an A record for `<subdomain>.myco.local` that points to the local server's first IPv4 address via WMI (`MicrosoftDNS_AType`).
- Includes a `GET /health` endpoint for simple health monitoring.

## API Usage

### Create an A Record

```http
POST http://<server>:8089/dns/a-record
Content-Type: application/json

{ "subdomain": "app" }
```

Successful requests return HTTP 200 with a confirmation message. Validation errors return HTTP 400.

### Health Check

```http
GET http://<server>:8089/health
```

Returns HTTP 200 with a small JSON payload when the service is running.

## Building and Installing the Service

1. Restore and publish the project on a Windows machine with the .NET SDK installed:
   ```powershell
   dotnet publish .\WinDnsRecordCreator\WinDnsRecordCreator.csproj -c Release -r win-x64 --self-contained false
   ```
2. Install the service (run from an elevated PowerShell session):
   ```powershell
   New-Service -Name "WinDnsRecordCreator" -BinaryPathName "C:\path\to\publish\WinDnsRecordCreator.exe" -DisplayName "Windows DNS A Record Creator" -Description "Creates DNS A records for myco.local"
   Start-Service -Name "WinDnsRecordCreator"
   ```
3. Configure firewall rules if necessary to allow inbound traffic on port `8089`.

> **Note:** The Windows Server must have the DNS role installed and the executing account must have permission to create DNS records in the `myco.local` zone.

## Testing the Service

Once the service is installed and running on the target Windows Server you can verify end-to-end behavior with the following steps:

1. **Confirm the service is listening**
   ```powershell
   netstat -ano | findstr :8089
   ```
   The output should show a `LISTENING` entry bound to port `8089` that maps to the service's process ID.

2. **Send a test request**
   Use any HTTP client (PowerShell, curl, Postman) to invoke the API. The example below uses PowerShell:
   ```powershell
   Invoke-RestMethod -Uri "http://YourDnsServer:8089/dns/a-record" -Method Post -Body '{"subdomain":"apitest","IpAddress":"10.237.203.12"}' -ContentType "application/json"
   ```
   A successful call returns a `200 OK` response with a confirmation message.

3. **Verify the DNS record exists**
   Query the DNS zone from the same server (or any machine that can reach the DNS server):
   ```powershell
   Resolve-DnsName -Name "apitest.myco.local" -Server localhost
   ```
   The result should display an `A` record that points to the server's IPv4 address.

4. **Clean up (optional)**
   If you created the record for testing purposes only, remove it through the DNS Manager MMC snap-in or PowerShell:
   ```powershell
   Remove-DnsServerResourceRecord -ZoneName "myco.local" -RRType "A" -Name "apitest" -Force
   ```

If the call returns an error, check the Windows Event Log (`Application` log, source `WinDnsRecordCreator`) for detailed error information emitted by the service's structured logging.
