# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["WorldSimulator/WorldSimulator.csproj", "WorldSimulator/"]

# Restore dependencies
RUN dotnet restore "WorldSimulator/WorldSimulator.csproj"

# Copy source code
COPY . .
WORKDIR "/src/WorldSimulator"

# Build and publish in Release mode
RUN dotnet publish "WorldSimulator.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_ENVIRONMENT=Production

# Expose port (if needed for health checks)
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "WorldSimulator.dll"]

