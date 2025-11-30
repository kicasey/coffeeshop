#so my website can be up all the time :DDD

# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy csproj and restore
COPY ["CoffeeShopSimulation.csproj", "./"]
RUN dotnet restore "CoffeeShopSimulation.csproj"

# copy everything and publish
COPY . .
RUN dotnet publish "CoffeeShopSimulation.csproj" -c Release -o /app/publish

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# copy published app from build stage
COPY --from=build /app/publish .

# Create directory for database (if needed)
RUN mkdir -p /app/data

# Render expects the app to listen on port 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

# Make sure the app directory is writable for SQLite
RUN chmod -R 755 /app

EXPOSE 10000

ENTRYPOINT ["dotnet", "CoffeeShopSimulation.dll"]
