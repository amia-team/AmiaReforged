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
    private readonly Dictionary<string, GlyphTypeSymbol> _userTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphStructDefinition> _structs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphAdtDefinition> _adts = new(StringComparer.Ordinal);

    private void Error(string code, string message, SourceSpan span) =>
        Diagnostics.Add(new(code, message, span));

    public BoundProgram? Bind(GlyphCompilationUnitSyntax syntax)
    {
        DeclareTypes(syntax.Declarations);

        if (!GlyphLanguageCatalog.Events.TryGetValue(syntax.Event, out _event))
        {
            Error("GLYPH3001", $"Unknown event '{syntax.Event}'.", syntax.Span);
            return null;
        }

        List<BoundStage> stages = [];

        if (_event == GlyphEventType.InteractionPipeline)
        {
            HashSet<string> seen = [];

            foreach (StatementSyntax statement in syntax.Body.Statements)
            {
                if (statement is not StageDeclarationSyntax stage)
                {
                    Error("GLYPH3003", "Interaction statements must be inside a stage.", statement.Span);
                    continue;
                }

                if (!seen.Add(stage.Name))
                    Error("GLYPH2006", $"Duplicate stage '{stage.Name}'.", stage.Span);

                _stage = stage.Name;
                _entry = "stage.interaction_" + stage.Name;
                stages.Add(new(_entry, Block(stage.Body), stage.Span));
            }

            foreach (string name in GlyphLanguageAliases.Stages)
                if (!seen.Contains(name))
                    stages.Add(new("stage.interaction_" + name, new([], syntax.Span), syntax.Span));
        }
        else
        {
            _entry = GlyphLanguageCatalog.EntryType(_event);
            stages.Add(new(_entry, Block(syntax.Body), syntax.Span));
        }

        return new(syntax.Name, _event, stages);
    }

    private void DeclareTypes(IReadOnlyList<TypeDeclarationSyntax> declarations)
    {
        foreach (TypeDeclarationSyntax declaration in declarations)
        {
            if (BuiltinType(declaration.Name) != null ||
                !_userTypes.TryAdd(declaration.Name, new(declaration.Name)))
                Error("GLYPH2006", $"Duplicate or reserved type '{declaration.Name}'.", declaration.Span);
        }

        HashSet<string> materialized = new(StringComparer.Ordinal);

        foreach (TypeDeclarationSyntax declaration in declarations)
        {
            if (!_userTypes.TryGetValue(declaration.Name, out GlyphTypeSymbol? type) ||
                !materialized.Add(declaration.Name))
                continue;

            switch (declaration)
            {
                case StructDeclarationSyntax structure:
                    _structs[structure.Name] = new(
                        structure.Name,
                        type,
                        BindFields(structure.Fields));
                    break;

                case AdtDeclarationSyntax adt:
                {
                    HashSet<string> variants = new(StringComparer.Ordinal);
                    List<GlyphVariantDefinition> boundVariants = [];

                    foreach (GlyphVariantDeclarationSyntax variant in adt.Variants)
                    {
                        if (!variants.Add(variant.Name))
                        {
                            Error("GLYPH2006", $"Duplicate variant '{adt.Name}.{variant.Name}'.", variant.Span);
                            continue;
                        }

                        boundVariants.Add(new(variant.Name, BindFields(variant.Fields)));
                    }

                    if (boundVariants.Count == 0)
                        Error("GLYPH2010", $"ADT '{adt.Name}' must declare at least one variant.", adt.Span);

                    _adts[adt.Name] = new(adt.Name, type, boundVariants);
                    break;
                }
            }
        }
    }

    private IReadOnlyList<GlyphFieldSymbol> BindFields(IReadOnlyList<GlyphFieldDeclarationSyntax> fields)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<GlyphFieldSymbol> result = [];

        foreach (GlyphFieldDeclarationSyntax field in fields)
        {
            if (!seen.Add(field.Name))
            {
                Error("GLYPH2006", $"Duplicate field '{field.Name}'.", field.Span);
                continue;
            }

            result.Add(new(field.Name, ResolveType(field.TypeName, field.Span)));
        }

        return result;
    }

    private GlyphTypeSymbol ResolveType(string name, SourceSpan span)
    {
        GlyphTypeSymbol? builtin = BuiltinType(name);
        if (builtin != null) return builtin;

        if (_userTypes.TryGetValue(name, out GlyphTypeSymbol? user))
            return user;

        Error("GLYPH3002", $"Unknown type '{name}'.", span);
        return GlyphTypeSymbol.Error;
    }

    private static GlyphTypeSymbol? BuiltinType(string name) => name switch
    {
        "Bool" => GlyphTypeSymbol.Bool,
        "Int" => GlyphTypeSymbol.Int,
        "Float" => GlyphTypeSymbol.Float,
        "String" => GlyphTypeSymbol.String,
        "Object" => GlyphTypeSymbol.Object,
        _ => null
    };

    private BoundBlock Block(BlockStatementSyntax block)
    {
        _scopes.Push(new(StringComparer.Ordinal));
        List<BoundStatement> statements = [];

        foreach (StatementSyntax statement in block.Statements)
        {
            BoundStatement? bound = Statement(statement);
            if (bound != null) statements.Add(bound);
        }

        _scopes.Pop();
        return new(statements, block.Span);
    }

    private BoundStatement? Statement(StatementSyntax syntax)
    {
        switch (syntax)
        {
            case BlockStatementSyntax block:
                return Block(block);

            case LetStatementSyntax let:
            {
                BoundExpression value = Expression(let.Value);
                if (!_scopes.Peek().TryAdd(let.Name, value))
                    Error("GLYPH2006", $"Duplicate local '{let.Name}'.", let.Span);
                return null;
            }

            case BreakStatementSyntax br:
                if (_loops == 0)
                    Error("GLYPH3004", "break requires an enclosing foreach.", br.Span);
                return new BoundBreak(br.Span);

            case IfStatementSyntax conditional:
            {
                BoundExpression condition = Expression(conditional.Condition, allowPredicate: true);
                Require(condition, GlyphTypeSymbol.Bool);
                return new BoundIf(
                    condition,
                    Block(conditional.Then),
                    conditional.Else == null ? null : Statement(conditional.Else),
                    conditional.Span);
            }

            case ForeachStatementSyntax loop:
            {
                BoundExpression list = Expression(loop.List);
                Require(list, GlyphTypeSymbol.Objects);
                int symbol = _nextSymbol++;

                _scopes.Push(new(StringComparer.Ordinal)
                {
                    [loop.Name] = new BoundLoopElement(symbol, loop.Span)
                });

                _loops++;
                BoundBlock body = Block(loop.Body);
                _loops--;
                _scopes.Pop();

                return new BoundForeach(symbol, list, body, loop.Span);
            }

            case MatchStatementSyntax match:
                return Match(match);

            case AssignmentStatementSyntax assignment:
                return Assignment(assignment);

            case ExpressionStatementSyntax expr:
            {
                if (expr.Expression is InvocationExpressionSyntax invocation)
                {
                    BoundExpression call = Call(invocation, allowAction: true);
                    if (call is BoundCall action &&
                        action.Symbol.Strategy == GlyphLoweringStrategy.Action)
                        return new BoundExpressionStatement(action, expr.Span);
                }

                Error("GLYPH2007", "Only action calls may be used as statements.", expr.Span);
                return null;
            }

            default:
                Error("GLYPH3003", "Stage declarations are only valid at the top of an interaction.", syntax.Span);
                return null;
        }
    }

    private BoundStatement Match(MatchStatementSyntax syntax)
    {
        BoundExpression value = Expression(syntax.Value);

        if (!_adts.TryGetValue(value.Type.Name, out GlyphAdtDefinition? adt))
        {
            Error("GLYPH2010", $"match requires an ADT value, got {value.Type.Name}.", syntax.Value.Span);
            return new BoundBlock([], syntax.Span);
        }

        BoundVariant? selected = value as BoundVariant;
        if (selected == null)
        {
            Error(
                "GLYPH2011",
                "This Glyph version can only match an ADT variant constructed in source. " +
                "Dynamic runtime ADT values are reserved for the runtime-aggregate slice.",
                syntax.Value.Span);
        }

        HashSet<string> seen = new(StringComparer.Ordinal);
        BoundBlock? selectedBody = null;

        foreach (MatchArmSyntax arm in syntax.Arms)
        {
            GlyphVariantDefinition? variant = adt.Variants.FirstOrDefault(v => v.Name == arm.Variant);

            if (variant == null)
            {
                Error("GLYPH2010", $"Unknown variant '{adt.Name}.{arm.Variant}'.", arm.Span);
                Block(arm.Body);
                continue;
            }

            if (!seen.Add(arm.Variant))
                Error("GLYPH2010", $"Duplicate match arm '{arm.Variant}'.", arm.Span);

            Dictionary<string, BoundExpression> patternScope = new(StringComparer.Ordinal);
            HashSet<string> boundNames = new(StringComparer.Ordinal);

            foreach (string binding in arm.Bindings)
            {
                if (!boundNames.Add(binding))
                {
                    Error("GLYPH2010", $"Duplicate pattern binding '{binding}'.", arm.Span);
                    continue;
                }

                GlyphFieldSymbol? field = variant.Fields.FirstOrDefault(f => f.Name == binding);
                if (field == null)
                {
                    Error("GLYPH2010", $"Variant '{adt.Name}.{variant.Name}' has no field '{binding}'.", arm.Span);
                    continue;
                }

                BoundExpression bindingValue =
                    selected != null &&
                    selected.Variant.Name == variant.Name &&
                    selected.Fields.TryGetValue(binding, out BoundExpression? actual)
                        ? actual
                        : new BoundPlaceholder(field.Type, arm.Span);

                patternScope[binding] = bindingValue;
            }

            _scopes.Push(patternScope);
            BoundBlock body;
            try
            {
                body = Block(arm.Body);
            }
            finally
            {
                _scopes.Pop();
            }

            if (selected != null && selected.Variant.Name == variant.Name)
                selectedBody = body;
        }

        foreach (GlyphVariantDefinition variant in adt.Variants)
        {
            if (!seen.Contains(variant.Name))
                Error("GLYPH2010", $"Non-exhaustive match on '{adt.Name}'; missing variant '{variant.Name}'.", syntax.Span);
        }

        return selectedBody ?? new BoundBlock([], syntax.Span);
    }

    private BoundStatement? Assignment(AssignmentStatementSyntax assignment)
    {
        string? name = Path(assignment.Target);
        string? setter = name == null ? null : GlyphLanguageAliases.Setters.GetValueOrDefault(name);

        List<ArgumentSyntax> args = [];
        ExpressionSyntax value = assignment.Value;

        if (assignment.Operator != "=")
            value = new BinaryExpressionSyntax(assignment.Target, assignment.Operator[..1], value, assignment.Span);

        if (assignment.Target is IndexExpressionSyntax
            {
                Receiver: NameExpressionSyntax { Name: GlyphLanguageAliases.MetadataName }
            } index)
        {
            setter = GlyphLanguageAliases.MetadataSetter;
            args.Add(new(null, index.Index, index.Span));
        }

        if (setter == null)
        {
            Error("GLYPH2008", "let bindings are immutable; only interaction state and metadata support assignment.", assignment.Span);
            return null;
        }

        args.Add(new(null, value, value.Span));

        var invocation = new InvocationExpressionSyntax(
            new NameExpressionSyntax(setter, assignment.Span),
            args,
            assignment.Span);

        BoundExpression call = Call(invocation, allowAction: true);
        return call is BoundCall bound ? new BoundExpressionStatement(bound, assignment.Span) : null;
    }

    private BoundExpression Expression(ExpressionSyntax syntax, bool allowPredicate = false)
    {
        switch (syntax)
        {
            case LiteralExpressionSyntax literal:
                return new BoundLiteral(
                    literal.Value,
                    literal.Value switch
                    {
                        bool => GlyphTypeSymbol.Bool,
                        int => GlyphTypeSymbol.Int,
                        double => GlyphTypeSymbol.Float,
                        _ => GlyphTypeSymbol.String
                    },
                    literal.Span);

            case InvocationExpressionSyntax call:
                return Call(call, allowPredicate: allowPredicate);

            case MemberAccessExpressionSyntax member
                when TryAggregateMember(member, out BoundExpression aggregateMember):
                return aggregateMember;

            case UnaryExpressionSyntax unary:
            {
                BoundExpression operand = Expression(unary.Operand);
                if (unary.Operator == "!") Require(operand, GlyphTypeSymbol.Bool);
                else if (!operand.Type.IsNumeric) Error("GLYPH2005", "Unary arithmetic requires a number.", unary.Span);

                return new BoundUnary(
                    unary.Operator,
                    operand,
                    unary.Operator == "!" ? GlyphTypeSymbol.Bool : GlyphTypeSymbol.Float,
                    unary.Span);
            }

            case BinaryExpressionSyntax binary:
            {
                BoundExpression left = Expression(binary.Left);
                BoundExpression right = Expression(binary.Right);
                bool boolean = binary.Operator is "&&" or "||";

                if (boolean)
                {
                    Require(left, GlyphTypeSymbol.Bool);
                    Require(right, GlyphTypeSymbol.Bool);
                }
                else if (!left.Type.IsNumeric || !right.Type.IsNumeric)
                {
                    Error("GLYPH2005", "This operator requires numeric operands.", binary.Span);
                }

                return new BoundBinary(
                    left,
                    binary.Operator,
                    right,
                    boolean || binary.Operator is "==" or "!=" or "<" or "<=" or ">" or ">="
                        ? GlyphTypeSymbol.Bool
                        : GlyphTypeSymbol.Float,
                    binary.Span);
            }

            case IndexExpressionSyntax
            {
                Receiver: NameExpressionSyntax { Name: GlyphLanguageAliases.MetadataName }
            } index:
                return Call(new(
                    new NameExpressionSyntax(GlyphLanguageAliases.MetadataGetter, index.Span),
                    [new(null, index.Index, index.Span)],
                    index.Span));
        }

        string? path = Path(syntax);

        if (path != null)
        {
            foreach (var scope in _scopes)
                if (scope.TryGetValue(path, out BoundExpression? local))
                    return local;

            var property = GlyphLanguageAliases.Properties.FirstOrDefault(a => a.Name == path);
            if (property != null)
            {
                return Call(new(
                    new NameExpressionSyntax(property.Target, syntax.Span),
                    property.ImplicitArgument == null
                        ? []
                        : [new(null, new NameExpressionSyntax(property.ImplicitArgument, syntax.Span), syntax.Span)],
                    syntax.Span));
            }

            string pin = GlyphLanguageAliases.ContextPin(path, _event);
            GlyphPin? context = catalog.Registry.Get(_entry)?.OutputPins
                .FirstOrDefault(p => p.Id == pin && p.DataType != GlyphDataType.Exec);

            if (context != null)
            {
                var getter = catalog.Registry.Get($"context.{_entry}.{pin}");
                if (getter != null)
                {
                    return new BoundCall(
                        new(path, getter, "value", GlyphLoweringStrategy.Value),
                        new Dictionary<string, BoundExpression>(),
                        syntax.Span);
                }

                return new BoundContext(_entry, pin, GlyphTypeSymbol.From(context.DataType), syntax.Span);
            }

        }

        Error("GLYPH3002", $"Unknown name or unavailable context '{path ?? "expression"}'.", syntax.Span);
        return new BoundError(syntax.Span);
    }

    private bool TryAggregateMember(MemberAccessExpressionSyntax syntax, out BoundExpression value)
    {
        value = null!;

        if (!TryAggregateReceiver(syntax.Receiver, out BoundExpression receiver))
            return false;

        switch (receiver)
        {
            case BoundStruct structure:
                if (structure.Fields.TryGetValue(syntax.Name, out BoundExpression? fieldValue))
                {
                    value = fieldValue;
                    return true;
                }

                Error("GLYPH3002", $"Struct '{structure.Definition.Name}' has no field '{syntax.Name}'.", syntax.Span);
                value = new BoundError(syntax.Span);
                return true;

            case BoundPlaceholder placeholder
                when _structs.TryGetValue(placeholder.Type.Name, out GlyphStructDefinition? definition):
            {
                GlyphFieldSymbol? field = definition.Fields.FirstOrDefault(f => f.Name == syntax.Name);

                if (field == null)
                {
                    Error("GLYPH3002", $"Struct '{definition.Name}' has no field '{syntax.Name}'.", syntax.Span);
                    value = new BoundError(syntax.Span);
                }
                else
                {
                    value = new BoundPlaceholder(field.Type, syntax.Span);
                }

                return true;
            }

            default:
                return false;
        }
    }

    private bool TryAggregateReceiver(ExpressionSyntax syntax, out BoundExpression value)
    {
        value = null!;

        switch (syntax)
        {
            case NameExpressionSyntax name:
                foreach (var scope in _scopes)
                {
                    if (scope.TryGetValue(name.Name, out BoundExpression? local))
                    {
                        value = local;
                        return true;
                    }
                }
                return false;

            case MemberAccessExpressionSyntax member:
                return TryAggregateMember(member, out value);

            case InvocationExpressionSyntax invocation:
            {
                string? path = Path(invocation.Function);
                if (!IsConstructorName(path)) return false;
                value = Call(invocation);
                return true;
            }

            default:
                return false;
        }
    }

    private BoundExpression Call(
        InvocationExpressionSyntax syntax,
        bool allowAction = false,
        bool allowPredicate = false)
    {
        string? name = Path(syntax.Function);

        if (name != null && TryBindConstructor(name, syntax, out BoundExpression constructor))
            return constructor;

        List<ArgumentSyntax> arguments = syntax.Arguments.ToList();

        var alias = GlyphLanguageAliases.Calls.FirstOrDefault(a => a.Name == name);
        if (alias != null)
        {
            name = alias.Target;
            if (alias.ImplicitArgument != null)
                arguments.Insert(0, new(null, new NameExpressionSyntax(alias.ImplicitArgument, syntax.Span), syntax.Span));
        }

        GlyphLanguageSymbol? symbol = name == null ? null : catalog.Find(name);

        if (symbol == null)
        {
            BoundExpression? receiverCall = TryResolveReceiverCall(syntax, arguments, allowAction, allowPredicate);
            if (receiverCall != null) return receiverCall;

            Error("GLYPH2002", $"Unknown function '{name}'.", syntax.Span);
            return new BoundError(syntax.Span);
        }

        CheckSymbolConstraints(symbol, name!, syntax.Span, allowAction, allowPredicate);

        List<LogicalArgument> logical = arguments
            .Select(a => new LogicalArgument(a.Name, Expression(a.Value), a.Span))
            .ToList();

        Dictionary<string, BoundExpression> bound = BindArguments(symbol, logical, syntax.Span);

        if (name == "set_status" &&
            bound.GetValueOrDefault("status") is BoundLiteral { Value: string status } &&
            status is not ("Active" or "Completed" or "Cancelled" or "Failed"))
            Error("GLYPH2004", "Status must be Active, Completed, Cancelled, or Failed.", syntax.Span);

        return new BoundCall(symbol, bound, syntax.Span);
    }

    private bool IsConstructorName(string? name)
    {
        if (name == null) return false;
        if (_structs.ContainsKey(name)) return true;

        int dot = name.IndexOf('.');
        if (dot <= 0 || dot == name.Length - 1) return false;

        string typeName = name[..dot];
        string variantName = name[(dot + 1)..];

        return _adts.TryGetValue(typeName, out GlyphAdtDefinition? adt) &&
               adt.Variants.Any(v => v.Name == variantName);
    }

    private bool TryBindConstructor(string name, InvocationExpressionSyntax syntax, out BoundExpression result)
    {
        if (_structs.TryGetValue(name, out GlyphStructDefinition? structure))
        {
            result = new BoundStruct(
                structure,
                BindAggregateArguments(structure.Fields, syntax.Arguments, syntax.Span),
                syntax.Span);
            return true;
        }

        int dot = name.IndexOf('.');
        if (dot <= 0 || dot == name.Length - 1)
        {
            result = null!;
            return false;
        }

        string typeName = name[..dot];
        string variantName = name[(dot + 1)..];

        if (!_adts.TryGetValue(typeName, out GlyphAdtDefinition? adt))
        {
            result = null!;
            return false;
        }

        GlyphVariantDefinition? variant = adt.Variants.FirstOrDefault(v => v.Name == variantName);

        if (variant == null)
        {
            Error("GLYPH2010", $"Unknown variant '{typeName}.{variantName}'.", syntax.Span);
            result = new BoundError(syntax.Span);
            return true;
        }

        result = new BoundVariant(
            adt,
            variant,
            BindAggregateArguments(variant.Fields, syntax.Arguments, syntax.Span),
            syntax.Span);
        return true;
    }

    private IReadOnlyDictionary<string, BoundExpression> BindAggregateArguments(
        IReadOnlyList<GlyphFieldSymbol> fields,
        IReadOnlyList<ArgumentSyntax> arguments,
        SourceSpan span)
    {
        Dictionary<string, BoundExpression> bound = new(StringComparer.Ordinal);
        int position = 0;
        bool namedSeen = false;

        foreach (ArgumentSyntax argument in arguments)
        {
            GlyphFieldSymbol? field;

            if (argument.Name != null)
            {
                namedSeen = true;
                field = fields.FirstOrDefault(f => f.Name == argument.Name);
            }
            else
            {
                if (namedSeen)
                    Error("GLYPH2003", "Positional arguments must precede named arguments.", argument.Span);

                field = position < fields.Count ? fields[position++] : null;
            }

            if (field == null || bound.ContainsKey(field.Name))
            {
                Error("GLYPH2003", "Unknown, excess, or duplicate aggregate field.", argument.Span);
                continue;
            }

            BoundExpression fieldValue = Expression(argument.Value);
            Require(fieldValue, field.Type);
            bound[field.Name] = fieldValue;
        }

        foreach (GlyphFieldSymbol field in fields)
        {
            if (!bound.ContainsKey(field.Name))
            {
                Error("GLYPH2003", $"Missing field '{field.Name}'.", span);
                bound[field.Name] = new BoundPlaceholder(field.Type, span);
            }
        }

        return bound;
    }

    private sealed record LogicalArgument(string? Name, BoundExpression Value, SourceSpan Span);

    private void CheckSymbolConstraints(
        GlyphLanguageSymbol symbol,
        string name,
        SourceSpan span,
        bool allowAction,
        bool allowPredicate)
    {
        if (symbol.Definition.RestrictToEventType is { } evt && evt != _event ||
            symbol.Definition.ScriptCategory is { } cat && cat != _event.GetCategory())
            Error("GLYPH3001", $"'{name}' is unavailable for {_event}.", span);

        if (symbol.AllowedStages != null && !symbol.AllowedStages.Contains(_stage))
            Error("GLYPH3003", $"'{name}' is unavailable in stage '{_stage}'.", span);

        if (name == "fail" && _loops > 0)
            Error("GLYPH3003", "fail inside foreach is not supported; fail before or after the loop.", span);

        if (symbol.Strategy == GlyphLoweringStrategy.Action && !allowAction ||
            symbol.Strategy == GlyphLoweringStrategy.PredicateBranch && !allowPredicate)
            Error("GLYPH2007", $"'{name}' requires {(symbol.Strategy == GlyphLoweringStrategy.Action ? "an action statement" : "a direct if condition")}.", span);
    }

    private Dictionary<string, BoundExpression> BindArguments(
        GlyphLanguageSymbol symbol,
        IReadOnlyList<LogicalArgument> arguments,
        SourceSpan span)
    {
        Dictionary<string, BoundExpression> bound = new();
        int position = 0;
        bool namedSeen = false;

        foreach (LogicalArgument arg in arguments)
        {
            GlyphPin? parameter;

            if (arg.Name != null)
            {
                namedSeen = true;
                parameter = symbol.Parameters.FirstOrDefault(p => p.Id == arg.Name);
            }
            else
            {
                if (namedSeen)
                    Error("GLYPH2003", "Positional arguments must precede named arguments.", arg.Span);
                parameter = position < symbol.Parameters.Count ? symbol.Parameters[position++] : null;
            }

            if (parameter == null || bound.ContainsKey(parameter.Id))
            {
                Error("GLYPH2003", "Unknown, excess, or duplicate argument.", arg.Span);
                continue;
            }

            Require(arg.Value, GlyphTypeSymbol.From(parameter.DataType));
            bound[parameter.Id] = arg.Value;
        }

        foreach (GlyphPin parameter in symbol.Parameters)
            if (!bound.ContainsKey(parameter.Id) && parameter.DefaultValue == null)
                Error("GLYPH2003", $"Missing argument '{parameter.Id}'.", span);

        return bound;
    }

    private BoundExpression? TryResolveReceiverCall(
        InvocationExpressionSyntax syntax,
        List<ArgumentSyntax> suppliedArguments,
        bool allowAction,
        bool allowPredicate)
    {
        if (syntax.Function is not MemberAccessExpressionSyntax member) return null;
        if (!catalog.TryResolveReceiverMethod(member.Name, out GlyphReceiverMethod receiverMethod)) return null;

        GlyphLanguageSymbol symbol = catalog.Find(receiverMethod.Target)
            ?? throw new InvalidOperationException(
                $"Registered receiver method '{member.Name}' has no target '{receiverMethod.Target}'.");

        BoundExpression boundReceiver = Expression(member.Receiver);

        if (boundReceiver.Type.RuntimeType != receiverMethod.ReceiverType)
            Require(boundReceiver, GlyphTypeSymbol.From(receiverMethod.ReceiverType));

        List<LogicalArgument> arguments = new(suppliedArguments.Count + 1)
        {
            new(null, boundReceiver, member.Receiver.Span)
        };

        arguments.AddRange(
            suppliedArguments.Select(a => new LogicalArgument(a.Name, Expression(a.Value), a.Span)));

        CheckSymbolConstraints(symbol, receiverMethod.Target, syntax.Span, allowAction, allowPredicate);

        return new BoundCall(
            symbol,
            BindArguments(symbol, arguments, syntax.Span),
            syntax.Span);
    }

    private void Require(BoundExpression value, GlyphTypeSymbol type)
    {
        if (value.Type == GlyphTypeSymbol.Error || type == GlyphTypeSymbol.Error) return;
        if (value.Type == type) return;

        if (value.Type.RuntimeType is { } from &&
            type.RuntimeType is { } to &&
            GlyphIrValidator.CanConnect(from, to))
            return;

        Error("GLYPH2004", $"Cannot convert {value.Type.Name} to {type.Name}.", value.Span);
    }

    private static string? Path(ExpressionSyntax syntax) => syntax switch
    {
        NameExpressionSyntax name => name.Name,
        MemberAccessExpressionSyntax member when Path(member.Receiver) is { } prefix =>
            prefix + "." + member.Name,
        _ => null
    };
}
