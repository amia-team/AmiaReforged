using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

public sealed record GlyphToken(string Kind, string Text, SourceSpan Span, object? Value = null);

public abstract record GlyphSyntax(SourceSpan Span);

public sealed record GlyphCompilationUnitSyntax(
    IReadOnlyList<GlobalDeclarationSyntax> GlobalDeclarations,
    IReadOnlyList<TypeDeclarationSyntax> Declarations,
    string Name,
    string Event,
    BlockStatementSyntax Body,
    SourceSpan Span) : GlyphSyntax(Span)
{
    public IReadOnlyList<GlyphImportSyntax> Imports { get; init; } = [];
    public string? ModuleName { get; init; }
    public int LanguageVersion { get; init; } = Compilation.GlyphLanguageVersion.Current;
}

public sealed record GlyphImportSyntax(string Name, SourceSpan Span) : GlyphSyntax(Span);

// Common abstraction for every top-level declaration. Both prelude-style declarations
// (constant / function) and type declarations (struct / ADT) derive from this so a single
// lookup and collision policy can treat all four kinds without impossible sibling casts.
public abstract record GlyphDeclarationSyntax(string Name, SourceSpan Span) : GlyphSyntax(Span)
{
    public bool IsPublic { get; init; }
}

public abstract record GlobalDeclarationSyntax(string Name, SourceSpan Span) : GlyphDeclarationSyntax(Name, Span);

public sealed record ConstantDeclarationSyntax(
    string Name,
    ExpressionSyntax? Initializer,
    SourceSpan Span, string? TypeName = null) : GlobalDeclarationSyntax(Name, Span);

public sealed record FunctionDeclarationSyntax(
    string Name,
    IReadOnlyList<ParameterSyntax> Parameters,
    string ReturnType,
    FunctionBodySyntax Body,
    SourceSpan Span) : GlobalDeclarationSyntax(Name, Span)
{
    public FunctionDeclarationSyntax(string name, IReadOnlyList<ParameterSyntax> parameters, string returnType,
        ExpressionSyntax body, SourceSpan span) : this(name, parameters, returnType, new ExpressionFunctionBodySyntax(body), span) { }
}

public abstract record FunctionBodySyntax;
public sealed record ExpressionFunctionBodySyntax(ExpressionSyntax Expression) : FunctionBodySyntax;
public sealed record BlockFunctionBodySyntax(BlockStatementSyntax Block) : FunctionBodySyntax;

public abstract record TypeDeclarationSyntax(string Name, SourceSpan Span) : GlyphDeclarationSyntax(Name, Span);

public sealed record StructDeclarationSyntax(
    string Name,
    IReadOnlyList<GlyphFieldDeclarationSyntax> Fields,
    SourceSpan Span) : TypeDeclarationSyntax(Name, Span);

public sealed record AdtDeclarationSyntax(
    string Name,
    IReadOnlyList<GlyphVariantDeclarationSyntax> Variants,
    SourceSpan Span) : TypeDeclarationSyntax(Name, Span);

public sealed record GlyphVariantDeclarationSyntax(
    string Name,
    IReadOnlyList<GlyphFieldDeclarationSyntax> Fields,
    SourceSpan Span) : GlyphSyntax(Span);

public sealed record GlyphFieldDeclarationSyntax(
    string Name,
    string TypeName,
    SourceSpan Span) : GlyphSyntax(Span);

public abstract record StatementSyntax(SourceSpan Span) : GlyphSyntax(Span);
public sealed record BlockStatementSyntax(IReadOnlyList<StatementSyntax> Statements, SourceSpan Span) : StatementSyntax(Span);
public sealed record StageDeclarationSyntax(string Name, BlockStatementSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record VarStatementSyntax(string Name, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record WhileStatementSyntax(ExpressionSyntax Condition, BlockStatementSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record ForRangeStatementSyntax(string Name, ExpressionSyntax Start, ExpressionSyntax End, bool Inclusive, ExpressionSyntax? Step, BlockStatementSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record ContinueStatementSyntax(SourceSpan Span) : StatementSyntax(Span);
public sealed record LetStatementSyntax(string Name, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record AssignmentStatementSyntax(ExpressionSyntax Target, string Operator, ExpressionSyntax Value, SourceSpan Span) : StatementSyntax(Span);
public sealed record ExpressionStatementSyntax(ExpressionSyntax Expression, SourceSpan Span) : StatementSyntax(Span);
public sealed record IfStatementSyntax(ExpressionSyntax Condition, BlockStatementSyntax Then, StatementSyntax? Else, SourceSpan Span) : StatementSyntax(Span);
public sealed record ForeachStatementSyntax(string Name, ExpressionSyntax List, BlockStatementSyntax Body, SourceSpan Span) : StatementSyntax(Span);
public sealed record BreakStatementSyntax(SourceSpan Span) : StatementSyntax(Span);
public sealed record ReturnStatementSyntax(ExpressionSyntax? Value, SourceSpan Span) : StatementSyntax(Span);

public sealed record MatchStatementSyntax(
    ExpressionSyntax Value,
    IReadOnlyList<MatchArmSyntax> Arms,
    SourceSpan Span) : StatementSyntax(Span);

public abstract record MatchPatternSyntax(SourceSpan Span) : GlyphSyntax(Span);
public sealed record VariantPatternSyntax(string Variant, IReadOnlyList<string> Bindings, SourceSpan Span) : MatchPatternSyntax(Span);
public sealed record ValuePatternSyntax(ExpressionSyntax Value, SourceSpan Span) : MatchPatternSyntax(Span);
public sealed record WildcardPatternSyntax(SourceSpan Span) : MatchPatternSyntax(Span);
public sealed record MatchArmSyntax(MatchPatternSyntax Pattern, BlockStatementSyntax Body, SourceSpan Span) : GlyphSyntax(Span);

public abstract record ExpressionSyntax(SourceSpan Span) : GlyphSyntax(Span);
public sealed record LiteralExpressionSyntax(object Value, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record NameExpressionSyntax(string Name, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record MemberAccessExpressionSyntax(ExpressionSyntax Receiver, string Name, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record InvocationExpressionSyntax(ExpressionSyntax Function, IReadOnlyList<ArgumentSyntax> Arguments, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record ArgumentSyntax(string? Name, ExpressionSyntax Value, SourceSpan Span) : GlyphSyntax(Span);
public sealed record ParameterSyntax(string Name, string? TypeName, SourceSpan Span) : GlyphSyntax(Span);
public sealed record UnaryExpressionSyntax(string Operator, ExpressionSyntax Operand, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record BinaryExpressionSyntax(ExpressionSyntax Left, string Operator, ExpressionSyntax Right, SourceSpan Span) : ExpressionSyntax(Span);
public sealed record IndexExpressionSyntax(ExpressionSyntax Receiver, ExpressionSyntax Index, SourceSpan Span) : ExpressionSyntax(Span);
