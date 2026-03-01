FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ECP.ApiGateway/ECP.ApiGateway.csproj", "ECP.ApiGateway/"]
RUN dotnet restore "ECP.ApiGateway/ECP.ApiGateway.csproj"
COPY . .
WORKDIR "/src/ECP.ApiGateway"
RUN dotnet build "./ECP.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ECP.ApiGateway.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECP.ApiGateway.dll"]