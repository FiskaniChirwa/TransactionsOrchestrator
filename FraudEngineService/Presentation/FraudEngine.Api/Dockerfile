# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. Copy the project file
COPY ["FraudEngineService/Presentation/FraudEngine.Api/FraudEngine.Api.csproj", "FraudEngineService/Presentation/FraudEngine.Api/"]

# 2. Restore
RUN dotnet restore "FraudEngineService/Presentation/FraudEngine.Api/FraudEngine.Api.csproj"

# 3. Copy everything
COPY . .

# 4. Build
WORKDIR "/app/FraudEngineService/Presentation/FraudEngine.Api"
RUN dotnet publish "FraudEngine.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "FraudEngine.Api.dll"]