FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PassDo.sln ./
COPY src/PassDo.Api/PassDo.Api.csproj src/PassDo.Api/
COPY src/PassDo.Application/PassDo.Application.csproj src/PassDo.Application/
COPY src/PassDo.Domain/PassDo.Domain.csproj src/PassDo.Domain/
COPY src/PassDo.Infrastructure/PassDo.Infrastructure.csproj src/PassDo.Infrastructure/
COPY tests/PassDo.UnitTests/PassDo.UnitTests.csproj tests/PassDo.UnitTests/
COPY tests/PassDo.IntegrationTests/PassDo.IntegrationTests.csproj tests/PassDo.IntegrationTests/

RUN dotnet restore PassDo.sln

COPY . .
RUN dotnet publish src/PassDo.Api/PassDo.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=10s --timeout=5s --start-period=40s --retries=10 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "PassDo.Api.dll"]
