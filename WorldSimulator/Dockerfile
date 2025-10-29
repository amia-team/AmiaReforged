# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project file first to leverage layer caching for restore
COPY WorldSimulator/WorldSimulator.csproj WorldSimulator/
RUN dotnet restore WorldSimulator/WorldSimulator.csproj

# Copy the rest and publish
COPY . .
RUN dotnet publish WorldSimulator/WorldSimulator.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Recommended for ASP.NET health endpoints and container behavior
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copy published output
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "WorldSimulator.dll"]
