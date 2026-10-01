using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.WorldEngine.API;

namespace AmiaReforged.PwEngine.Features.Glyph.API;

public sealed class GlyphModuleController
{
    internal static IGlyphModuleRepository? Repository;
    private static GlyphBootstrap? Runtime => GlyphController.Runtime;
    private static ApiResult Unavailable() => new(503, new ErrorResponse("Service unavailable", "Glyph module service is not initialized."));
    internal static async Task RefreshAsync(bool mutationHeld = false)
    {
        if (Repository == null || Runtime == null) return;
        if (!mutationHeld) await GlyphController.Mutations.WaitAsync();
        try
        {
            var modules = await Repository.GetAllAsync();
            Runtime.Compiler.Modules.Replace(new(modules.SelectMany(m => m.History()).Select(v => v.Revision),
                modules.Where(m => !m.IsArchived && m.ActiveRevisionId != null).Select(m => m.ActiveRevisionId!.Value)));
        }
        finally { if (!mutationHeld) GlyphController.Mutations.Release(); }
    }
    [HttpGet("/api/worldengine/glyph-modules")]
    public static async Task<ApiResult> List(RouteContext ctx) => Repository == null ? Unavailable() : new(200, (await Repository.GetAllAsync()).Select(Dto).ToArray());
    [HttpGet("/api/worldengine/glyph-modules/{id}")]
    public static async Task<ApiResult> Get(RouteContext ctx)
    {
        if (Repository == null) return Unavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out var id)) return new(400, new ErrorResponse("Bad request", "Invalid module ID."));
        var module = await Repository.GetAsync(id);
        return module == null ? new(404, new ErrorResponse("Not found", "Module not found.")) : new(200, Dto(module));
    }
    [HttpPost("/api/worldengine/glyph-modules")]
    public static async Task<ApiResult> Create(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return Unavailable();
        var request = await ctx.ReadJsonBodyAsync<ModuleRequest>();
        if (request == null || !Identifier(request.Name) || request.SourceText?.Length > 128 * 1024)
            return new(400, new ErrorResponse("Bad request", "A module identifier and source up to 128 KiB are required."));
        await GlyphController.Mutations.WaitAsync();
        try
        {
            if ((await Repository.GetAllAsync()).Any(m => m.Name == request.Name))
                return new(409, new ErrorResponse("Duplicate module", "A module with that name already exists, including archived modules."));
            var probe = Runtime.Compiler.CompileModule(GlyphModuleRevision.Create(request.Name, $"mod {request.Name} {{}}"));
            if (!probe.Success) return new(400, new { Success = false, probe.Diagnostics });
            var module = new GlyphModule { Name = request.Name, SourceText = request.SourceText ?? $"mod {request.Name} {{\n\n}}" };
            await Repository.SaveAsync(module, true);
            return new(201, Dto(module));
        }
        finally { GlyphController.Mutations.Release(); }
    }
    [HttpPut("/api/worldengine/glyph-modules/{id}")]
    public static async Task<ApiResult> Update(RouteContext ctx)
    {
        if (Repository == null) return Unavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out var id)) return new(400, new ErrorResponse("Bad request", "Invalid module ID."));
        var request = await ctx.ReadJsonBodyAsync<ModuleRequest>();
        if (request == null || request.SourceText?.Length > 128 * 1024) return new(400, new ErrorResponse("Bad request", "Source must be at most 128 KiB."));
        await GlyphController.Mutations.WaitAsync();
        try
        {
            var module = await Repository.GetAsync(id);
            if (module == null) return new(404, new ErrorResponse("Not found", "Module not found."));
            if (request.Name != module.Name) return new(409, new ErrorResponse("Stable module name", "Create another module to rename a published interface."));
            if (request.SourceText != null) module.SourceText = request.SourceText;
            GlyphModuleSnapshot? snapshot = null;
            if (request.IsArchived != null)
            {
                await RefreshAsync(mutationHeld: true);
                module.IsArchived = request.IsArchived.Value;
                snapshot = Runtime?.Compiler.Modules.Snapshot.Select(module.Name, module.IsArchived ? null : module.ActiveRevisionId);
            }
            await Repository.SaveAsync(module);
            if (snapshot != null) Runtime!.Compiler.Modules.Replace(snapshot);
            return new(200, Dto(module));
        }
        finally { GlyphController.Mutations.Release(); }
    }
    [HttpPost("/api/worldengine/glyph-modules/compile")]
    public static async Task<ApiResult> Compile(RouteContext ctx)
    {
        if (Runtime == null) return Unavailable();
        var request = await ctx.ReadJsonBodyAsync<ModuleRequest>();
        if (request?.SourceText == null || !Identifier(request.Name)) return new(400, new ErrorResponse("Bad request", "Name and SourceText are required."));
        await RefreshAsync();
        var revision = GlyphModuleRevision.Create(request.Name, request.SourceText);
        var result = Runtime.Compiler.CompileModule(revision);
        return Response(revision, result);
    }
    [HttpPost("/api/worldengine/glyph-modules/{id}/publish")]
    public static async Task<ApiResult> Publish(RouteContext ctx)
    {
        if (Repository == null || Runtime == null) return Unavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out var id)) return new(400, new ErrorResponse("Bad request", "Invalid module ID."));
        var request = await ctx.ReadJsonBodyAsync<ModulePublicationRequest>();
        return await PublishAsync(id, request);
    }
    internal static async Task<ApiResult> PublishAsync(Guid id, ModulePublicationRequest? request)
    {
        if (Repository == null || Runtime == null) return Unavailable();
        await GlyphController.Mutations.WaitAsync();
        try
        {
            var module = await Repository.GetAsync(id);
            if (module == null) return new(404, new ErrorResponse("Not found", "Module not found."));
            if (module.IsArchived) return new(409, new ErrorResponse("Archived module", "Restore the module before publishing."));
            await RefreshAsync(mutationHeld: true);
            var revision = GlyphModuleRevision.Create(module.Name, request?.SourceText ?? module.SourceText);
            var result = Runtime.Compiler.CompileModule(revision);
            if (!result.Success) return Response(revision, result);
            if (request?.ExpectedCompilationHash == null || request.ExpectedCompilationHash != Fingerprint(revision, result))
                return new(409, new ErrorResponse("Revalidation required", "Source or module dependencies changed. Compile / validate before publishing."));
            var dependencies = result.Binding!.Dependencies.ToDictionary(d => d.Name, StringComparer.Ordinal);
            List<GlyphDiagnostic> parseDiagnostics = [];
            var syntax = GlyphModuleBinding.Parse(revision.SourceText, module.Name + ".glyph", parseDiagnostics)!;
            revision = revision with { Imports = syntax.Imports.Select(i => dependencies[i.Name]).Select(d => new GlyphModuleReference(d.Name, d.RevisionId, d.SourceHash)).ToArray() };
            var history = module.History(); history.Add(new(revision, DateTime.UtcNow));
            module.PublishedVersionsJson = JsonSerializer.Serialize(history); module.ActiveRevisionId = revision.RevisionId; module.SourceText = revision.SourceText;
            var snapshot = Runtime.Compiler.Modules.Snapshot.Publish(revision);
            await Repository.SaveAsync(module);
            Runtime.Compiler.Modules.Replace(snapshot);
            return new(200, new { Success = true, Module = Dto(module), Diagnostics = Array.Empty<GlyphDiagnostic>() });
        }
        finally { GlyphController.Mutations.Release(); }
    }
    [HttpPost("/api/worldengine/glyph-modules/{id}/rollback")]
    public static async Task<ApiResult> Rollback(RouteContext ctx)
    {
        if (Repository == null) return Unavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out var id)) return new(400, new ErrorResponse("Bad request", "Invalid module ID."));
        await GlyphController.Mutations.WaitAsync();
        try
        {
            var module = await Repository.GetAsync(id);
            if (module == null) return new(404, new ErrorResponse("Not found", "Module not found."));
            var history = module.History(); int current = history.FindIndex(v => v.Revision.RevisionId == module.ActiveRevisionId);
            if (current < 1) return new(409, new ErrorResponse("No previous revision", "There is no previous module revision."));
            await RefreshAsync(mutationHeld: true);
            module.ActiveRevisionId = history[current - 1].Revision.RevisionId; module.SourceText = history[current - 1].Revision.SourceText;
            var snapshot = Runtime?.Compiler.Modules.Snapshot.Select(module.Name, module.IsArchived ? null : module.ActiveRevisionId);
            await Repository.SaveAsync(module);
            if (snapshot != null) Runtime!.Compiler.Modules.Replace(snapshot);
            return new(200, Dto(module));
        }
        finally { GlyphController.Mutations.Release(); }
    }
    [HttpDelete("/api/worldengine/glyph-modules/{id}")]
    public static async Task<ApiResult> Archive(RouteContext ctx)
    {
        if (Repository == null) return Unavailable();
        if (!Guid.TryParse(ctx.GetRouteValue("id"), out var id)) return new(400, new ErrorResponse("Bad request", "Invalid module ID."));
        await GlyphController.Mutations.WaitAsync();
        try
        {
            var module = await Repository.GetAsync(id);
            if (module == null) return new(404, new ErrorResponse("Not found", "Module not found."));
            await RefreshAsync(mutationHeld: true);
            var snapshot = Runtime?.Compiler.Modules.Snapshot.Select(module.Name, null);
            module.IsArchived = true; await Repository.SaveAsync(module);
            if (snapshot != null) Runtime!.Compiler.Modules.Replace(snapshot);
            return new(204, new { });
        }
        finally { GlyphController.Mutations.Release(); }
    }
    [HttpPost("/api/worldengine/glyphs/module-metadata")]
    public static async Task<ApiResult> Metadata(RouteContext ctx)
    {
        if (Runtime == null) return Unavailable();
        var request = await ctx.ReadJsonBodyAsync<GlyphController.CompileGlyphRequest>();
        if (request?.SourceText == null) return new(400, new ErrorResponse("Bad request", "SourceText is required."));
        await RefreshAsync();
        return new(200, GlyphModuleMetadata.Create(Runtime.Compiler, request.SourceText, request.LanguageVersion));
    }
    internal static string Fingerprint(GlyphModuleRevision revision, GlyphModuleCompilationResult result) =>
        GlyphModuleBinding.CompilationHash(revision.SourceHash, 2, result.Binding?.Dependencies.Where(d => d.Name != revision.Name) ?? []);
    private static ApiResult Response(GlyphModuleRevision revision, GlyphModuleCompilationResult result) => new(200, new
    { result.Success, result.Diagnostics, revision.SourceHash, CompilationHash = Fingerprint(revision, result), Dependencies = result.Binding?.Dependencies.Where(d => d.Name != revision.Name).Select(d => new GlyphModuleReference(d.Name, d.RevisionId, d.SourceHash)).ToArray() ?? [] });
    private static bool Identifier(string? name) => name is { Length: > 0 and <= 128 } && (char.IsLetter(name[0]) || name[0] == '_') && name.All(c => char.IsLetterOrDigit(c) || c == '_');
    private static object Dto(GlyphModule module) => new
    {
        module.Id, module.Name, module.SourceText, module.IsArchived, module.ActiveRevisionId, module.CreatedAt, module.UpdatedAt,
        Versions = module.History().Select(v => new { v.Revision.RevisionId, v.PublishedAt, v.Revision.SourceHash, v.Revision.Imports, IsActive = module.ActiveRevisionId == v.Revision.RevisionId }).ToArray()
    };
    public sealed record ModuleRequest(string Name, string? SourceText = null, bool? IsArchived = null);
    public sealed record ModulePublicationRequest(string? SourceText = null, string? ExpectedCompilationHash = null);
}
