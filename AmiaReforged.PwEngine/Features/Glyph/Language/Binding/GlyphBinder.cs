using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public sealed class GlyphBinder(GlyphLanguageCatalog catalog, GlyphGlobalEnvironment? globals = null, GlyphModuleBinding? modules = null)
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];

    private GlyphModuleScope? _moduleScope = modules?.RootScope;
    private GlyphEventType _event;
    private string _entry = "", _stage = "";
    private int _loops, _nextSymbol, _bindingSteps;
    private readonly HashSet<string> _expandingFunctions = new(StringComparer.Ordinal);

    private readonly Stack<Dictionary<string, BoundExpression>> _scopes = new();
    private readonly Dictionary<string, GlyphTypeSymbol> _userTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphStructDefinition> _structs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphAdtDefinition> _adts = new(StringComparer.Ordinal);

    private void Error(string code, string message, SourceSpan span) =>
        Diagnostics.Add(new(code, message, span));

    public BoundProgram? Bind(GlyphCompilationUnitSyntax syntax)
    {
        DeclareTypes([.. globals?.Structs.Values.Cast<TypeDeclarationSyntax>() ?? [], .. globals?.Adts.Values.Cast<TypeDeclarationSyntax>() ?? [], .. modules == null ? syntax.Declarations : []]);

        if (!GlyphLanguageCatalog.Events.TryGetValue(syntax.Event, out _event))
        {
            Error("GLYPH3001", $"Unknown event '{syntax.Event}'.", syntax.Span);
            return null;
        }

        List<BoundStage> stages = [];

        if (Platform.GlyphEvents.Get(_event).Stages is { } eventStages)
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

                var stageDescriptor = eventStages.FirstOrDefault(s => s.Name == stage.Name);
                if (stageDescriptor == null)
                {
                    Error("GLYPH3003", $"Unknown stage '{stage.Name}' for '{syntax.Event}'.", stage.Span);
                    continue;
                }
                _stage = stage.Name;
                _entry = stageDescriptor.EntryTypeId;
                stages.Add(new(_entry, Block(stage.Body), stage.Span));
            }

            foreach (string name in eventStages.Select(s => s.Name))
                if (!seen.Contains(name))
                    stages.Add(new(Platform.GlyphEvents.Get(_event).Entry(name), new([], syntax.Span), syntax.Span));
        }
        else
        {
            _entry = GlyphLanguageCatalog.EntryType(_event);
            stages.Add(new(_entry, Block(syntax.Body), syntax.Span));
        }

        return new(syntax.Name, _event, stages);
    }

    public IReadOnlyDictionary<string, IReadOnlyList<GlyphAvailabilityDto>> ValidateModuleFunctions()
    {
        DeclareTypes([.. globals?.Structs.Values.Cast<TypeDeclarationSyntax>() ?? [], .. globals?.Adts.Values.Cast<TypeDeclarationSyntax>() ?? []]);
        Dictionary<string, IReadOnlyList<GlyphAvailabilityDto>> available = new(StringComparer.Ordinal);
        foreach (var function in globals?.Functions.Values ?? [])
        {
            List<GlyphAvailabilityDto> scopes = [];
            List<GlyphDiagnostic>? firstErrors = null;
            foreach (var descriptor in Platform.GlyphEvents.All)
            foreach (string? stage in descriptor.Stages == null ? new string?[] { null } : descriptor.Stages.Select(s => (string?)s.Name))
            {
                int before = Diagnostics.Count;
                _bindingSteps = 0;
                _event = descriptor.EventType; _stage = stage ?? ""; _entry = descriptor.Entry(stage);
                _moduleScope = modules?.FunctionOwners.GetValueOrDefault(function.Name) ?? modules?.RootScope;
                Dictionary<string, BoundExpression> parameters = new(StringComparer.Ordinal);
                foreach (var parameter in function.Parameters)
                {
                    if (!parameters.TryAdd(parameter.Name, new BoundVariableRead(_nextSymbol++, ResolveType(parameter.TypeName ?? "", parameter.Span, true), parameter.Span, Mutable: false)))
                        Error("GLYPH2006", $"Duplicate parameter '{parameter.Name}'.", parameter.Span);
                }
                _scopes.Clear(); _scopes.Push(parameters); _expandingFunctions.Add(function.Name);
                BoundExpression body = Expression(function.Body);
                Require(body, ResolveType(function.ReturnType, function.Span, true));
                _scopes.Clear(); _expandingFunctions.Remove(function.Name);
                if (Diagnostics.Count == before) scopes.Add(new(descriptor.Name, stage));
                else firstErrors ??= Diagnostics.Skip(before).ToList();
                Diagnostics.RemoveRange(before, Diagnostics.Count - before);
            }
            available[function.Name] = scopes.AsReadOnly();
            if (scopes.Count == 0 && firstErrors != null) Diagnostics.AddRange(firstErrors);
        }
        _moduleScope = modules?.RootScope;
        return available;
    }

    private void DeclareTypes(IReadOnlyList<TypeDeclarationSyntax> declarations)
    {
        foreach (TypeDeclarationSyntax declaration in declarations)
        {
            if (BuiltinType(declaration.Name) != null ||
                !_userTypes.TryAdd(declaration.Name, new(declaration.Name, GlyphDataType.Aggregate)))
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

            result.Add(new(field.Name, ResolveType(field.TypeName, field.Span, canonical: true)));
        }

        return result;
    }

    private string ResolveName(string name, SourceSpan span) => _moduleScope?.Resolve(name, span, Diagnostics) ?? name;

    private GlyphTypeSymbol ResolveType(string name, SourceSpan span, bool canonical = false)
    {
        if (!canonical) name = ResolveName(name, span);
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
        "Void" => GlyphTypeSymbol.Void,
        "Object" => GlyphTypeSymbol.Object,
        "Location" => GlyphTypeSymbol.Location,
        "Effect" => GlyphTypeSymbol.Effect,
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
                List<BoundLet> prefix = [];
                BoundExpression binding = CaptureImpure(value, prefix);
                if (!_scopes.Peek().TryAdd(let.Name, binding))
                    Error("GLYPH2006", $"Duplicate local '{let.Name}'.", let.Span);
                return prefix.Count == 0 ? null : new BoundBlock(prefix, let.Span);
            }

            case VarStatementSyntax variable:
            {
                BoundExpression value = Expression(variable.Value);
                if (value.Type == GlyphTypeSymbol.Void)
                    Error("GLYPH2004", "A variable initializer must produce a value.", variable.Span);
                int symbol = _nextSymbol++;
                if (!_scopes.Peek().TryAdd(variable.Name, new BoundVariableRead(symbol, value.Type, variable.Span)))
                    Error("GLYPH2006", $"Duplicate local '{variable.Name}'.", variable.Span);
                return new BoundVar(symbol, value, variable.Span);
            }

            case ContinueStatementSyntax cont:
                if (_loops == 0) Error("GLYPH3004", "continue requires an enclosing loop.", cont.Span);
                return new BoundContinue(cont.Span);

            case WhileStatementSyntax loop:
            {
                BoundExpression condition = Expression(loop.Condition);
                Require(condition, GlyphTypeSymbol.Bool);
                _loops++;
                BoundBlock body = Block(loop.Body);
                _loops--;
                return new BoundWhile(condition, body, loop.Span);
            }

            case ForRangeStatementSyntax loop:
            {
                BoundExpression start = IntegerRangeExpression(loop.Start), end = IntegerRangeExpression(loop.End);
                BoundExpression? step = loop.Step == null ? null : IntegerRangeExpression(loop.Step);
                RequireInteger(start); RequireInteger(end);
                if (step != null) RequireInteger(step);
                if (ConstantInteger(step) == 0)
                    Error("GLYPH2014", "A range step cannot be zero.", loop.Step!.Span);
                int symbol = _nextSymbol++;
                _scopes.Push(new(StringComparer.Ordinal) { [loop.Name] = new BoundLoopElement(symbol, loop.Span, GlyphTypeSymbol.Int) });
                _loops++;
                BoundBlock body = Block(loop.Body);
                _loops--;
                _scopes.Pop();
                return new BoundForRange(symbol, start, end, loop.Inclusive, step, body, loop.Span);
            }

            case BreakStatementSyntax br:
                if (_loops == 0)
                    Error("GLYPH3004", "break requires an enclosing loop.", br.Span);
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
                if (list.Type.RuntimeType != GlyphDataType.List || list.Type.ElementType == null)
                    Error("GLYPH2004", "for/foreach requires a typed list.", loop.Span);
                int symbol = _nextSymbol++;

                _scopes.Push(new(StringComparer.Ordinal)
                {
                    [loop.Name] = new BoundLoopElement(symbol, loop.Span, list.Type.ElementType)
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

    // Range syntax has an integer context. Preserve ordinary expression arithmetic's
    // existing Float semantics while retaining integral arithmetic for range cursors.
    private BoundExpression IntegerRangeExpression(ExpressionSyntax syntax) => Integral(Expression(syntax));
    private static BoundExpression Integral(BoundExpression value)
    {
        if (value is BoundUnary { Operator: "+" or "-" } unary)
        {
            var operand = Integral(unary.Operand);
            return unary with { Operand = operand, Type = operand.Type == GlyphTypeSymbol.Int ? GlyphTypeSymbol.Int : unary.Type };
        }
        if (value is BoundBinary { Operator: "+" or "-" or "*" or "%" } binary)
        {
            var left = Integral(binary.Left); var right = Integral(binary.Right);
            return binary with { Left = left, Right = right, Type = left.Type == GlyphTypeSymbol.Int && right.Type == GlyphTypeSymbol.Int ? GlyphTypeSymbol.Int : binary.Type };
        }
        return value;
    }

    private void RequireInteger(BoundExpression value)
    {
        if (value.Type != GlyphTypeSymbol.Int && value.Type != GlyphTypeSymbol.Error)
            Error("GLYPH2004", "Range bounds and step must be Int.", value.Span);
    }

    private static long? ConstantInteger(BoundExpression? value) => value switch
    {
        BoundLiteral { Value: int number } => number,
        BoundUnary { Operator: "-" } unary when ConstantInteger(unary.Operand) is { } n => -n,
        BoundUnary { Operator: "+" } unary => ConstantInteger(unary.Operand),
        BoundBinary binary when ConstantInteger(binary.Left) is { } l && ConstantInteger(binary.Right) is { } r =>
            binary.Operator switch { "+" => l + r, "-" => l - r, "*" => l * r, "%" when r != 0 => l % r, _ => null },
        _ => null
    };

    private BoundStatement Match(MatchStatementSyntax syntax)
    {
        BoundExpression value = Expression(syntax.Value);
        _adts.TryGetValue(value.Type.Name, out GlyphAdtDefinition? adt);
        if (adt == null && value.Type.RuntimeType is not (GlyphDataType.Int or GlyphDataType.Bool or GlyphDataType.String or GlyphDataType.NwObject))
            Error("GLYPH2010", $"match requires an ADT or scalar value, got {value.Type.Name}.", syntax.Value.Span);

        int symbol = _nextSymbol++;
        BoundExpression captured = new BoundVariableRead(symbol, value.Type, syntax.Value.Span, Mutable: false);
        HashSet<string> variants = new(StringComparer.Ordinal);
        HashSet<(GlyphTypeSymbol, object)> constants = [];
        bool wildcard = false;
        List<BoundMatchArm> arms = [];
        foreach (MatchArmSyntax arm in syntax.Arms)
        {
            Dictionary<string, BoundExpression> scope = new(StringComparer.Ordinal);
            BoundPattern pattern = new BoundWildcardPattern();
            if (wildcard)
                Error("GLYPH2010", "Wildcard must be the last match arm.", arm.Span);
            switch (arm.Pattern)
            {
                case WildcardPatternSyntax:
                    if (wildcard) Error("GLYPH2010", "Duplicate wildcard arm.", arm.Span);
                    wildcard = true;
                    break;
                case ValuePatternSyntax scalar:
                {
                    BoundExpression constant = Expression(scalar.Value);
                    if (constant is BoundUnary { Operator: "+" or "-", Operand: BoundLiteral { Value: int } } && ConstantInteger(constant) is { } integer && integer is >= int.MinValue and <= int.MaxValue)
                        constant = new BoundLiteral((int)integer, GlyphTypeSymbol.Int, scalar.Span);
                    if (adt != null)
                        Error("GLYPH2010", "ADT matches require variant patterns.", arm.Span);
                    if (constant is not BoundLiteral)
                        Error("GLYPH2010", "Value patterns must be literals or resolved constants.", scalar.Span);
                    if (constant.Type != value.Type && constant.Type != GlyphTypeSymbol.Error)
                        Error("GLYPH2004", $"Pattern type {constant.Type.Name} does not match {value.Type.Name}.", scalar.Span);
                    object? key = constant is BoundLiteral literal ? literal.Value : ConstantInteger(constant);
                    if (key != null && !constants.Add((constant.Type, key)))
                        Error("GLYPH2010", "Duplicate value match arm.", arm.Span);
                    pattern = new BoundValuePattern(constant);
                    break;
                }
                case VariantPatternSyntax variantPattern:
                {
                    string variantName = variantPattern.Variant;
                    int separator = variantName.LastIndexOf('.');
                    bool matchingType = separator < 0 || ResolveName(variantName[..separator], variantPattern.Span) == value.Type.Name;
                    if (separator >= 0) variantName = variantName[(separator + 1)..];
                    GlyphVariantDefinition? variant = matchingType ? adt?.Variants.FirstOrDefault(v => v.Name == variantName) : null;
                    if (variant == null)
                    {
                        Error("GLYPH2010", $"Unknown variant '{value.Type.Name}.{variantPattern.Variant}'.", arm.Span);
                        break;
                    }
                    if (!variants.Add(variant.Name)) Error("GLYPH2010", $"Duplicate match arm '{variant.Name}'.", arm.Span);
                    HashSet<string> bindings = new(StringComparer.Ordinal);
                    foreach (string binding in variantPattern.Bindings)
                    {
                        if (!bindings.Add(binding))
                        { Error("GLYPH2010", $"Duplicate pattern binding '{binding}'.", arm.Span); continue; }
                        GlyphFieldSymbol? field = variant.Fields.FirstOrDefault(f => f.Name == binding);
                        if (field == null)
                        { Error("GLYPH2010", $"Variant '{adt!.Name}.{variant.Name}' has no field '{binding}'.", arm.Span); continue; }
                        scope[binding] = new BoundAggregateField(captured, binding, field.Type, arm.Span);
                    }
                    pattern = new BoundVariantPattern(adt!.Name, variant.Name);
                    break;
                }
            }
            _scopes.Push(scope);
            BoundBlock body = Block(arm.Body);
            _scopes.Pop();
            arms.Add(new(pattern, body, arm.Span));
        }
        if (adt != null && !wildcard)
            foreach (GlyphVariantDefinition variant in adt.Variants)
                if (!variants.Contains(variant.Name))
                    Error("GLYPH2010", $"Non-exhaustive match on '{adt.Name}'; missing variant '{variant.Name}'.", syntax.Span);
        return new BoundMatch(symbol, value, arms, syntax.Span);
    }

    private BoundStatement? Assignment(AssignmentStatementSyntax assignment)
    {
        if (assignment.Target is NameExpressionSyntax localName)
        {
            BoundExpression? local = _scopes.Select(scope => scope.GetValueOrDefault(localName.Name)).FirstOrDefault(v => v != null);
            if (local != null)
            {
                if (local is not BoundVariableRead { Mutable: true } variable)
                { Error("GLYPH2008", "Only var locals can be assigned; let, loop and pattern bindings are immutable.", assignment.Span); return null; }
                ExpressionSyntax rhs = assignment.Operator == "=" ? assignment.Value :
                    new BinaryExpressionSyntax(assignment.Target, assignment.Operator[..1], assignment.Value, assignment.Span);
                BoundExpression assignedValue = Expression(rhs);
                Require(assignedValue, variable.Type);
                return new BoundVariableAssignment(variable.SymbolId, variable.Type, assignedValue, assignment.Span);
            }
        }
        string? name = Path(assignment.Target);
        string? setter = name == null ? null : catalog.Setters.GetValueOrDefault(name);

        List<ArgumentSyntax> args = [];
        ExpressionSyntax value = assignment.Value;

        if (assignment.Operator != "=")
            value = new BinaryExpressionSyntax(assignment.Target, assignment.Operator[..1], value, assignment.Span);

        if (assignment.Target is IndexExpressionSyntax
            {
                Receiver: NameExpressionSyntax indexerName
            } index && catalog.Indexers.FirstOrDefault(i => i.Name == indexerName.Name) is { } indexer)
        {
            setter = indexer.Setter;
            args.Add(new(null, index.Index, index.Span));
        }

        if (setter == null)
        {
            Error("GLYPH2008", "Assignment requires a var local or registered writable state; let bindings are immutable.", assignment.Span);
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
        if (++_bindingSteps > 65536)
        {
            if (_bindingSteps == 65537) Error("GLYPH1007", "Expanded binding exceeds 65536 expressions.", syntax.Span);
            return new BoundError(syntax.Span);
        }
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
                else if (binary.Operator is "==" or "!=" && left.Type == right.Type &&
                    left.Type.RuntimeType is GlyphDataType.String or GlyphDataType.Bool or GlyphDataType.NwObject)
                { }
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
                Receiver: NameExpressionSyntax indexerName
            } index when catalog.Indexers.FirstOrDefault(i => i.Name == indexerName.Name) is { } indexer:
                return Call(new(
                    new NameExpressionSyntax(indexer.Getter, index.Span),
                    [new(null, index.Index, index.Span)],
                    index.Span));
        }

        string? path = Path(syntax);

        if (path != null)
        {
            foreach (var scope in _scopes)
                if (scope.TryGetValue(path, out BoundExpression? local))
                    return local;

            if (globals?.GetResolvedConstant(ResolveName(path, syntax.Span)) is { } constant)
                return new BoundLiteral(constant.Value, constant.Kind switch
                {
                    GlyphConstantKind.Bool => GlyphTypeSymbol.Bool,
                    GlyphConstantKind.Int => GlyphTypeSymbol.Int,
                    GlyphConstantKind.Float => GlyphTypeSymbol.Float,
                    GlyphConstantKind.Object => GlyphTypeSymbol.Object,
                    _ => GlyphTypeSymbol.String
                }, syntax.Span);

            var property = catalog.PropertyAliases.FirstOrDefault(a => a.Name == path);
            if (property != null)
            {
                return Call(new(
                    new NameExpressionSyntax(property.Target, syntax.Span),
                    property.ImplicitArgument == null
                        ? []
                        : [new(null, new NameExpressionSyntax(property.ImplicitArgument, syntax.Span), syntax.Span)],
                    syntax.Span));
            }

            string pin = catalog.ContextPin(path, _entry);
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

        if (!_structs.TryGetValue(receiver.Type.Name, out GlyphStructDefinition? definition)) return false;
        GlyphFieldSymbol? field = definition.Fields.FirstOrDefault(f => f.Name == syntax.Name);
        if (field == null)
        {
            Error("GLYPH3002", $"Struct '{definition.Name}' has no field '{syntax.Name}'.", syntax.Span);
            value = new BoundError(syntax.Span);
        }
        else value = receiver is BoundStruct structure
            ? structure.Fields[syntax.Name]
            : new BoundAggregateField(receiver, syntax.Name, field.Type, syntax.Span);
        return true;
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
                if (!IsConstructorName(path) && globals?.GetFunction(_moduleScope?.Resolve(path ?? "", syntax.Span) ?? path ?? "") == null && catalog.Find(path ?? "")?.ReturnType.RuntimeType != GlyphDataType.Aggregate) return false;
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
        if (name != null) name = ResolveName(name, syntax.Span);

        if (name != null && globals?.GetFunction(name) is { } function)
            return BindGlobalFunction(function, syntax);

        if (name != null && TryBindConstructor(name, syntax, out BoundExpression constructor))
            return constructor;

        List<ArgumentSyntax> arguments = syntax.Arguments.ToList();

        var alias = catalog.CallAliases.FirstOrDefault(a => a.Name == name);
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

    private BoundExpression BindGlobalFunction(FunctionDeclarationSyntax function, InvocationExpressionSyntax invocation)
    {
        if (_expandingFunctions.Count >= 64 || _expandingFunctions.Contains(function.Name))
        {
            Error("GLYPH2012", $"Recursive global function '{function.Name}' is not supported.", invocation.Span);
            return new BoundError(invocation.Span);
        }
        var arguments = new Dictionary<string, BoundExpression>(StringComparer.Ordinal);
        List<BoundLet> prefix = [];
        int position = 0;
        bool named = false;
        foreach (var supplied in invocation.Arguments)
        {
            ParameterSyntax? parameter;
            if (supplied.Name != null) { named = true; parameter = function.Parameters.FirstOrDefault(p => p.Name == supplied.Name); }
            else
            {
                if (named) Error("GLYPH2003", "Positional arguments must precede named arguments.", supplied.Span);
                parameter = function.Parameters.ElementAtOrDefault(position++);
            }
            if (parameter == null || arguments.ContainsKey(parameter.Name))
            { Error("GLYPH2003", "Unknown, excess, or duplicate argument.", supplied.Span); continue; }
            BoundExpression value = Expression(supplied.Value);
            Require(value, ResolveType(parameter.TypeName ?? "", parameter.Span, canonical: true));
            value = CaptureImpure(value, prefix);
            arguments.Add(parameter.Name, value);
        }
        foreach (var parameter in function.Parameters)
            if (!arguments.ContainsKey(parameter.Name))
            {
                Error("GLYPH2003", $"Missing argument '{parameter.Name}'.", invocation.Span);
                arguments[parameter.Name] = new BoundError(invocation.Span);
            }
        // Global bodies see their parameters and globals, not caller-local variables.
        _expandingFunctions.Add(function.Name);
        var savedScopes = _scopes.ToArray();
        _scopes.Clear();
        _scopes.Push(arguments);
        var savedOwner = _moduleScope;
        _moduleScope = modules?.FunctionOwners.GetValueOrDefault(function.Name) ?? modules?.RootScope;
        int diagnosticStart = Diagnostics.Count;
        BoundExpression body = Expression(function.Body);
        Require(body, ResolveType(function.ReturnType, function.Span, canonical: true));
        if (modules != null && Diagnostics.Count > diagnosticStart)
            Error("GLYPH2028", $"While expanding '{function.Name}', called here.", invocation.Span);
        _moduleScope = savedOwner;
        _scopes.Clear();
        foreach (var scope in savedScopes.Reverse()) _scopes.Push(scope);
        _expandingFunctions.Remove(function.Name);
        return prefix.Count == 0 ? body : new BoundSequence(prefix, body, invocation.Span);
    }

    private bool IsConstructorName(string? name)
    {
        if (name == null) return false;
        name = _moduleScope?.Resolve(name, new("", 0, 0, 1, 1)) ?? name;
        if (_structs.ContainsKey(name)) return true;

        int dot = name.LastIndexOf('.');
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

        int dot = name.LastIndexOf('.');
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
            Error("GLYPH3003", "fail inside loops is not supported; fail before or after the loop.", span);

        if (symbol.Strategy == GlyphLoweringStrategy.Action && symbol.OutputPin == null && !allowAction ||
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

            Require(arg.Value, GlyphTypeSymbol.From(parameter));
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
        if (!catalog.HasReceiverMethod(member.Name)) return null;
        BoundExpression boundReceiver = Expression(member.Receiver);
        if (!catalog.TryResolveReceiverMethod(member.Name, boundReceiver.Type.RuntimeType ?? GlyphDataType.Exec, out GlyphReceiverMethod receiverMethod))
        {
            Error("GLYPH2004", $"No receiver method '{member.Name}' for {boundReceiver.Type.Name}.", member.Span);
            return new BoundError(member.Span);
        }

        GlyphLanguageSymbol symbol = catalog.Find(receiverMethod.Target)
            ?? throw new InvalidOperationException(
                $"Registered receiver method '{member.Name}' has no target '{receiverMethod.Target}'.");

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

    private BoundExpression CaptureImpure(BoundExpression value, List<BoundLet> prefix)
    {
        if (value is BoundSequence sequence)
        {
            prefix.AddRange(sequence.Prefix);
            return CaptureImpure(sequence.Value, prefix);
        }
        if (value is BoundStruct structure)
            return structure with { Fields = structure.Fields.ToDictionary(p => p.Key, p => CaptureImpure(p.Value, prefix)) };
        if (value is BoundVariant variant)
            return variant with { Fields = variant.Fields.ToDictionary(p => p.Key, p => CaptureImpure(p.Value, prefix)) };
        if (!HasSideEffects(value)) return value;
        int id = _nextSymbol++;
        prefix.Add(new(id, value, value.Span));
        return new BoundStoredValue(id, value.Type, value.Span);
    }

    internal static bool HasSideEffects(BoundExpression value) => value switch
    {
        BoundStruct structure => structure.Fields.Values.Any(HasSideEffects),
        BoundVariant variant => variant.Fields.Values.Any(HasSideEffects),
        BoundSequence sequence => sequence.Prefix.Count > 0 || HasSideEffects(sequence.Value),
        BoundCall call => call.Symbol.Strategy == GlyphLoweringStrategy.Action || call.Arguments.Values.Any(HasSideEffects),
        BoundUnary unary => HasSideEffects(unary.Operand),
        BoundBinary binary => HasSideEffects(binary.Left) || HasSideEffects(binary.Right),
        _ => false
    };

    private void Require(BoundExpression value, GlyphTypeSymbol type)
    {
        if (value.Type == GlyphTypeSymbol.Error || type == GlyphTypeSymbol.Error) return;
        if (value.Type == type) return;

        if (value.Type.RuntimeType is GlyphDataType.Aggregate or GlyphDataType.List || type.RuntimeType is GlyphDataType.Aggregate or GlyphDataType.List)
        {
            Error("GLYPH2004", $"Cannot convert {value.Type.Name} to {type.Name}.", value.Span);
            return;
        }
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
