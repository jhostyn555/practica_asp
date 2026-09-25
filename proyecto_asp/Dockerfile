FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Instalar las librerias nativas de Linux que exige SkiaSharp (como libfontconfig)
USER root
RUN apt-get update && apt-get install -y --no-install-recommends \
    libfontconfig1 \
    libfreetype6 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["proyecto_asp.csproj", "./"]
RUN dotnet restore "proyecto_asp.csproj"
COPY . .
RUN dotnet build "proyecto_asp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "proyecto_asp.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "proyecto_asp.dll"]