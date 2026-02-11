FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/Services/Media/Media.API/Media.API/Media.API.csproj", "Services/Media/Media.API/Media.API/"]
RUN dotnet restore "Services/Media/Media.API/Media.API/Media.API.csproj"

# Copy everything else and build
COPY src/Services/Media/ Services/Media/
WORKDIR "/src/Services/Media/Media.API/Media.API"
RUN dotnet build "Media.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Media.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Media.API.dll"]
