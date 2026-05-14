# XBOL Client API

API for the XBOL client applications

## Development Setup

This refers to development using Visual Studio 2026 on Windows, or using the .NET 10 SDK through the command line.

### Requirements

- [Visual Studio 2026](https://visualstudio.microsoft.com/insiders/) (Windows)
- [.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (Linux)
- PostgreSQL

### Quick Start

In Visual Studio, press `F5` or the play button. For the command-line interface:

```bash
dotnet watch --project Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI
```

### Build & Compilation

To build the entire solution:

```bash
dotnet build Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI.slnx
```

### Configuration

Edit `appsettings.Development.json` for local settings (connection strings, service URLs, etc.). Settings cascade: `appsettings.json` → `appsettings.{Environment}.json` → .NET user secrets in Development → environment variables. All settings are validated at startup via `AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()`, so missing or invalid values fail the boot with an `OptionsValidationException` rather than a lazy runtime failure.

IDE autocomplete is provided by `appsettings.schema.json`, which regenerates automatically on Debug builds.

#### Firebase Service Account (local development)

The API uses Firebase Admin SDK (GCIP) for client authentication. A Google Cloud service account key is required. **Do not commit the key to the repo** — use .NET user secrets instead.

1. Download the service account JSON key from the GCP Console (`IAM & Admin → Service Accounts → Keys`).
2. Set the key as a user secret:

```bash
dotnet user-secrets set "GcipAuth:ServiceAccountJson" "$(cat path/to/service-account-key.json)" \
  --project Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI
```

The app reads `GcipAuth:ServiceAccountJson` at startup and creates a `GoogleCredential` from it. The `TenantId` and `ProjectId` are already set in `appsettings.json` and do not need to be overridden.

To verify the secret is stored:

```bash
dotnet user-secrets list --project Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI
```

## Deployment

The container is production-ready with:

- **Security**: Non-root `app` user
- **Optimization**: Release build with ReadyToRun compilation
- **Health checks**: Automatic container health monitoring
- **Restart policy**: `unless-stopped` for high availability
- **Environment**: `ASPNETCORE_ENVIRONMENT=Production`

#### Requirements

- Make
- [Podman](https://podman.io/) (or [Docker](https://www.docker.com/))
- [Podman Compose](https://docs.podman.io/en/latest/markdown/podman-compose.1.html) (or [Docker Compose](https://docs.docker.com/compose/))

#### Usage

```bash
make build    # Create the Docker container
make run      # Run the Docker Compose environment
```

**Access the containerized services**

- **API Base URL**: <http://localhost:8080>
- **API Health Check**: <http://localhost:8080/healthz>

#### GCP Secrets

Runtime configuration is stored in GCP Secret Manager. Each environment has a dedicated secret:

| Secret                       | Contents                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------ |
| `dev-xbol-db-secret`         | PostgreSQL credentials (`DB_HOST`, `DB_PORT`, `DB_USER`, `DB_PASS`, `DB_NAME`) |
| `dev-xbol-api-client-secret` | App configuration (connection string, CORS origins, allowed users, GCIP key)   |

The app secret stores environment variables using ASP.NET's `__` (double underscore) convention for nested config and array indices:

```json
{
  "ConnectionStrings__Database": "Host=<DB_HOST>;Port=<DB_PORT>;Database=<DB_NAME>;Username=<DB_USER>;Password=<DB_PASS>",
  "Cors__AcceptedOrigins__0": "http://localhost:3000",
  "Cors__AcceptedOrigins__1": "https://dev-web.pwrticket.mx",
  "Authentication__AllowedUsers__0__Email": "client@xbol.com",
  "Authentication__AllowedUsers__0__Password": "<password>",
  "GcipAuth__ServiceAccountJson": "<service-account-json>",
  "TicketingClient__BaseAddress": "https://dev-api.ticketing.pwrticket.mx"
}
```

Connection strings are assembled from the values in `dev-xbol-db-secret`. To update:

```bash
gcloud secrets versions add dev-xbol-api-client-secret --data-file=- <<'EOF'
{ ... }
EOF
```

QA and Prod secrets follow the same pattern with `qa-` and `prod-` prefixes.
