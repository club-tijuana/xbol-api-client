# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ENV DOTNET_RUNNING_IN_CONTAINER=true
WORKDIR /app/src

# Copy all project files for restore (optimize layer caching - changes to .cs files won't invalidate restore)
COPY ["Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI.csproj", "Odasoft.XBOL.ClientAPI/"]
COPY ["Odasoft.XBOL.Business/Odasoft.XBOL.Business.csproj", "Odasoft.XBOL.Business/"]
COPY ["Odasoft.XBOL.Data/Odasoft.XBOL.Data.csproj", "Odasoft.XBOL.Data/"]
COPY ["Odasoft.XBOL.Models/Odasoft.XBOL.Models.csproj", "Odasoft.XBOL.Models/"]

# Restore dependencies
RUN dotnet restore "Odasoft.XBOL.ClientAPI/Odasoft.XBOL.ClientAPI.csproj" --verbosity detailed

# Copy entire source tree
COPY . .

# Build the API project
WORKDIR "/app/src/Odasoft.XBOL.ClientAPI"
RUN dotnet build "Odasoft.XBOL.ClientAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Odasoft.XBOL.ClientAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app

# Install curl for health checks (run as root)
USER root
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Switch back to non-root user (respects $APP_UID from VS or defaults to app)
USER $APP_UID

# Copy published output from publish stage
COPY --from=publish /app/publish .

# Health check configuration
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl --fail http://localhost:8080/healthz || exit 1

ENTRYPOINT ["dotnet", "Odasoft.XBOL.ClientAPI.dll"]
