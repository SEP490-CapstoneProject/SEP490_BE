FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Advertisement/Advertisement.API/Advertisement.API.csproj", "Services/Advertisement/Advertisement.API/"]
COPY ["src/Services/Advertisement/Advertisement.Application/Advertisement.Application.csproj", "Services/Advertisement/Advertisement.Application/"]
COPY ["src/Services/Advertisement/Advertisement.Domain/Advertisement.Domain.csproj", "Services/Advertisement/Advertisement.Domain/"]
COPY ["src/Services/Advertisement/Advertisement.Infrastructure/Advertisement.Infrastructure.csproj", "Services/Advertisement/Advertisement.Infrastructure/"]

RUN dotnet restore "Services/Advertisement/Advertisement.API/Advertisement.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Advertisement/ Services/Advertisement/

WORKDIR "/src/Services/Advertisement/Advertisement.API"
RUN dotnet build "Advertisement.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Advertisement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Advertisement.API.dll"]
