# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build metadata
ARG GIT_SHA=unknown
ARG BUILD_TIME=unknown
ARG BUILD_VERSION=dev

# Copy project file
COPY ["AmiaReforged.AdminPanel/AmiaReforged.AdminPanel.csproj", "AmiaReforged.AdminPanel/"]

# Restore dependencies
RUN dotnet restore "AmiaReforged.AdminPanel/AmiaReforged.AdminPanel.csproj"

# Copy source code
COPY . .
WORKDIR "/src/AmiaReforged.AdminPanel"

# Build and publish in Release mode
RUN dotnet publish "AmiaReforged.AdminPanel.csproj" -c Release -o /app/publish \
    /p:UseAppHost=false \
    /p:ContinuousIntegrationBuild=true \
    /p:SourceRevisionId=$GIT_SHA \
    /p:InformationalVersion=$BUILD_VERSION

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# OCI image labels
ARG GIT_SHA=unknown
ARG BUILD_TIME=unknown
ARG BUILD_VERSION=dev
LABEL org.opencontainers.image.title="Amia Admin Panel" \
      org.opencontainers.image.description="Docker container monitoring and management panel" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.revision="$GIT_SHA" \
      org.opencontainers.image.created="$BUILD_TIME"

# Copy published output from build stage
COPY --from=build /app/publish .

# Create data directory for config persistence
RUN mkdir -p /data

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Volume for persistent config
VOLUME ["/data"]

# Run the application
ENTRYPOINT ["dotnet", "AmiaReforged.AdminPanel.dll"]
