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
    private GlyphTypeSymbol? _functionReturnType;
    private int _languageVersion = modules?.LanguageVersion ?? GlyphLanguageVersion.Current;
    private readonly HashSet<string> _expandingFunctions = new(StringComparer.Ordinal);

    private readonly Stack<Dictionary<string, BoundExpression>> _scopes = new();
    private Dictionary<string, GlyphTypeSymbol> _typeBindings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeDeclarationSyntax> _typeDeclarations = new(StringComparer.Ordinal);
    private int _typeDepth;
    private readonly Dictionary<string, GlyphTypeSymbol> _userTypes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphStructDefinition> _structs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphAdtDefinition> _adts = new(StringComparer.Ordinal);

    private void Error(string code, string message, SourceSpan span) =>
        Diagnostics.Add(new(code, message, span));

    public BoundProgram? Bind(GlyphCompilationUnitSyntax syntax)
    {
        _languageVersion = syntax.LanguageVersion;
        if (modules == null) GlyphModuleBinding.ValidateImplementations(syntax, Diagnostics);
        DeclareTypes([.. globals?.Structs.Values.Cast<TypeDeclarationSyntax>() ?? [], .. globals?.Adts.Values.Cast<TypeDeclarationSyntax>() ?? [], .. modules == null ? syntax.Declarations : []]);

        ValidateFunctions(syntax.GlobalDeclarations.OfType<FunctionDeclarationSyntax>().Where(f => f.Body is BlockFunctionBodySyntax || f.DeclaringType != null || f.TypeParameters.Count > 0));
        _bindingSteps = 0; _stage = "";

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

        return new(syntax.Name, _event, stages) { LanguageVersion = syntax.LanguageVersion };
    }

    public IReadOnlyDictionary<string, IReadOnlyList<GlyphAvailabilityDto>> ValidateModuleFunctions()
    {
        DeclareTypes([.. globals?.Structs.Values.Cast<TypeDeclarationSyntax>() ?? [], .. globals?.Adts.Values.Cast<TypeDeclarationSyntax>() ?? []]);
        return ValidateFunctions(globals?.Functions.Values ?? []);
    }

    private IReadOnlyDictionary<string, IReadOnlyList<GlyphAvailabilityDto>> ValidateFunctions(IEnumerable<FunctionDeclarationSyntax> functions)
    {
        Dictionary<string, IReadOnlyList<GlyphAvailabilityDto>> available = new(StringComparer.Ordinal);
        foreach (var function in functions)
        {
            ValidateTypeParameters(function);
            var savedBindings = _typeBindings;
            _typeBindings = TemplateBindings(function);
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
                    GlyphTypeSymbol parameterType = ResolveType(parameter.TypeName ?? "", parameter.Span, true);
                    if (parameterType == GlyphTypeSymbol.Void)
                        Error("GLYPH2004", "Function parameters must have a value type.", parameter.Span);
                    if (!parameters.TryAdd(parameter.Name, new BoundVariableRead(_nextSymbol++, parameterType, parameter.Span, Mutable: false)))
                        Error("GLYPH2006", $"Duplicate parameter '{parameter.Name}'.", parameter.Span);
                }
                _scopes.Clear(); _scopes.Push(parameters); _expandingFunctions.Add(function.Name);
                int savedLoops = _loops;
                _loops = 0;
                BindFunctionBody(function, []);
                _loops = savedLoops;
                _scopes.Clear(); _expandingFunctions.Remove(function.Name);
                if (Diagnostics.Count == before) scopes.Add(new(descriptor.Name, stage));
                else firstErrors ??= Diagnostics.Skip(before).ToList();
                Diagnostics.RemoveRange(before, Diagnostics.Count - before);
            }
            available[function.Name] = scopes.AsReadOnly();
            if (scopes.Count == 0 && firstErrors != null) Diagnostics.AddRange(firstErrors);
            _typeBindings = savedBindings;
        }
        _moduleScope = modules?.RootScope;
        return available;
    }

    private void ValidateTypeParameters(GlyphDeclarationSyntax declaration)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (string parameter in declaration.TypeParameters)
            if (!seen.Add(parameter) || BuiltinType(parameter) != null || parameter is "List" or "Dictionary" or "Self" or "Option" || _typeDeclarations.ContainsKey(parameter))
                Error("GLYPH2006", $"Duplicate or reserved type parameter '{parameter}'.", declaration.Span);
    }

    private Dictionary<string, GlyphTypeSymbol> TemplateBindings(GlyphDeclarationSyntax declaration) =>
        declaration.TypeParameters.Distinct().ToDictionary(p => p, p => new GlyphTypeSymbol("!" + declaration.Name + "." + p) { IsTypeParameter = true }, StringComparer.Ordinal);

    private void DeclareTypes(IReadOnlyList<TypeDeclarationSyntax> declarations)
    {
        if (_languageVersion >= 5) declarations = [GlyphBuiltins.Option, ..declarations];
        foreach (TypeDeclarationSyntax declaration in declarations)
        {
            if (_languageVersion >= 4 && declaration.Name is ("List" or "Dictionary" or "Self") || BuiltinType(declaration.Name) != null ||
                !_typeDeclarations.TryAdd(declaration.Name, declaration))
                Error("GLYPH2006", $"Duplicate or reserved type '{declaration.Name}'.", declaration.Span);
        }
        foreach (TypeDeclarationSyntax declaration in declarations) ValidateTypeParameters(declaration);

        foreach (var method in globals?.Functions.Values.Where(f => f.DeclaringType != null) ?? [])
        {
            var owner = declarations.FirstOrDefault(d => d.Name == GlyphTypeNames.Parse(method.DeclaringType!).Name);
            if (owner == null || owner.Span.SourceId != method.Span.SourceId)
                Error("GLYPH2031", "impl requires a struct or ADT declared in the same source module.", method.Span);
            string member = method.Name[(method.Name.LastIndexOf('.') + 1)..];
            if (owner is AdtDeclarationSyntax adt && adt.Variants.Any(v => v.Name == member) ||
                owner is StructDeclarationSyntax structure && structure.Fields.Any(f => f.Name == member))
                Error("GLYPH2006", $"Member '{method.Name}' collides with a field or variant.", method.Span);
            if (method.Parameters.Any(p => p.Name == "self") && (!method.IsInstance || method.Parameters[0].TypeName != method.DeclaringType))
                Error("GLYPH2004", "self must be the first parameter and have the impl type.", method.Span);
        }
        foreach (var declaration in declarations)
        {
            var bindings = TemplateBindings(declaration);
            MaterializeType(declaration, declaration.TypeParameters.Select(p => bindings[p]).ToArray(), declaration.Span);
        }
    }

    private GlyphTypeSymbol MaterializeType(TypeDeclarationSyntax declaration, IReadOnlyList<GlyphTypeSymbol> arguments, SourceSpan span)
    {
        string name = GlyphTypeNames.Apply(declaration.Name, arguments.Select(t => t.Name));
        if (_userTypes.TryGetValue(name, out var existing)) return existing;
        if (_typeDepth >= 64 || _userTypes.Count >= 1024)
        {
            Error("GLYPH1007", "Generic type expansion exceeds depth 64 or 1024 specializations.", span);
            return GlyphTypeSymbol.Error;
        }
        GlyphTypeSymbol type = new(name, GlyphDataType.Aggregate);
        _userTypes[name] = type;
        var savedBindings = _typeBindings;
        _typeBindings = declaration.TypeParameters.Distinct().Zip(arguments).ToDictionary(p => p.First, p => p.Second, StringComparer.Ordinal);
        _typeDepth++;
        switch (declaration)
        {
            case StructDeclarationSyntax structure:
                _structs[name] = new(name, type, BindFields(structure.Fields));
                break;
            case AdtDeclarationSyntax adt:
                HashSet<string> seen = new(StringComparer.Ordinal);
                List<GlyphVariantDefinition> variants = [];
                foreach (var variant in adt.Variants)
                {
                    if (!seen.Add(variant.Name))
                        Error("GLYPH2006", $"Duplicate variant '{adt.Name}.{variant.Name}'.", variant.Span);
                    else variants.Add(new(variant.Name, BindFields(variant.Fields)));
                }
                if (variants.Count == 0) Error("GLYPH2010", $"ADT '{adt.Name}' must declare at least one variant.", adt.Span);
                _adts[name] = new(name, type, variants);
                break;
        }
        _typeDepth--;
        _typeBindings = savedBindings;
        return type;
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
        if (_typeBindings.Values.FirstOrDefault(t => t.Name == name) is { } symbolic) return symbolic;
        var reference = GlyphTypeNames.Parse(name);
        if (reference.Arguments.Count == 0 && _typeBindings.TryGetValue(name, out var parameter)) return parameter;
        string baseName = canonical ? reference.Name : ResolveName(reference.Name, span);
        var arguments = reference.Arguments.Select(a => ResolveType(a, span, canonical)).ToArray();
        if (arguments.Any(t => t == GlyphTypeSymbol.Error)) return GlyphTypeSymbol.Error;
        if (baseName is "List" or "Dictionary" && arguments.Length > 0)
        {
            bool list = baseName == "List";
            if (arguments.Length == (list ? 1 : 2) && arguments.All(t => t.IsBasic || t.IsTypeParameter))
                return list ? GlyphTypeSymbol.List(arguments[0]) : GlyphTypeSymbol.Dictionary(arguments[0], arguments[1]);
            Error("GLYPH2004", "Collections require basic Object, String, Int, Float, or Bool type arguments.", span);
            return GlyphTypeSymbol.Error;
        }
        GlyphTypeSymbol? builtin = BuiltinType(baseName);
        if (builtin != null && arguments.Length == 0) return builtin;
        if (_typeDeclarations.TryGetValue(baseName, out var declaration))
        {
            if (arguments.Length != declaration.TypeParameters.Count)
            {
                Error("GLYPH2004", $"Type '{baseName}' requires {declaration.TypeParameters.Count} type arguments, got {arguments.Length}.", span);
                return GlyphTypeSymbol.Error;
            }
            if (arguments.Any(t => t == GlyphTypeSymbol.Void))
            {
                Error("GLYPH2004", "Generic arguments must have a value type.", span);
                return GlyphTypeSymbol.Error;
            }
            return MaterializeType(declaration, arguments, span);
        }
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

            case ReturnStatementSyntax ret:
            {
                BoundExpression? value = ret.Value == null ? null : Expression(ret.Value);
                if (_functionReturnType == null)
                    Error("GLYPH2029", "return requires an enclosing function.", ret.Span);
                else if (_functionReturnType == GlyphTypeSymbol.Void)
                {
                    if (value != null) Error("GLYPH2004", "Void functions cannot return a value.", ret.Span);
                }
                else if (value == null)
                    Error("GLYPH2004", "This function must return a value.", ret.Span);
                else Require(value, _functionReturnType);
                return new BoundReturn(value, ret.Span);
            }

            case LetStatementSyntax let:
            {
                BoundExpression value = Expression(let.Value);
                if (value.Type == GlyphTypeSymbol.Void)
                    Error("GLYPH2004", "A binding initializer must produce a value.", let.Span);
                if (_languageVersion >= 4 && value.Type.IsCollection)
                {
                    int symbol = _nextSymbol++;
                    if (!_scopes.Peek().TryAdd(let.Name, new BoundVariableRead(symbol, value.Type, let.Span, Mutable: false)))
                        Error("GLYPH2006", $"Duplicate local '{let.Name}'.", let.Span);
                    return new BoundVar(symbol, value, let.Span);
                }
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
                    if (call is BoundFunctionCall { Type: var resultType } && resultType == GlyphTypeSymbol.Void)
                        return new BoundFunctionStatement(call, expr.Span);
                    if (call is BoundSequence { Type: var sequenceType } && sequenceType == GlyphTypeSymbol.Void)
                        return new BoundFunctionStatement(call, expr.Span);
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
                    bool matchingType = separator < 0 || ResolveType(variantName[..separator], variantPattern.Span) == value.Type;
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
            case CollectionConstructorSyntax constructor:
            {
                var type = ResolveType(constructor.TypeName, constructor.Span, canonical: true);
                return type == GlyphTypeSymbol.Error ? new BoundError(constructor.Span) : CollectionCall("new", type, [], constructor.Span);
            }
            case ListExpressionSyntax list:
            {
                var values = list.Values.Select(v => Expression(v)).ToArray();
                if (values.Length == 0 || !(values[0].Type.IsBasic || values[0].Type.IsTypeParameter))
                { Error("GLYPH2004", "Use List<T>() for an empty list; list elements must have basic types.", list.Span); return new BoundError(list.Span); }
                foreach (var value in values) Require(value, values[0].Type);
                return new BoundListLiteral(values, GlyphTypeSymbol.List(values[0].Type), list.Span);
            }
            case IndexExpressionSyntax index when _languageVersion >= 4 && !(index.Receiver is NameExpressionSyntax state && catalog.Indexers.Any(i => i.Name == state.Name)):
            {
                BoundExpression receiver = Expression(index.Receiver);
                return receiver.Type.IsCollection ? CollectionCall("index", receiver.Type, [new(null, receiver, index.Receiver.Span), new(null, Expression(index.Index), index.Index.Span)], index.Span)
                    : CollectionError("Indexing requires a collection or registered state.", index.Span);
            }
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
                value = Call(invocation);
                return value.Type.RuntimeType == GlyphDataType.Aggregate;
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
        IReadOnlyList<string> typeArguments = [];
        if (syntax.Function is TypeApplicationExpressionSyntax application)
        {
            typeArguments = application.Arguments;
            name = Path(application.Target);
        }
        if (name != null)
        {
            int open = name.IndexOf('<');
            if (open < 0) name = ResolveName(name, syntax.Span);
            else name = ResolveName(name[..open], syntax.Span) + name[open..];
        }

        if (name != null && globals?.GetFunction(name) is { } function)
        {
            if (function.DeclaringType != null && _moduleScope != null && !_moduleScope.CanAccessDeclaration(name))
            { Error("GLYPH2021", $"Method '{name}' is private or not imported.", syntax.Span); return new BoundError(syntax.Span); }
            return BindGlobalFunction(function, syntax, typeArguments: typeArguments);
        }

        if (name != null && name.Contains('<') && !name.EndsWith('>'))
        {
            int close = name.LastIndexOf('>');
            string ownerName = name[..(close + 1)];
            string methodName = GlyphTypeNames.Parse(ownerName).Name + name[(close + 1)..];
            if (globals?.GetFunction(methodName) is { } method)
            {
                if (_moduleScope != null && !_moduleScope.CanAccessDeclaration(methodName))
                { Error("GLYPH2021", $"Method '{methodName}' is private or not imported.", syntax.Span); return new BoundError(syntax.Span); }
                var owner = ResolveType(ownerName, syntax.Span);
                return BindGlobalFunction(method, syntax, typeArguments: typeArguments, ownerType: owner);
            }
        }
        if (name != null && typeArguments.Count > 0) name = GlyphTypeNames.Apply(name, typeArguments);
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

    private void InferTypeArguments(string pattern, GlyphTypeSymbol actual, FunctionDeclarationSyntax function,
        Dictionary<string, GlyphTypeSymbol> bindings, SourceSpan span)
    {
        if (function.TypeParameters.Contains(pattern))
        {
            if (bindings.TryGetValue(pattern, out var existing) && existing != actual)
                Error("GLYPH2004", $"Conflicting types for '{pattern}': {existing.Name} and {actual.Name}.", span);
            else bindings[pattern] = actual;
            return;
        }
        var expected = GlyphTypeNames.Parse(pattern);
        var provided = GlyphTypeNames.Parse(actual.Name);
        if (expected.Name != provided.Name || expected.Arguments.Count != provided.Arguments.Count) return;
        for (int i = 0; i < expected.Arguments.Count; i++)
        {
            GlyphTypeSymbol argument = actual.ElementType ?? GlyphTypeSymbol.Error;
            if (actual.RuntimeType == GlyphDataType.Dictionary) argument = i == 0 ? actual.KeyType! : actual.ValueType!;
            else if (actual.RuntimeType != GlyphDataType.List)
                argument = ResolveType(provided.Arguments[i], span, canonical: true);
            InferTypeArguments(expected.Arguments[i], argument, function, bindings, span);
        }
    }

    private BoundExpression BindGlobalFunction(FunctionDeclarationSyntax function, InvocationExpressionSyntax invocation,
        BoundExpression? receiver = null, IReadOnlyList<string>? typeArguments = null, GlyphTypeSymbol? ownerType = null)
    {
        if (_expandingFunctions.Count >= 64 || _expandingFunctions.Contains(function.Name))
        {
            Error("GLYPH2012", $"Recursive global function '{function.Name}' is not supported.", invocation.Span);
            return new BoundError(invocation.Span);
        }
        var bindings = new Dictionary<string, GlyphTypeSymbol>(StringComparer.Ordinal);
        if (function.DeclaringType != null && (ownerType ?? receiver?.Type) is { } owner)
            InferTypeArguments(function.DeclaringType, owner, function, bindings, invocation.Span);
        var explicitParameters = function.TypeParameters.Where(p => !bindings.ContainsKey(p)).ToArray();
        if (typeArguments is { Count: > 0 })
        {
            if (typeArguments.Count != explicitParameters.Length)
                Error("GLYPH2004", $"Function '{function.Name}' requires {explicitParameters.Length} explicit type arguments, got {typeArguments.Count}.", invocation.Span);
            for (int i = 0; i < Math.Min(typeArguments.Count, explicitParameters.Length); i++)
                bindings[explicitParameters[i]] = ResolveType(typeArguments[i], invocation.Span);
        }
        List<(ParameterSyntax Parameter, BoundExpression Value, SourceSpan Span)> suppliedValues = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
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
            if (parameter == null || !seen.Add(parameter.Name))
            { Error("GLYPH2003", "Unknown, excess, or duplicate argument.", supplied.Span); continue; }
            BoundExpression value = receiver != null && parameter.Name == "self" ? receiver : Expression(supplied.Value);
            if (typeArguments is not { Count: > 0 }) InferTypeArguments(parameter.TypeName ?? "", value.Type, function, bindings, supplied.Span);
            suppliedValues.Add((parameter, value, supplied.Span));
        }
        foreach (string parameter in function.TypeParameters)
            if (!bindings.ContainsKey(parameter))
            {
                Error("GLYPH2004", $"Cannot infer '{parameter}' for '{function.Name}'; supply explicit type arguments.", invocation.Span);
                bindings[parameter] = GlyphTypeSymbol.Error;
            }
        if (bindings.Values.Any(t => t == GlyphTypeSymbol.Void))
            Error("GLYPH2004", "Generic arguments must have a value type.", invocation.Span);
        var savedBindings = _typeBindings;
        _typeBindings = bindings;
        var arguments = new Dictionary<string, BoundExpression>(StringComparer.Ordinal);
        List<BoundLet> prefix = [];
        List<BoundVariableAssignment> parameterValues = [];
        foreach (var supplied in suppliedValues)
        {
            var parameterType = ResolveType(supplied.Parameter.TypeName ?? "", supplied.Parameter.Span, canonical: true);
            if (parameterType == GlyphTypeSymbol.Void)
                Error("GLYPH2004", "Function parameters must have a value type.", supplied.Parameter.Span);
            Require(supplied.Value, parameterType);
            BoundExpression value = supplied.Value;
            if (function.Body is BlockFunctionBodySyntax || function.DeclaringType != null || function.TypeParameters.Count > 0)
            {
                int symbol = _nextSymbol++;
                parameterValues.Add(new(symbol, parameterType, value, supplied.Span));
                value = new BoundVariableRead(symbol, parameterType, supplied.Span, Mutable: false);
            }
            else value = CaptureImpure(value, prefix);
            arguments.Add(supplied.Parameter.Name, value);
        }
        foreach (var parameter in function.Parameters)
            if (!arguments.ContainsKey(parameter.Name))
            {
                Error("GLYPH2003", $"Missing argument '{parameter.Name}'.", invocation.Span);
                arguments[parameter.Name] = new BoundError(invocation.Span);
            }
        _expandingFunctions.Add(function.Name);
        var savedScopes = _scopes.ToArray();
        _scopes.Clear();
        _scopes.Push(arguments);
        var savedOwner = _moduleScope;
        _moduleScope = modules?.FunctionOwners.GetValueOrDefault(function.Name) ?? modules?.RootScope;
        int diagnosticStart = Diagnostics.Count;
        int savedLoops = _loops;
        _loops = 0;
        BoundExpression body = BindFunctionBody(function, parameterValues);
        _loops = savedLoops;
        if (modules != null && Diagnostics.Count > diagnosticStart)
            Error("GLYPH2028", $"While expanding '{function.Name}', called here.", invocation.Span);
        _moduleScope = savedOwner;
        _typeBindings = savedBindings;
        _scopes.Clear();
        foreach (var scope in savedScopes.Reverse()) _scopes.Push(scope);
        _expandingFunctions.Remove(function.Name);
        return prefix.Count == 0 ? body : new BoundSequence(prefix, body, invocation.Span);
    }

    private BoundExpression BindFunctionBody(FunctionDeclarationSyntax function, IReadOnlyList<BoundVariableAssignment> arguments)
    {
        int savedVersion = _languageVersion;
        _languageVersion = function.LanguageVersion;
        GlyphTypeSymbol returnType = ResolveType(function.ReturnType, function.Span, canonical: true);
        if (function.Body is ExpressionFunctionBodySyntax expression)
        {
            BoundExpression value = Expression(expression.Expression);
            Require(value, returnType);
            _languageVersion = savedVersion;
            return arguments.Count == 0 ? value : new BoundFunctionCall(arguments, new([new BoundReturn(value, expression.Expression.Span)], function.Span),
                returnType == GlyphTypeSymbol.Void ? null : _nextSymbol++, returnType, function.Span);
        }
        var block = (BlockFunctionBodySyntax)function.Body;
        GlyphTypeSymbol? savedReturnType = _functionReturnType;
        _functionReturnType = returnType;
        BoundBlock body = Block(block.Block);
        _functionReturnType = savedReturnType;
        if (returnType != GlyphTypeSymbol.Void && !Terminates(body))
            Error("GLYPH2030", $"Function '{function.Name}' must return a value on every path.", function.Span);
        _languageVersion = savedVersion;
        return new BoundFunctionCall(arguments, body, returnType == GlyphTypeSymbol.Void ? null : _nextSymbol++, returnType, function.Span);
    }

    private static bool Terminates(BoundStatement statement) => statement switch
    {
        BoundReturn => true,
        BoundBlock block => block.Statements.Any(Terminates),
        BoundIf branch => branch.Else != null && Terminates(branch.Then) && Terminates(branch.Else),
        BoundMatch match => (match.Value.Type.RuntimeType == GlyphDataType.Aggregate || match.Arms.Any(a => a.Pattern is BoundWildcardPattern))
            && match.Arms.All(a => Terminates(a.Body)),
        BoundExpressionStatement action => !action.Call.Symbol.Definition.OutputPins.Any(p => p.Id == "exec_out"),
        _ => false
    };

    private bool TryBindConstructor(string name, InvocationExpressionSyntax syntax, out BoundExpression result)
    {
        string candidate = name;
        int open = name.IndexOf('<');
        if (open >= 0 && !name.EndsWith('>')) candidate = name[..(name.LastIndexOf('>') + 1)];
        if (_typeDeclarations.ContainsKey(GlyphTypeNames.Parse(candidate).Name))
        {
            var type = ResolveType(candidate, syntax.Span);
            if (type == GlyphTypeSymbol.Error) { result = new BoundError(syntax.Span); return true; }
            name = type.Name + name[candidate.Length..];
        }
        else if (open < 0 && name.Contains('.'))
        {
            candidate = name[..name.LastIndexOf('.')];
            if (_typeDeclarations.TryGetValue(candidate, out var declaration) && declaration.TypeParameters.Count > 0)
            {
                Error("GLYPH2004", $"Type '{candidate}' requires explicit type arguments.", syntax.Span);
                result = new BoundError(syntax.Span); return true;
            }
        }
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
        var application = syntax.Function as TypeApplicationExpressionSyntax;
        if ((application?.Target ?? syntax.Function) is not MemberAccessExpressionSyntax member) return null;
        bool authored = globals?.Functions.Values.Any(f => f.DeclaringType != null && f.Name.EndsWith("." + member.Name, StringComparison.Ordinal)) == true;
        bool collection = _languageVersion >= 4 && member.Name is "count" or "contains" or "append" or "with" or "remove_at" or "contains_key" or "without" or "get" or "keys" or "values";
        if (!catalog.HasReceiverMethod(member.Name) && !authored && !collection) return null;
        BoundExpression boundReceiver = Expression(member.Receiver);
        if (boundReceiver.Type.IsCollection && collection)
            return CollectionCall(member.Name, boundReceiver.Type,
                [new(null, boundReceiver, member.Receiver.Span), ..suppliedArguments.Select(a => new LogicalArgument(a.Name, Expression(a.Value), a.Span))], syntax.Span);
        string methodName = GlyphTypeNames.Parse(boundReceiver.Type.Name).Name + "." + member.Name;
        if (globals?.GetFunction(methodName) is { IsInstance: true } method)
        {
            if (_moduleScope != null && !_moduleScope.CanAccessDeclaration(methodName))
            { Error("GLYPH2021", $"Method '{methodName}' is private or not imported.", member.Span); return new BoundError(member.Span); }
            return BindGlobalFunction(method, syntax with { Arguments = [new(null, member.Receiver, member.Receiver.Span), ..suppliedArguments] }, boundReceiver, application?.Arguments);
        }
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

    private BoundExpression CollectionError(string message, SourceSpan span)
    { Error("GLYPH2004", message, span); return new BoundError(span); }

    private BoundExpression CollectionCall(string operation, GlyphTypeSymbol type, IReadOnlyList<LogicalArgument> arguments, SourceSpan span)
    {
        if ((type.ElementType ?? type.ValueType)?.IsTypeParameter == true || type.KeyType?.IsTypeParameter == true)
        {
            bool list = type.RuntimeType == GlyphDataType.List;
            var templateDefinition = catalog.Registry.Get(Runtime.Nodes.Flow.CollectionExecutor.Id(operation, type.RuntimeType!.Value, GlyphDataType.Int, list ? null : GlyphDataType.Int));
            if (templateDefinition == null) return CollectionError($"{type.Name} has no operation '{operation}'.", span);
            GlyphTypeSymbol PinType(string pin) => pin switch
            {
                "collection" => type,
                "index" => GlyphTypeSymbol.Int,
                "key" => type.KeyType!,
                _ => (type.ElementType ?? type.ValueType)!
            };
            HashSet<string> seen = new(StringComparer.Ordinal);
            int templatePosition = 0;
            bool named = false;
            foreach (var argument in arguments)
            {
                if (argument.Name != null) named = true;
                else if (named) Error("GLYPH2003", "Positional arguments must precede named arguments.", argument.Span);
                string? pin = argument.Name ?? templateDefinition.InputPins.ElementAtOrDefault(templatePosition++)?.Id;
                if (pin == null || !templateDefinition.InputPins.Any(p => p.Id == pin) || !seen.Add(pin))
                    Error("GLYPH2003", "Unknown, excess, or duplicate argument.", argument.Span);
                else Require(argument.Value, PinType(pin));
            }
            foreach (var pin in templateDefinition.InputPins)
                if (!seen.Contains(pin.Id)) Error("GLYPH2003", $"Missing argument '{pin.Id}'.", span);
            var result = operation switch
            {
                "count" => GlyphTypeSymbol.Int,
                "contains" or "contains_key" => GlyphTypeSymbol.Bool,
                "index" or "get" => (type.ElementType ?? type.ValueType)!,
                "keys" or "values" => GlyphTypeSymbol.List(operation == "keys" ? type.KeyType! : type.ValueType!),
                _ => type
            };
            return new BoundPlaceholder(result, span);
        }
        if (type.RuntimeType == GlyphDataType.List && type.ElementType?.IsBasic != true)
            return CollectionError("Collection operations support only basic element types.", span);
        string id = Runtime.Nodes.Flow.CollectionExecutor.Id(operation, type.RuntimeType!.Value,
            (type.ElementType ?? type.ValueType)!.RuntimeType!.Value, type.KeyType?.RuntimeType);
        var definition = catalog.Registry.Get(id);
        if (definition == null) return CollectionError($"{type.Name} has no operation '{operation}'.", span);
        var symbol = new GlyphLanguageSymbol(operation, definition, "value", GlyphLoweringStrategy.Value);
        int before = Diagnostics.Count;
        var bound = BindArguments(symbol, arguments, span);
        if (Diagnostics.Count != before) return new BoundError(span);
        // Capture every input in source order before evaluating the operation.
        List<BoundVariableAssignment> captures = [];
        Dictionary<string, BoundExpression> reads = new(StringComparer.Ordinal);
        int position = 0;
        foreach (var argument in arguments)
        {
            string? pin = argument.Name ?? symbol.Parameters.ElementAtOrDefault(position++)?.Id;
            if (pin == null || reads.ContainsKey(pin)) continue;
            int slot = _nextSymbol++;
            var targetType = GlyphTypeSymbol.From(symbol.Parameters.First(p => p.Id == pin));
            captures.Add(new(slot, targetType, argument.Value, argument.Span));
            reads[pin] = new BoundVariableRead(slot, targetType, argument.Span, Mutable: false);
        }
        if (arguments.Count == 0) return new BoundCall(symbol, bound, span);
        return new BoundFunctionCall(captures, new([new BoundReturn(new BoundCall(symbol, reads, span), span)], span), _nextSymbol++, symbol.ReturnType, span);
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

    private bool HasSideEffects(BoundExpression value) => value switch
    {
        BoundFunctionCall or BoundListLiteral => true,
        BoundAggregateField field => _languageVersion >= 4 && HasSideEffects(field.Receiver),
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

        if (value.Type.RuntimeType is GlyphDataType.Aggregate or GlyphDataType.List or GlyphDataType.Dictionary || type.RuntimeType is GlyphDataType.Aggregate or GlyphDataType.List or GlyphDataType.Dictionary)
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
        TypeApplicationExpressionSyntax application when Path(application.Target) is { } target => GlyphTypeNames.Apply(target, application.Arguments),
        _ => null
    };
}
