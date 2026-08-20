# Multi-stage build for Approva.Api. Build context must be the `api/` directory:
#   docker build -f Dockerfile -t approva-api .
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln .
COPY Approva.Domain/*.csproj Approva.Domain/
COPY Approva.Application/*.csproj Approva.Application/
COPY Approva.Infrastructure/*.csproj Approva.Infrastructure/
COPY Approva.Api/*.csproj Approva.Api/
COPY Approva.Tests/*.csproj Approva.Tests/
RUN dotnet restore Approva.Api/Approva.Api.csproj

COPY . .
RUN dotnet publish Approva.Api/Approva.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

ENTRYPOINT ["dotnet", "Approva.Api.dll"]
