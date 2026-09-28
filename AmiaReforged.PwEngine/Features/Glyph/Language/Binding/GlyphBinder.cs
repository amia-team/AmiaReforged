using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed class GlyphBinder(GlyphLanguageCatalog catalog)
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];
    private GlyphEventType _event;
    private string _entry = "", _stage = "";
    private int _loops, _nextSymbol;
    private readonly Stack<Dictionary<string, BoundExpression>> _scopes = new();
    private void Error(string code, string message, SourceSpan span) => Diagnostics.Add(new(code, message, span));
    public BoundProgram? Bind(GlyphCompilationUnitSyntax syntax)
    {
        if (!GlyphLanguageCatalog.Events.TryGetValue(syntax.Event, out _event))
        { Error("GLYPH3001", $"Unknown event '{syntax.Event}'.", syntax.Span); return null; }
        List<BoundStage> stages = [];
        if (_event == GlyphEventType.InteractionPipeline)
        {
            HashSet<string> seen = [];
            foreach (StatementSyntax statement in syntax.Body.Statements)
            {
                if (statement is not StageDeclarationSyntax stage)
                { Error("GLYPH3003", "Interaction statements must be inside a stage.", statement.Span); continue; }
                if (!seen.Add(stage.Name)) Error("GLYPH2006", $"Duplicate stage '{stage.Name}'.", stage.Span);
                _stage = stage.Name; _entry = "stage.interaction_" + stage.Name;
                stages.Add(new(_entry, Block(stage.Body), stage.Span));
            }
            // Omitted stages remain independent no-op entry points.
            foreach (string name in new[] { "attempted", "started", "tick", "completed" })
                if (!seen.Contains(name)) stages.Add(new("stage.interaction_" + name, new([], syntax.Span), syntax.Span));
        }
        else
        {
            _entry = GlyphLanguageCatalog.EntryType(_event);
            stages.Add(new(_entry, Block(syntax.Body), syntax.Span));
        }
        return new(syntax.Name, _event, stages);
    }
    private BoundBlock Block(BlockStatementSyntax block)
    {
        _scopes.Push(new(StringComparer.Ordinal));
        List<BoundStatement> statements = [];
        foreach (StatementSyntax statement in block.Statements)
        {
            var bound = Statement(statement);
            if (bound != null) statements.Add(bound);
        }
        _scopes.Pop();
        return new(statements, block.Span);
    }
    private BoundStatement? Statement(StatementSyntax syntax)
    {
        switch (syntax)
        {
            case BlockStatementSyntax block: return Block(block);
            case LetStatementSyntax let:
                BoundExpression value = Expression(let.Value);
                if (!_scopes.Peek().TryAdd(let.Name, value)) Error("GLYPH2006", $"Duplicate local '{let.Name}'.", let.Span);
                return null;
            case BreakStatementSyntax br:
                if (_loops == 0) Error("GLYPH3004", "break requires an enclosing foreach.", br.Span);
                return new BoundBreak(br.Span);
            case IfStatementSyntax conditional:
                BoundExpression condition = Expression(conditional.Condition, allowPredicate: true);
                Require(condition, GlyphTypeSymbol.Bool);
                return new BoundIf(condition, Block(conditional.Then), conditional.Else == null ? null : Statement(conditional.Else), conditional.Span);
            case ForeachStatementSyntax loop:
                BoundExpression list = Expression(loop.List);
                Require(list, GlyphTypeSymbol.Objects);
                int symbol = _nextSymbol++;
                _scopes.Push(new(StringComparer.Ordinal) { [loop.Name] = new BoundLoopElement(symbol, loop.Span) });
                _loops++;
                BoundBlock body = Block(loop.Body);
                _loops--; _scopes.Pop();
                return new BoundForeach(symbol, list, body, loop.Span);
            case AssignmentStatementSyntax assignment:
                return Assignment(assignment);
            case ExpressionStatementSyntax expr:
                if (expr.Expression is InvocationExpressionSyntax invocation)
                {
                    BoundExpression call = Call(invocation, allowAction: true);
                    if (call is BoundCall action && action.Symbol.Strategy == GlyphLoweringStrategy.Action)
                        return new BoundExpressionStatement(action, expr.Span);
                }
                Error("GLYPH2007", "Only action calls may be used as statements.", expr.Span); return null;
            default: Error("GLYPH3003", "Stage declarations are only valid at the top of an interaction.", syntax.Span); return null;
        }
    }
    private BoundStatement? Assignment(AssignmentStatementSyntax assignment)
    {
        string? name = Path(assignment.Target);
        string? setter = name switch { "progress" => "set_progress", "required_rounds" => "set_required_rounds", "status" => "set_status", _ => null };
        List<ArgumentSyntax> args = [];
        ExpressionSyntax value = assignment.Value;
        if (assignment.Operator != "=") value = new BinaryExpressionSyntax(assignment.Target, assignment.Operator[..1], value, assignment.Span);
        if (assignment.Target is IndexExpressionSyntax { Receiver: NameExpressionSyntax { Name: "metadata" } } index)
        { setter = "set_metadata"; args.Add(new(null, index.Index, index.Span)); }
        if (setter == null)
        { Error("GLYPH2008", "let bindings are immutable; only interaction state and metadata support assignment.", assignment.Span); return null; }
        args.Add(new(null, value, value.Span));
        var invocation = new InvocationExpressionSyntax(new NameExpressionSyntax(setter, assignment.Span), args, assignment.Span);
        BoundExpression call = Call(invocation, allowAction: true);
        return call is BoundCall bound ? new BoundExpressionStatement(bound, assignment.Span) : null;
    }
    private BoundExpression Expression(ExpressionSyntax syntax, bool allowPredicate = false)
    {
        switch (syntax)
        {
            case LiteralExpressionSyntax literal:
                return new BoundLiteral(literal.Value, literal.Value switch
                { bool => GlyphTypeSymbol.Bool, int => GlyphTypeSymbol.Int, double => GlyphTypeSymbol.Float, _ => GlyphTypeSymbol.String }, literal.Span);
            case InvocationExpressionSyntax call: return Call(call, allowPredicate: allowPredicate);
            case UnaryExpressionSyntax unary:
                BoundExpression operand = Expression(unary.Operand);
                if (unary.Operator == "!") Require(operand, GlyphTypeSymbol.Bool);
                else if (!operand.Type.IsNumeric) Error("GLYPH2005", "Unary arithmetic requires a number.", unary.Span);
                return new BoundUnary(unary.Operator, operand, unary.Operator == "!" ? GlyphTypeSymbol.Bool : GlyphTypeSymbol.Float, unary.Span);
            case BinaryExpressionSyntax binary:
                BoundExpression left = Expression(binary.Left), right = Expression(binary.Right);
                bool boolean = binary.Operator is "&&" or "||";
                if (boolean) { Require(left, GlyphTypeSymbol.Bool); Require(right, GlyphTypeSymbol.Bool); }
                else if (!left.Type.IsNumeric || !right.Type.IsNumeric)
                    Error("GLYPH2005", "This operator requires numeric operands.", binary.Span);
                return new BoundBinary(left, binary.Operator, right, boolean || binary.Operator is "==" or "!=" or "<" or "<=" or ">" or ">=" ? GlyphTypeSymbol.Bool : GlyphTypeSymbol.Float, binary.Span);
            case IndexExpressionSyntax { Receiver: NameExpressionSyntax { Name: "metadata" } } index:
                return Call(new(new NameExpressionSyntax("metadata", index.Span), [new(null, index.Index, index.Span)], index.Span));
        }
        string? path = Path(syntax);
        if (path != null)
        {
            foreach (var scope in _scopes) if (scope.TryGetValue(path, out var local)) return local;
            if (path is "creature.hp" or "creature.max_hp" or "creature.name" or "creature.ac")
                return Call(new(new NameExpressionSyntax(path, syntax.Span), [new(null, new NameExpressionSyntax("creature", syntax.Span), syntax.Span)], syntax.Span));
            if (path == "party.members") return Call(new(new NameExpressionSyntax(path, syntax.Span), [], syntax.Span));
            string pin = path switch
            {
                "party.size" => "party_size", "time.hour" => "game_time", "spawn.count" => "spawn_count",
                "player" => _event == GlyphEventType.InteractionPipeline ? "creature" : "triggering_player",
                "creature" when _event.GetCategory() == GlyphScriptCategory.Trait => "target_creature",
                _ when path.StartsWith("context.") => path[8..],
                _ when path.StartsWith("chaos.") => path[6..],
                _ => path
            };
            GlyphPin? context = catalog.Registry.Get(_entry)?.OutputPins.FirstOrDefault(p => p.Id == pin && p.DataType != GlyphDataType.Exec);
            if (context != null)
            {
                // Prefer runtime context getters so state writes are visible on the next source read.
                var getter = catalog.Registry.Get($"context.{_entry}.{pin}");
                if (getter != null)
                    return new BoundCall(new(path, getter, "value", GlyphLoweringStrategy.Value), new Dictionary<string, BoundExpression>(), syntax.Span);
                return new BoundContext(_entry, pin, GlyphTypeSymbol.From(context.DataType), syntax.Span);
            }
        }
        Error("GLYPH3002", $"Unknown name or unavailable context '{path ?? "expression"}'.", syntax.Span);
        return new BoundError(syntax.Span);
    }
    private BoundExpression Call(InvocationExpressionSyntax syntax, bool allowAction = false, bool allowPredicate = false)
    {
        string? name = Path(syntax.Function);
        List<ArgumentSyntax> arguments = syntax.Arguments.ToList();
        // Receiver sugar injects real context parameters; it never invokes .NET members.
        if (name is "player.has_knowledge" or "player.has_item")
        {
            string context = name == "player.has_knowledge" ? "context.character_id" : "player";
            name = name == "player.has_knowledge" ? "has_knowledge" : "has_item";
            arguments.Insert(0, new(null, new NameExpressionSyntax(context, syntax.Span), syntax.Span));
        }
        GlyphLanguageSymbol? symbol = name == null ? null : catalog.Find(name);
        if (symbol == null) { Error("GLYPH2002", $"Unknown function '{name}'.", syntax.Span); return new BoundError(syntax.Span); }
        if (symbol.Definition.RestrictToEventType is { } evt && evt != _event ||
            symbol.Definition.ScriptCategory is { } cat && cat != _event.GetCategory())
            Error("GLYPH3001", $"'{name}' is unavailable for {_event}.", syntax.Span);
        if (symbol.AllowedStages != null && !symbol.AllowedStages.Contains(_stage))
            Error("GLYPH3003", $"'{name}' is unavailable in stage '{_stage}'.", syntax.Span);
        if (name == "fail" && _loops > 0)
            Error("GLYPH3003", "fail inside foreach is not supported; fail before or after the loop.", syntax.Span);
        if (symbol.Strategy == GlyphLoweringStrategy.Action && !allowAction ||
            symbol.Strategy == GlyphLoweringStrategy.PredicateBranch && !allowPredicate)
            Error("GLYPH2007", $"'{name}' requires {(symbol.Strategy == GlyphLoweringStrategy.Action ? "an action statement" : "a direct if condition")}.", syntax.Span);
        Dictionary<string, BoundExpression> bound = new();
        int position = 0;
        bool namedSeen = false;
        foreach (ArgumentSyntax arg in arguments)
        {
            GlyphPin? parameter;
            if (arg.Name != null) { namedSeen = true; parameter = symbol.Parameters.FirstOrDefault(p => p.Id == arg.Name); }
            else
            {
                if (namedSeen) Error("GLYPH2003", "Positional arguments must precede named arguments.", arg.Span);
                parameter = position < symbol.Parameters.Count ? symbol.Parameters[position++] : null;
            }
            BoundExpression value = Expression(arg.Value);
            if (parameter == null || bound.ContainsKey(parameter.Id))
            { Error("GLYPH2003", "Unknown, excess, or duplicate argument.", arg.Span); continue; }
            Require(value, GlyphTypeSymbol.From(parameter.DataType));
            bound[parameter.Id] = value;
        }
        foreach (GlyphPin parameter in symbol.Parameters)
            if (!bound.ContainsKey(parameter.Id) && parameter.DefaultValue == null)
                Error("GLYPH2003", $"Missing argument '{parameter.Id}'.", syntax.Span);
        if (name == "set_status" && bound.GetValueOrDefault("status") is BoundLiteral { Value: string status } &&
            status is not ("Active" or "Completed" or "Cancelled" or "Failed"))
            Error("GLYPH2004", "Status must be Active, Completed, Cancelled, or Failed.", syntax.Span);
        return new BoundCall(symbol, bound, syntax.Span);
    }
    private void Require(BoundExpression value, GlyphTypeSymbol type)
    {
        if (value.Type == GlyphTypeSymbol.Error) return;
        if (value.Type.RuntimeType == null || type.RuntimeType == null || !GlyphIrValidator.CanConnect(value.Type.RuntimeType.Value, type.RuntimeType.Value))
            Error("GLYPH2004", $"Cannot convert {value.Type.Name} to {type.Name}.", value.Span);
    }
    private static string? Path(ExpressionSyntax syntax) => syntax switch
    { NameExpressionSyntax name => name.Name, MemberAccessExpressionSyntax member when Path(member.Receiver) is { } prefix => prefix + "." + member.Name, _ => null };
}
