# Etapa de construcción
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copiar archivos de proyecto
COPY ["VentaFacil.web/VentaFacil.web.csproj", "VentaFacil.web/"]
COPY ["VentaFacil.web.sln", "./"]

# Restaurar dependencias
RUN dotnet restore "VentaFacil.web/VentaFacil.web.csproj"

# Copiar todo y construir
COPY . .
WORKDIR "/src/VentaFacil.web"
RUN dotnet build "VentaFacil.web.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publicar
RUN dotnet publish "VentaFacil.web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Etapa final
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Crear directorios necesarios
RUN mkdir -p /app/keys /wwwroot/uploads /app/logs /app/init && \
    chmod 755 /app/keys /wwwroot/uploads /app/logs /app/init

# Instalar herramientas de diagnóstico
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Configurar usuario no-root
RUN useradd -m -u 1000 -s /bin/sh appuser && \
    chown -R appuser:appuser /app /wwwroot
USER appuser

# Copiar desde la etapa de build
COPY --from=build /app/publish .

# Exponer puertos
EXPOSE 8080
EXPOSE 8081

# Punto de entrada
ENTRYPOINT ["dotnet", "VentaFacil.web.dll"]
