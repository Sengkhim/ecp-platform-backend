FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# ✅ Paths now relative to parent folder (build context = ..)
COPY ["ECP.OrderService/ECP.OrderService.csproj", "ECP.OrderService/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]

RUN dotnet restore "ECP.OrderService/ECP.OrderService.csproj"

COPY ECP.OrderService/ ECP.OrderService/
COPY Contracts/ Contracts/

WORKDIR "/src/ECP.OrderService"
RUN dotnet build "./ECP.OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ECP.OrderService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.OrderService.dll"]