# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# 1. Copy the project file
COPY ["TransactionAggregationService/Presentation/TransactionAggregation.Api/TransactionAggregation.Api.csproj", "TransactionAggregationService/Presentation/TransactionAggregation.Api/"]

# 2. Restore
RUN dotnet restore "TransactionAggregationService/Presentation/TransactionAggregation.Api/TransactionAggregation.Api.csproj"

# 3. Copy everything
COPY . .

# 4. Build
WORKDIR "/app/TransactionAggregationService/Presentation/TransactionAggregation.Api"
RUN dotnet publish "TransactionAggregation.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "TransactionAggregation.Api.dll"]