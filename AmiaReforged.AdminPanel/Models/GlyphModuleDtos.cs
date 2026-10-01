namespace AmiaReforged.AdminPanel.Models;

public sealed record GlyphModuleReferenceDto(string Name, Guid RevisionId, string SourceHash);
public sealed record GlyphModuleVersionDto(Guid RevisionId, DateTime PublishedAt, string SourceHash,
    IReadOnlyList<GlyphModuleReferenceDto> Imports, bool IsActive);
public sealed record GlyphModuleDto(Guid Id, string Name, string SourceText, bool IsArchived,
    Guid? ActiveRevisionId, DateTime CreatedAt, DateTime UpdatedAt, List<GlyphModuleVersionDto> Versions);
public sealed record GlyphModuleRequest(string Name, string? SourceText = null, bool? IsArchived = null);
public sealed record GlyphModulePublicationRequest(string? SourceText, string? ExpectedCompilationHash);
public sealed record GlyphAggregateFieldDto(string Name, string TypeName);
public sealed record GlyphAggregateVariantDto(string Name, IReadOnlyList<GlyphAggregateFieldDto> Fields);
public sealed record GlyphAggregateMetadataDto(string Name, IReadOnlyList<GlyphAggregateFieldDto> Fields,
    IReadOnlyList<GlyphAggregateVariantDto> Variants);
public sealed record GlyphModuleMetadataDto(IReadOnlyList<GlyphFunctionMetadataDto> Functions,
    IReadOnlyList<GlyphConstantMetadataDto> Constants, IReadOnlyList<string> Types,
    IReadOnlyList<GlyphAggregateMetadataDto> Aggregates, IReadOnlyList<string> Modules,
    IReadOnlyDictionary<string, GlyphSourceSpanDto> SourceLocations, string RegistryHash,
    IReadOnlyList<GlyphDiagnosticDto> Diagnostics)
{
    public IReadOnlyList<GlyphReceiverMethodMetadataDto> ReceiverMethods { get; init; } = [];
    public GlyphLanguageMetadataDto Merge(GlyphLanguageMetadataDto standard) => standard with
    {
        Functions = standard.Functions.Concat(Functions ?? []).GroupBy(f => f.Name).Select(g => g.Last()).ToArray(),
        ReceiverMethods = standard.ReceiverMethods.Concat(ReceiverMethods ?? []).ToArray(),
        Constants = standard.Constants.Concat(Constants ?? []).ToArray(),
        Types = standard.Types.Concat(Types ?? []).Distinct().ToArray(),
        Aggregates = Aggregates ?? [], Modules = Modules ?? [], SourceLocations = SourceLocations ?? new Dictionary<string, GlyphSourceSpanDto>(),
        ConstantDomains = standard.ConstantDomains.Concat((Constants ?? []).GroupBy(c => c.Namespace)
            .Select(g => new GlyphConstantDomainMetadataDto(g.Key, g.Select(c => c.Type).Distinct().ToArray(), g.Count()))).ToArray()
    };
}
