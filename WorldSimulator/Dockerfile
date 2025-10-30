# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build metadata (can be overridden at build time)
ARG GIT_SHA=unknown
ARG BUILD_TIME=unknown
ARG BUILD_VERSION=dev

# Copy project file
COPY ["WorldSimulator/WorldSimulator.csproj", "WorldSimulator/"]

# Restore dependencies
RUN dotnet restore "WorldSimulator/WorldSimulator.csproj"

# Copy source code
COPY . .
WORKDIR "/src/WorldSimulator"

# Build and publish in Release mode with metadata
RUN dotnet publish "WorldSimulator.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:ContinuousIntegrationBuild=true \
    /p:SourceRevisionId=$GIT_SHA \
    /p:InformationalVersion=$BUILD_VERSION

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# OCI image labels for traceability
ARG GIT_SHA=unknown
ARG BUILD_TIME=unknown
ARG BUILD_VERSION=dev
LABEL org.opencontainers.image.title="WorldSimulator" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.revision="$GIT_SHA" \
      org.opencontainers.image.created="$BUILD_TIME" \
      org.opencontainers.image.source="https://example.com/AmiaReforged/WorldSimulator"

# Copy published output from build stage
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:80 \
    DOTNET_ENVIRONMENT=Production

# Expose port (if needed for health checks)
EXPOSE 80

# Run the application
ENTRYPOINT ["dotnet", "WorldSimulator.dll"]

