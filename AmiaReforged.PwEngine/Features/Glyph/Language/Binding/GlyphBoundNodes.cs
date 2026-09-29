using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed record GlyphTypeSymbol(
    string Name,
    GlyphDataType? RuntimeType = null,
    GlyphTypeSymbol? ElementType = null)
{
    public static readonly GlyphTypeSymbol
        Error = new("Error"),
        Void = new("Void"),
        Bool = new("Bool", GlyphDataType.Bool),
        Int = new("Int", GlyphDataType.Int),
        Float = new("Float", GlyphDataType.Float),
        String = new("String", GlyphDataType.String),
        Object = new("Object", GlyphDataType.NwObject),
        Objects = new("List<Object>", GlyphDataType.List, Object);

    public static GlyphTypeSymbol From(GlyphDataType type) => type switch
    {
        GlyphDataType.Bool => Bool,
        GlyphDataType.Int => Int,
        GlyphDataType.Float => Float,
        GlyphDataType.String => String,
        GlyphDataType.NwObject => Object,
        GlyphDataType.List => Objects,
        _ => new(type.ToString(), type)
    };

    public bool IsNumeric => RuntimeType is GlyphDataType.Int or GlyphDataType.Float;
}

public sealed record GlyphFieldSymbol(string Name, GlyphTypeSymbol Type);

public sealed record GlyphStructDefinition(
    string Name,
    GlyphTypeSymbol Type,
    IReadOnlyList<GlyphFieldSymbol> Fields);

public sealed record GlyphVariantDefinition(
    string Name,
    IReadOnlyList<GlyphFieldSymbol> Fields);

public sealed record GlyphAdtDefinition(
    string Name,
    GlyphTypeSymbol Type,
    IReadOnlyList<GlyphVariantDefinition> Variants);

public sealed record BoundProgram(string Name, GlyphEventType Event, IReadOnlyList<BoundStage> Stages);
public sealed record BoundStage(string EntryTypeId, BoundBlock Body, SourceSpan Span);

public abstract record BoundStatement(SourceSpan Span);
public sealed record BoundBlock(IReadOnlyList<BoundStatement> Statements, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundIf(BoundExpression Condition, BoundStatement Then, BoundStatement? Else, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundForeach(int SymbolId, BoundExpression List, BoundBlock Body, SourceSpan Span) : BoundStatement(Span);
public sealed record BoundBreak(SourceSpan Span) : BoundStatement(Span);
public sealed record BoundExpressionStatement(BoundCall Call, SourceSpan Span) : BoundStatement(Span);

public abstract record BoundExpression(GlyphTypeSymbol Type, SourceSpan Span);
public sealed record BoundError(SourceSpan Span) : BoundExpression(GlyphTypeSymbol.Error, Span);
public sealed record BoundLiteral(object Value, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundContext(string EntryTypeId, string Pin, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundLoopElement(int SymbolId, SourceSpan Span) : BoundExpression(GlyphTypeSymbol.Object, Span);
public sealed record BoundCall(GlyphLanguageSymbol Symbol, IReadOnlyDictionary<string, BoundExpression> Arguments, SourceSpan Span) : BoundExpression(Symbol.ReturnType, Span);
public sealed record BoundUnary(string Operator, BoundExpression Operand, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);
public sealed record BoundBinary(BoundExpression Left, string Operator, BoundExpression Right, GlyphTypeSymbol Type, SourceSpan Span) : BoundExpression(Type, Span);

public sealed record BoundStruct(
    GlyphStructDefinition Definition,
    IReadOnlyDictionary<string, BoundExpression> Fields,
    SourceSpan Span) : BoundExpression(Definition.Type, Span);

public sealed record BoundVariant(
    GlyphAdtDefinition Definition,
    GlyphVariantDefinition Variant,
    IReadOnlyDictionary<string, BoundExpression> Fields,
    SourceSpan Span) : BoundExpression(Definition.Type, Span);

public sealed record BoundPlaceholder(
    GlyphTypeSymbol PlaceholderType,
    SourceSpan Span) : BoundExpression(PlaceholderType, Span);
