FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Connection/Connection.API/Connection.API.csproj", "Services/Connection/Connection.API/"]
COPY ["src/Services/Connection/Connection.Application/Connection.Application.csproj", "Services/Connection/Connection.Application/"]
COPY ["src/Services/Connection/Connection.Domain/Connection.Domain.csproj", "Services/Connection/Connection.Domain/"]
COPY ["src/Services/Connection/Connection.Infrastructure/Connection.Infrastructure.csproj", "Services/Connection/Connection.Infrastructure/"]

RUN dotnet restore "Services/Connection/Connection.API/Connection.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Connection/ Services/Connection/

WORKDIR "/src/Services/Connection/Connection.API"
RUN dotnet build "Connection.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Connection.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Connection.API.dll"]
