FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Moderation/Moderation.API/Moderation.API.csproj", "Services/Moderation/Moderation.API/"]
COPY ["src/Services/Moderation/Moderation.Application/Moderation.Application.csproj", "Services/Moderation/Moderation.Application/"]
COPY ["src/Services/Moderation/Moderation.Domain/Moderation.Domain.csproj", "Services/Moderation/Moderation.Domain/"]
COPY ["src/Services/Moderation/Moderation.Infrastructure/Moderation.Infrastructure.csproj", "Services/Moderation/Moderation.Infrastructure/"]

RUN dotnet restore "Services/Moderation/Moderation.API/Moderation.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Moderation/ Services/Moderation/

WORKDIR "/src/Services/Moderation/Moderation.API"
RUN dotnet build "Moderation.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Moderation.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Moderation.API.dll"]
