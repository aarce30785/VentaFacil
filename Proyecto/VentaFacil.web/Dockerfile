# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        iputils-ping dnsutils net-tools curl telnet netcat-openbsd gnupg && \
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/debian/11/prod bullseye main" \
        > /etc/apt/sources.list.d/microsoft.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev && \
    rm -rf /var/lib/apt/lists/*

ENV PATH="$PATH:/opt/mssql-tools/bin"

WORKDIR /app

# Solo expone el puerto HTTP que Hostinger usa
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Docker

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ./VentaFacil.web ./VentaFacil.web

WORKDIR /src/VentaFacil.web
RUN dotnet restore
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

COPY ./init /app/init

ENTRYPOINT ["dotnet", "VentaFacil.web.dll"]
