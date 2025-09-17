# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Instalar herramientas de red, diagnóstico y mssql-tools
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
        iputils-ping \
        dnsutils \
        net-tools \
        curl \
        telnet \
        netcat-openbsd \
        gnupg && \
    # Instalar mssql-tools para pruebas de conexión SQL
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/debian/11/prod bullseye main" > /etc/apt/sources.list.d/microsoft.list && \
    apt-get update && \
    ACCEPT_EULA=Y apt-get install -y mssql-tools unixodbc-dev && \
    # Limpiar cache
    rm -rf /var/lib/apt/lists/*

# Agregar mssql-tools al PATH
ENV PATH="$PATH:/opt/mssql-tools/bin"

WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["VentaFacil.web/VentaFacil.web.csproj", "VentaFacil.web/"]
RUN dotnet restore "./VentaFacil.web/VentaFacil.web.csproj"
COPY . .
WORKDIR "/src/VentaFacil.web"
RUN dotnet build "./VentaFacil.web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./VentaFacil.web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "VentaFacil.web.dll"]