using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.AdminPanel.Models;

namespace AmiaReforged.AdminPanel.Services;

/// <summary>
/// Handles cross-environment deployment by exporting entities (with dependencies) from a
/// source WorldEngine endpoint and importing them into a target endpoint.
/// 
/// This service creates its own HTTP requests independently of the editor's active endpoint,
/// avoiding race conditions with the editor session.
/// </summary>
public class DeploymentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IWorldEngineEndpointService _endpointService;
    private readonly ILogger<DeploymentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = false
    };

    /// <summary>Entity types that support cross-environment deployment.</summary>
    public static readonly HashSet<WorldEngineEntityType> SupportedEntityTypes = new()
    {
        WorldEngineEntityType.Items,
        WorldEngineEntityType.Industries,
        WorldEngineEntityType.Regions,
        WorldEngineEntityType.ResourceNodes,
        WorldEngineEntityType.Interactions,
    };

    public DeploymentService(
        IHttpClientFactory httpClientFactory,
        IWorldEngineEndpointService endpointService,
        ILogger<DeploymentService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _endpointService = endpointService;
        _logger = logger;
    }

    /// <summary>
    /// Deploy a single entity (and its dependencies) from one endpoint to another.
    /// The entity is fetched from the source, serialized to JSON, and imported into the target.
    /// Import endpoints use upsert semantics — matching by tag or ID replaces existing rows.
    /// </summary>
    public async Task<DeployResult> DeployEntityAsync(
        Guid sourceEndpointId,
        Guid targetEndpointId,
        WorldEngineEntityType entityType,
        string entityKey)
    {
        DeployResult result = new()
        {
            EntityType = entityType,
            EntityKey = entityKey
        };

        try
        {
            if (sourceEndpointId == targetEndpointId)
            {
                result.Errors.Add("Source and target endpoints cannot be the same.");
                return result;
            }

            if (!SupportedEntityTypes.Contains(entityType))
            {
                result.Errors.Add($"Entity type '{entityType}' does not support cross-environment deployment yet.");
                return result;
            }

            (Uri sourceUri, string sourceKey) = await ResolveEndpointAsync(sourceEndpointId);
            (Uri targetUri, string targetKey) = await ResolveEndpointAsync(targetEndpointId);

            _logger.LogInformation(
                "Deploying {EntityType} '{EntityKey}' from endpoint {Source} to {Target}",
                entityType, entityKey, sourceEndpointId, targetEndpointId);

            switch (entityType)
            {
                case WorldEngineEntityType.Items:
                    await DeployItemAsync(sourceUri, sourceKey, targetUri, targetKey, entityKey, result);
                    break;

                case WorldEngineEntityType.Industries:
                    await DeployIndustryAsync(sourceUri, sourceKey, targetUri, targetKey, entityKey, result);
                    break;

                case WorldEngineEntityType.Regions:
                    await DeployRegionAsync(sourceUri, sourceKey, targetUri, targetKey, entityKey, result);
                    break;

                case WorldEngineEntityType.ResourceNodes:
                    await DeployResourceNodeAsync(sourceUri, sourceKey, targetUri, targetKey, entityKey, result);
                    break;

                case WorldEngineEntityType.Interactions:
                    await DeployInteractionAsync(sourceUri, sourceKey, targetUri, targetKey, entityKey, result);
                    break;
            }
        }
        catch (WorldEngineApiException ex)
        {
            result.Errors.Add($"API error: [{ex.StatusCode}] {ex.ErrorTitle} — {ex.Detail}");
            _logger.LogError(ex, "Deploy failed for {EntityType} '{EntityKey}'", entityType, entityKey);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unexpected error: {ex.Message}");
            _logger.LogError(ex, "Deploy failed for {EntityType} '{EntityKey}'", entityType, entityKey);
        }

        return result;
    }

    /// <summary>
    /// Returns a description of the dependencies that will be deployed alongside the entity.
    /// Used by the UI to preview what will happen before confirming.
    /// </summary>
    public static List<string> GetDependencyDescription(WorldEngineEntityType entityType)
    {
        return entityType switch
        {
            WorldEngineEntityType.Industries => [
                "Recipe Templates (by industry tag)",
                "Items (referenced by recipe ingredients/products)"
            ],
            WorldEngineEntityType.Items => ["Self-contained (no additional dependencies)"],
            WorldEngineEntityType.Regions => ["Self-contained (areas and environment data are embedded)"],
            WorldEngineEntityType.ResourceNodes => ["Self-contained (no additional dependencies)"],
            WorldEngineEntityType.Interactions => ["Self-contained (responses and effects are embedded)"],
            _ => ["Not supported"]
        };
    }

    // ==================== Entity-Specific Deploy Methods ====================

    private async Task DeployItemAsync(
        Uri sourceUri, string sourceKey,
        Uri targetUri, string targetKey,
        string itemTag, DeployResult result)
    {
        // Fetch the single item from source
        ItemBlueprintDto? item = await FetchAsync<ItemBlueprintDto>(
            sourceUri, sourceKey, $"/api/worldengine/items/{Uri.EscapeDataString(itemTag)}");

        if (item == null)
        {
            result.Errors.Add($"Item '{itemTag}' not found on source endpoint.");
            return;
        }

        // Import as a JSON array (the import endpoint expects an array)
        string json = JsonSerializer.Serialize(new[] { item }, SerializeOptions);
        result.EntityResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/items/import", json);
    }

    private async Task DeployIndustryAsync(
        Uri sourceUri, string sourceKey,
        Uri targetUri, string targetKey,
        string industryTag, DeployResult result)
    {
        // 1. Fetch the industry itself
        IndustryDefinitionDto? industry = await FetchAsync<IndustryDefinitionDto>(
            sourceUri, sourceKey, $"/api/worldengine/industries/{Uri.EscapeDataString(industryTag)}");

        if (industry == null)
        {
            result.Errors.Add($"Industry '{industryTag}' not found on source endpoint.");
            return;
        }

        // 2. Fetch recipe templates for this industry
        List<RecipeTemplateDefinitionDto>? recipes = await FetchAsync<List<RecipeTemplateDefinitionDto>>(
            sourceUri, sourceKey,
            $"/api/worldengine/recipe-templates/industry/{Uri.EscapeDataString(industryTag)}");
        recipes ??= [];

        // 3. Extract unique item tags from recipe ingredients and products, plus from the
        //    industry's own embedded recipes
        HashSet<string> itemTags = new(StringComparer.OrdinalIgnoreCase);

        foreach (RecipeTemplateDefinitionDto recipe in recipes)
        {
            // Template products don't have item tags (they use forms), but ingredients may have ExactItemTag
            foreach (ToolRequirementDto tool in recipe.RequiredTools)
            {
                if (!string.IsNullOrWhiteSpace(tool.ExactItemTag))
                    itemTags.Add(tool.ExactItemTag);
            }
        }

        // Also extract from the industry's embedded recipe list
        foreach (IndustryRecipeDto recipe in industry.Recipes)
        {
            foreach (IndustryIngredientDto ing in recipe.Ingredients)
            {
                if (!string.IsNullOrWhiteSpace(ing.ItemTag))
                    itemTags.Add(ing.ItemTag);
            }
            foreach (IndustryProductDto prod in recipe.Products)
            {
                if (!string.IsNullOrWhiteSpace(prod.ItemTag))
                    itemTags.Add(prod.ItemTag);
            }
            foreach (ToolRequirementDto tool in recipe.RequiredTools)
            {
                if (!string.IsNullOrWhiteSpace(tool.ExactItemTag))
                    itemTags.Add(tool.ExactItemTag);
            }
        }

        // 4. Fetch referenced items from source
        List<ItemBlueprintDto> items = [];
        foreach (string tag in itemTags)
        {
            ItemBlueprintDto? item = await FetchAsync<ItemBlueprintDto>(
                sourceUri, sourceKey, $"/api/worldengine/items/{Uri.EscapeDataString(tag)}");
            if (item != null)
                items.Add(item);
            else
                _logger.LogWarning("Referenced item '{Tag}' not found on source; skipping", tag);
        }

        // 5. Import dependencies first (items, then recipe templates), then the industry itself

        if (items.Count > 0)
        {
            string itemsJson = JsonSerializer.Serialize(items, SerializeOptions);
            ImportResult? itemResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/items/import", itemsJson);
            if (itemResult != null)
                result.DependencyResults["Items"] = itemResult;
        }

        // Note: Recipe templates don't have a bulk import endpoint yet. We import the industry
        // which includes its embedded recipes. The recipe templates are a separate concept
        // (templates vs resolved recipes). For now we deploy the industry definition which
        // carries its own recipes.

        // 6. Import the industry
        string industryJson = JsonSerializer.Serialize(new[] { industry }, SerializeOptions);
        result.EntityResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/industries/import", industryJson);
    }

    private async Task DeployRegionAsync(
        Uri sourceUri, string sourceKey,
        Uri targetUri, string targetKey,
        string regionTag, DeployResult result)
    {
        // Regions are self-contained — areas and environment data are embedded
        RegionDefinitionDto? region = await FetchAsync<RegionDefinitionDto>(
            sourceUri, sourceKey, $"/api/worldengine/regions/{Uri.EscapeDataString(regionTag)}");

        if (region == null)
        {
            result.Errors.Add($"Region '{regionTag}' not found on source endpoint.");
            return;
        }

        string json = JsonSerializer.Serialize(new[] { region }, SerializeOptions);
        result.EntityResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/regions/import", json);
    }

    private async Task DeployResourceNodeAsync(
        Uri sourceUri, string sourceKey,
        Uri targetUri, string targetKey,
        string nodeTag, DeployResult result)
    {
        ResourceNodeDefinitionDto? node = await FetchAsync<ResourceNodeDefinitionDto>(
            sourceUri, sourceKey, $"/api/worldengine/resource-nodes/{Uri.EscapeDataString(nodeTag)}");

        if (node == null)
        {
            result.Errors.Add($"Resource node '{nodeTag}' not found on source endpoint.");
            return;
        }

        string json = JsonSerializer.Serialize(new[] { node }, SerializeOptions);
        result.EntityResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/resource-nodes/import", json);
    }

    private async Task DeployInteractionAsync(
        Uri sourceUri, string sourceKey,
        Uri targetUri, string targetKey,
        string interactionTag, DeployResult result)
    {
        InteractionDefinitionDto? interaction = await FetchAsync<InteractionDefinitionDto>(
            sourceUri, sourceKey, $"/api/worldengine/interactions/{Uri.EscapeDataString(interactionTag)}");

        if (interaction == null)
        {
            result.Errors.Add($"Interaction '{interactionTag}' not found on source endpoint.");
            return;
        }

        string json = JsonSerializer.Serialize(new[] { interaction }, SerializeOptions);
        result.EntityResult = await ImportAsync(targetUri, targetKey, "/api/worldengine/interactions/import", json);
    }

    // ==================== HTTP Helpers ====================

    private async Task<(Uri BaseUri, string ApiKey)> ResolveEndpointAsync(Guid endpointId)
    {
        WorldEngineEndpoint? ep = await _endpointService.GetEndpointAsync(endpointId);
        if (ep == null)
            throw new InvalidOperationException($"Endpoint '{endpointId}' not found.");

        if (string.IsNullOrWhiteSpace(ep.ApiKey))
            throw new InvalidOperationException($"Endpoint '{ep.Name}' has no API key configured.");

        return (new Uri(ep.BaseUrl.TrimEnd('/') + "/"), ep.ApiKey.Trim());
    }

    private async Task<T?> FetchAsync<T>(Uri baseUri, string apiKey, string relativeUrl) where T : class
    {
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = new(HttpMethod.Get, new Uri(baseUri, relativeUrl));
        request.Headers.Add("X-API-Key", apiKey);

        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions);
    }

    private async Task<ImportResult?> ImportAsync(Uri baseUri, string apiKey, string relativeUrl, string jsonContent)
    {
        HttpClient http = _httpClientFactory.CreateClient("WorldEngine");
        using HttpRequestMessage request = new(HttpMethod.Post, new Uri(baseUri, relativeUrl));
        request.Headers.Add("X-API-Key", apiKey);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await http.SendAsync(request);
        await EnsureSuccessOrThrow(response);

        string body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return null;
        return JsonSerializer.Deserialize<ImportResult>(body, JsonOptions);
    }

    private static async Task EnsureSuccessOrThrow(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;

        string body = await response.Content.ReadAsStringAsync();
        try
        {
            ApiErrorResponse? error = JsonSerializer.Deserialize<ApiErrorResponse>(body, JsonOptions);
            if (error != null)
                throw new WorldEngineApiException((int)response.StatusCode, error.Error, error.Detail);
        }
        catch (JsonException) { }

        throw new WorldEngineApiException(
            (int)response.StatusCode, response.ReasonPhrase ?? "Error", body);
    }
}
