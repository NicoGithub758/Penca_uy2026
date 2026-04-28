# Consulte https://aka.ms/customizecontainer para aprender a personalizar su contenedor de depuración y cómo Visual Studio usa este Dockerfile para compilar sus imágenes para una depuración más rápida.

# Esta fase se usa cuando se ejecuta desde VS en modo rápido (valor predeterminado para la configuración de depuración)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# Esta fase se usa para compilar el proyecto de servicio
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Se incluyen todos los proyectos de la solución.
COPY ["Penca_uy2026/Penca_uy2026.csproj", "Penca_uy2026/"]
COPY ["Penca_uy2026.Models/Penca_uy2026.Models.csproj", "Penca_uy2026.Models/"]
COPY ["Penca_uy2026.Data/Penca_uy2026.Data.csproj", "Penca_uy2026.Data/"]
COPY ["Penca_uy2026.Business/Penca_uy2026.Business.csproj", "Penca_uy2026.Business/"]

RUN dotnet restore "./Penca_uy2026/Penca_uy2026.csproj"
COPY . .
WORKDIR "/src/Penca_uy2026"
RUN dotnet build "./Penca_uy2026.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Esta fase se usa para publicar el proyecto de servicio que se copiará en la fase final.
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Penca_uy2026.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Esta fase se usa en producción o cuando se ejecuta desde VS en modo normal (valor predeterminado cuando no se usa la configuración de depuración)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Penca_uy2026.dll"]