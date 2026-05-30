# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy csproj first for caching
COPY ECP.ProductService/*.csproj ./ECP.ProductService/

RUN dotnet restore ./ECP.ProductService/ECP.ProductService.csproj

# Copy source code
COPY . .

WORKDIR /src/ECP.ProductService

RUN dotnet publish \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ECP.ProductService.dll"]