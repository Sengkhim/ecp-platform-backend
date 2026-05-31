# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy project files first for layer caching
COPY ECP.OrderService/ECP.OrderService.csproj ECP.OrderService/
COPY Contracts/Contracts.csproj Contracts/

RUN dotnet restore ECP.OrderService/ECP.OrderService.csproj

# Copy source code
COPY . .

WORKDIR /src/ECP.OrderService

RUN dotnet publish \
    ECP.OrderService.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ECP.OrderService.dll"]