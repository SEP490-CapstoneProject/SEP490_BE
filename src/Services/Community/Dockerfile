FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Community/Community.API/Community.API.csproj", "Services/Community/Community.API/"]
COPY ["src/Services/Community/Community.Application/Community.Application.csproj", "Services/Community/Community.Application/"]
COPY ["src/Services/Community/Community.Domain/Community.Domain.csproj", "Services/Community/Community.Domain/"]
COPY ["src/Services/Community/Community.Infrastructure/Community.Infrastructure.csproj", "Services/Community/Community.Infrastructure/"]

RUN dotnet restore "Services/Community/Community.API/Community.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Community/ Services/Community/

WORKDIR "/src/Services/Community/Community.API"
RUN dotnet build "Community.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Community.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Community.API.dll"]
