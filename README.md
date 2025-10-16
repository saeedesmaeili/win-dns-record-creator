# Windows DNS A Record Creator Service

This project contains a Windows Service built with ASP.NET Core. The service hosts a minimal web API that exposes a single endpoint for creating DNS A records within the `mafpars.local` zone on a Windows Server that is running the DNS role.

## Features

- Runs as a native Windows Service by using the `UseWindowsService` hosting extension.
- Hosts an HTTP API with Kestrel listening on port `8089`.
- Provides a `POST /dns/a-record` endpoint that accepts a JSON payload containing a `subdomain` value.
- Automatically creates an A record for `<subdomain>.mafpars.local` that points to the local server's first IPv4 address via WMI (`MicrosoftDNS_AType`).
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
   New-Service -Name "WinDnsRecordCreator" -BinaryPathName "C:\path\to\publish\WinDnsRecordCreator.exe" -DisplayName "Windows DNS A Record Creator" -Description "Creates DNS A records for mafpars.local"
   Start-Service -Name "WinDnsRecordCreator"
   ```
3. Configure firewall rules if necessary to allow inbound traffic on port `8089`.

> **Note:** The Windows Server must have the DNS role installed and the executing account must have permission to create DNS records in the `mafpars.local` zone.
