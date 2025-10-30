# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Build metadata
ARG GIT_SHA=unknown
ARG BUILD_TIME=unknown
ARG BUILD_VERSION=dev

# Copy project files
COPY ["AmiaReforged.PwEngine/AmiaReforged.PwEngine.csproj", "AmiaReforged.PwEngine/"]
COPY ["AmiaReforged.Core/AmiaReforged.Core.csproj", "AmiaReforged.Core/"]

# Restore dependencies
RUN cd AmiaReforged.PwEngine && dotnet restore

# Copy source code
COPY . .
WORKDIR "/src/AmiaReforged.PwEngine"

# Build and publish in Release mode
RUN dotnet publish "AmiaReforged.PwEngine.csproj" -c Release -o /app/publish \
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
LABEL org.opencontainers.image.title="PwEngine" \
      org.opencontainers.image.version="$BUILD_VERSION" \
      org.opencontainers.image.revision="$GIT_SHA" \
      org.opencontainers.image.created="$BUILD_TIME"

# Copy published output from build stage
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_ENVIRONMENT=Production

# Expose port for API
EXPOSE 8080

# Run the application
ENTRYPOINT ["dotnet", "AmiaReforged.PwEngine.dll"]

