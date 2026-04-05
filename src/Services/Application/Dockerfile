FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Application/Application.API/Application.API.csproj", "Services/Application/Application.API/"]
COPY ["src/Services/Application/Application.Application/Application.Application.csproj", "Services/Application/Application.Application/"]
COPY ["src/Services/Application/Application.Domain/Application.Domain.csproj", "Services/Application/Application.Domain/"]
COPY ["src/Services/Application/Application.Infrastructure/Application.Infrastructure.csproj", "Services/Application/Application.Infrastructure/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]

RUN dotnet restore "Services/Application/Application.API/Application.API.csproj"

COPY src/Services/Application/ Services/Application/
COPY src/Shared/RecruitmentPlatform.Contracts/ Shared/RecruitmentPlatform.Contracts/

WORKDIR "/src/Services/Application/Application.API"
RUN dotnet build "Application.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Application.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Application.API.dll"]
