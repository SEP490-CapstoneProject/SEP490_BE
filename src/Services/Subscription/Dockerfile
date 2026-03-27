FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Shared/RecruitmentPlatform.Common/RecruitmentPlatform.Common.csproj", "Shared/RecruitmentPlatform.Common/"]
COPY ["src/Shared/RecruitmentPlatform.Contracts/RecruitmentPlatform.Contracts.csproj", "Shared/RecruitmentPlatform.Contracts/"]
COPY ["src/Services/Subscription/Subscription.API/Subscription.API.csproj", "Services/Subscription/Subscription.API/"]
COPY ["src/Services/Subscription/Subscription.Application/Subscription.Application.csproj", "Services/Subscription/Subscription.Application/"]
COPY ["src/Services/Subscription/Subscription.Domain/Subscription.Domain.csproj", "Services/Subscription/Subscription.Domain/"]
COPY ["src/Services/Subscription/Subscription.Infrastructure/Subscription.Infrastructure.csproj", "Services/Subscription/Subscription.Infrastructure/"]

RUN dotnet restore "Services/Subscription/Subscription.API/Subscription.API.csproj"

COPY src/Shared/ Shared/
COPY src/Services/Subscription/ Services/Subscription/

WORKDIR "/src/Services/Subscription/Subscription.API"
RUN dotnet build "Subscription.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Subscription.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Subscription.API.dll"]
