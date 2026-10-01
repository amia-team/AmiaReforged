using System.Globalization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Lowering;

/// <summary>Emits registered runtime operations. Syntax never reaches the interpreter.</summary>
public sealed class GlyphLowerer
{
    public sealed class LimitExceededException : Exception;
    private readonly GlyphGraph _ir = new() { Id = Guid.Empty };
    private readonly Dictionary<Guid, SourceSpan> _sourceMap = [];
    private readonly Dictionary<string, GlyphNodeInstance> _entries = [];
    private readonly Dictionary<int, GlyphNodeInstance> _loops = [];
    private readonly Dictionary<int, Output> _stored = [];
    private int _identity, _temporary = -1, _languageVersion;
    private readonly Stack<(int? Symbol, GlyphTypeSymbol Type)> _returns = new();
    private readonly record struct Output(GlyphNodeInstance Node, string Pin);
    public (GlyphGraph Ir, IReadOnlyDictionary<Guid, SourceSpan> SourceMap) Lower(BoundProgram program)
    {
        _languageVersion = program.LanguageVersion;
        _ir.Name = program.Name; _ir.EventType = program.Event;
        foreach (BoundStage stage in program.Stages) _entries[stage.EntryTypeId] = Node(stage.EntryTypeId, stage.Span);
        foreach (BoundStage stage in program.Stages) Block(stage.Body, [new(_entries[stage.EntryTypeId], "exec_out")]);
        return (_ir, _sourceMap);
    }
    private Guid Identity()
    {
        byte[] bytes = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(bytes, ++_identity);
        return new Guid(bytes);
    }
    private GlyphNodeInstance Node(string type, SourceSpan span, Dictionary<string, string>? properties = null)
    {
        if (_ir.Nodes.Count >= 4096) throw new LimitExceededException();
        GlyphNodeInstance node = new() { InstanceId = Identity(), TypeId = type, PropertyOverrides = properties ?? [] };
        _ir.Nodes.Add(node); _sourceMap[node.InstanceId] = span;
        return node;
    }
    private void Wire(Output from, GlyphNodeInstance to, string pin) => _ir.Edges.Add(new()
    { Id = Identity(), SourceNodeId = from.Node.InstanceId, SourcePinId = from.Pin, TargetNodeId = to.InstanceId, TargetPinId = pin });
    private void Connect(IEnumerable<Output> from, GlyphNodeInstance to) { foreach (Output output in from) Wire(output, to, "exec_in"); }
    private List<Output> Block(BoundBlock block, List<Output> tails)
    {
        foreach (BoundStatement statement in block.Statements)
        {
            if (tails.Count == 0) break;
            tails = Statement(statement, tails);
        }
        return tails;
    }
    private List<Output> Statement(BoundStatement statement, List<Output> tails)
    {
        switch (statement)
        {
            case BoundBlock block: return Block(block, tails);
            case BoundFunctionStatement invocation:
                Expression(invocation.Call, ref tails);
                return tails;
            case BoundReturn ret:
            {
                var target = _returns.Peek();
                if (ret.Value != null && target.Symbol is { } symbol)
                    tails = Write(symbol, target.Type, ret.Value, ret.Span, tails);
                Connect(tails, Node("flow.return", ret.Span));
                return [];
            }
            case BoundLet let:
                _stored[let.SymbolId] = Expression(let.Value, ref tails);
                return tails;
            case BoundVar variable:
                return Write(variable.SymbolId, variable.Value.Type, variable.Value, variable.Span, tails);
            case BoundVariableAssignment assignment:
                return Write(assignment.SymbolId, assignment.ValueType, assignment.Value, assignment.Span, tails);
            case BoundContinue cont:
                Connect(tails, Node("flow.continue", cont.Span)); return [];
            case BoundWhile loop:
            {
                GlyphNodeInstance owner = Node("flow.while", loop.Span);
                Connect(tails, owner);
                List<Output> conditionTails = [new(owner, "loop_body")];
                Output condition = Expression(loop.Condition, ref conditionTails);
                GlyphNodeInstance test = Node("flow.branch", loop.Condition.Span);
                Wire(condition, test, "condition"); Connect(conditionTails, test);
                Block(loop.Body, [new(test, "true")]);
                Connect([new(test, "false")], Node("flow.break", loop.Condition.Span));
                return [new(owner, "completed")];
            }
            case BoundForRange loop:
            {
                GlyphNodeInstance range = Node("flow.for_range", loop.Span, new() { ["auto_step"] = loop.Step == null ? "true" : "false", ["inclusive"] = loop.Inclusive ? "true" : "false" });
                Wire(Expression(loop.Start, ref tails), range, "start");
                Wire(Expression(loop.End, ref tails), range, "end");
                if (loop.Step != null) Wire(Expression(loop.Step, ref tails), range, "step");
                Connect(tails, range); _loops[loop.SymbolId] = range;
                Block(loop.Body, [new(range, "loop_body")]); _loops.Remove(loop.SymbolId);
                return [new(range, "completed")];
            }
            case BoundMatch match:
                return Match(match, tails);
            case BoundBreak br:
                Connect(tails, Node("flow.break", br.Span)); return [];
            case BoundExpressionStatement action:
                GlyphNodeInstance call = Call(action.Call, ref tails); Connect(tails, call);
                return action.Call.Symbol.Definition.OutputPins.Any(p => p.Id == "exec_out") ? [new(call, "exec_out")] : [];
            case BoundIf conditional:
                GlyphNodeInstance branch;
                string yes = "true", no = "false";
                if (conditional.Condition is BoundCall { Symbol.Strategy: GlyphLoweringStrategy.PredicateBranch } predicate)
                { branch = Call(predicate, ref tails); yes = "success"; no = "failure"; }
                else
                { branch = Node("flow.branch", conditional.Span); Wire(Expression(conditional.Condition, ref tails), branch, "condition"); }
                Connect(tails, branch);
                List<Output> trueTails = Statement(conditional.Then, [new(branch, yes)]);
                List<Output> falseTails = conditional.Else == null ? [new(branch, no)] : Statement(conditional.Else, [new(branch, no)]);
                trueTails.AddRange(falseTails); return trueTails;
            case BoundIteratorForeach loop:
                LowerIterator(loop.Iterator, null, loop.Span, ref tails, loop.SymbolId, loop.Body);
                return tails;
            case BoundForeach loop:
                GlyphNodeInstance each = Each(loop.List.Type, loop.Span);
                if (_languageVersion >= 4) each.PropertyOverrides["snapshot"] = "true";
                Wire(Expression(loop.List, ref tails), each, "list"); Connect(tails, each);
                _loops[loop.SymbolId] = each;
                Block(loop.Body, [new(each, "loop_body")]);
                _loops.Remove(loop.SymbolId);
                return [new(each, "completed")];
            default: throw new InvalidOperationException($"Unsupported bound statement {statement.GetType().Name}.");
        }
    }
    private static string Suffix(GlyphTypeSymbol type) => Runtime.Nodes.Flow.RuntimeValueModule.Suffix(type.RuntimeType!.Value, type.ElementType?.RuntimeType, type.KeyType?.RuntimeType, type.ValueType?.RuntimeType);
    private static Dictionary<string, string> SlotProperties(int id, GlyphTypeSymbol type) => new()
    { ["slot"] = id.ToString(CultureInfo.InvariantCulture), ["nominal"] = type.Name };
    private Output Local(int id, GlyphTypeSymbol type, SourceSpan span) =>
        new(Node("local.read_" + Suffix(type), span, SlotProperties(id, type)), "value");
    private List<Output> Write(int id, GlyphTypeSymbol type, BoundExpression value, SourceSpan span, List<Output> tails)
    {
        Output input = Expression(value, ref tails);
        return WriteOutput(id, type, input, span, tails);
    }
    private List<Output> WriteOutput(int id, GlyphTypeSymbol type, Output input, SourceSpan span, List<Output> tails)
    {
        GlyphNodeInstance write = Node("local.write_" + Suffix(type), span, SlotProperties(id, type));
        if (_languageVersion >= 4) write.PropertyOverrides["snapshot"] = "true";
        Wire(input, write, "value"); Connect(tails, write);
        return [new(write, "exec_out")];
    }
    private List<Output> Match(BoundMatch match, List<Output> tails)
    {
        List<Output> next = Write(match.SymbolId, match.Value.Type, match.Value, match.Span, tails);
        List<Output> joined = [];
        foreach (var arm in match.Arms)
        {
            if (arm.Pattern is BoundWildcardPattern)
            {
                joined.AddRange(Block(arm.Body, next)); next = []; break;
            }
            Output condition;
            if (arm.Pattern is BoundVariantPattern variant)
            {
                GlyphNodeInstance test = Node("aggregate.is_variant", arm.Span, new() { ["type"] = variant.TypeName, ["variant"] = variant.Variant });
                Wire(Local(match.SymbolId, match.Value.Type, match.Span), test, "aggregate");
                condition = new(test, "result");
            }
            else
            {
                var scalar = (BoundValuePattern)arm.Pattern;
                condition = Expression(new BoundBinary(new BoundVariableRead(match.SymbolId, match.Value.Type, match.Span, false), "==", scalar.Value, GlyphTypeSymbol.Bool, arm.Span), ref next);
            }
            GlyphNodeInstance branch = Node("flow.branch", arm.Span);
            Wire(condition, branch, "condition"); Connect(next, branch);
            joined.AddRange(Block(arm.Body, [new(branch, "true")]));
            next = [new(branch, "false")];
        }
        // Valid ADT matches are exhaustive; their final false path cannot join.
        if (match.Value.Type.RuntimeType != GlyphDataType.Aggregate) joined.AddRange(next);
        return joined;
    }
    private Output Aggregate(string type, string? variant, IReadOnlyDictionary<string, BoundExpression> fields, IReadOnlyList<GlyphFieldSymbol> schema, SourceSpan span, ref List<Output> tails)
    {
        Output aggregate = new(Node("aggregate.new", span, new() { ["type"] = type, ["variant"] = variant ?? "" }), "value");
        foreach (var field in fields)
        {
            GlyphTypeSymbol fieldType = schema.Single(f => f.Name == field.Key).Type;
            GlyphNodeInstance add = Node("aggregate.with_" + Suffix(fieldType), field.Value.Span, new() { ["field"] = field.Key, ["nominal"] = fieldType.Name, ["type"] = type, ["snapshot"] = (_languageVersion >= 4).ToString() });
            Wire(aggregate, add, "aggregate"); Wire(Expression(field.Value, ref tails), add, "field_value");
            aggregate = new(add, "value");
        }
        return aggregate;
    }

    private GlyphNodeInstance Call(BoundCall call, ref List<Output> tails)
    {
        GlyphNodeInstance node = Node(call.Symbol.Definition.TypeId, call.Span);
        foreach (var argument in call.Arguments) Wire(Expression(argument.Value, ref tails), node, argument.Key);
        return node;
    }
    private GlyphNodeInstance CollectionNode(string operation, GlyphTypeSymbol type, GlyphTypeSymbol result, SourceSpan span) =>
        Node(Runtime.Nodes.Flow.CollectionExecutor.Id(operation, type.RuntimeType!.Value,
            (type.ElementType ?? type.ValueType)!.RuntimeType!.Value, type.KeyType?.RuntimeType), span,
            new() { ["collection_type"] = type.Name, ["element_type"] = (type.ElementType ?? type.ValueType)!.Name, ["result_type"] = result.Name, ["strict"] = (_languageVersion >= 6).ToString().ToLowerInvariant() });

    private GlyphNodeInstance Each(GlyphTypeSymbol list, SourceSpan span) =>
        Node(list.ElementType == GlyphTypeSymbol.Object ? "flow.for_each" : list.ElementType == GlyphTypeSymbol.Effect ? "flow.for_each_effect"
            : "flow.for_each_" + list.ElementType!.RuntimeType!.Value.ToString().ToLowerInvariant(), span,
            new() { ["list_type"] = list.Name, ["element_type"] = list.ElementType!.Name, ["snapshot"] = (_languageVersion >= 4).ToString().ToLowerInvariant() });

    private Output LowerIterator(BoundIterator iterator, string? operation, SourceSpan span, ref List<Output> tails,
        int? loopSymbol = null, BoundBlock? loopBody = null)
    {
        foreach (var capture in iterator.Setup) tails = Statement(capture, tails);
        int result = _temporary--;
        GlyphTypeSymbol resultType = operation == "collect" ? GlyphTypeSymbol.List(iterator.Element) : GlyphTypeSymbol.Bool;
        Dictionary<string, string> BufferProperties() => new()
        {
            ["slot"] = result.ToString(CultureInfo.InvariantCulture), ["element_type"] = iterator.Element.Name,
            ["result_type"] = resultType.Name
        };
        if (operation == "collect")
        {
            var buffer = Node("iterator.new_" + iterator.Element.RuntimeType!.Value.ToString().ToLowerInvariant(), span, BufferProperties());
            Connect(tails, buffer); tails = [new(buffer, "exec_out")];
        }
        else if (operation == "any") tails = Write(result, GlyphTypeSymbol.Bool, new BoundLiteral(false, GlyphTypeSymbol.Bool, span), span, tails);
        var each = Each(iterator.Source.Type, span);
        Wire(Expression(iterator.Source, ref tails), each, "list"); Connect(tails, each);
        List<Output> body = [new(each, "loop_body")];
        Output current = new(each, "element");
        foreach (var step in iterator.Steps)
        {
            body = WriteOutput(step.Parameter, step.InputType, current, step.Span, body);
            if (step.Operation == "filter")
            {
                var branch = Node("flow.branch", step.Span);
                Wire(Expression(step.Body, ref body), branch, "condition"); Connect(body, branch);
                Connect([new(branch, "false")], Node("flow.continue", step.Span));
                body = [new(branch, "true")];
                current = Local(step.Parameter, step.InputType, step.Span);
            }
            else
            {
                int mapped = _temporary--;
                body = Write(mapped, step.Body.Type, step.Body, step.Span, body);
                current = Local(mapped, step.Body.Type, step.Span);
            }
        }
        if (operation == "collect")
        {
            var add = Node("iterator.add_" + iterator.Element.RuntimeType!.Value.ToString().ToLowerInvariant(), span, BufferProperties());
            Wire(current, add, "value"); Connect(body, add);
        }
        else if (operation == "any")
        {
            body = Write(result, GlyphTypeSymbol.Bool, new BoundLiteral(true, GlyphTypeSymbol.Bool, span), span, body);
            Connect(body, Node("flow.break", span));
        }
        else if (loopSymbol is { } symbol)
        {
            _stored[symbol] = current;
            Block(loopBody!, body);
            _stored.Remove(symbol);
        }
        tails = [new(each, "completed")];
        if (operation == "collect")
        {
            var finish = Node("iterator.finish_" + iterator.Element.RuntimeType!.Value.ToString().ToLowerInvariant(), span, BufferProperties());
            tails = WriteOutput(result, resultType, new(finish, "value"), span, tails);
        }
        return operation == null ? current : Local(result, resultType, span);
    }

    private Output ShortCircuit(BoundBinary binary, ref List<Output> tails)
    {
        Output left = Expression(binary.Left, ref tails);
        GlyphNodeInstance branch = Node("flow.branch", binary.Span);
        Wire(left, branch, "condition");
        Connect(tails, branch);
        string slot = branch.InstanceId.ToString("N");
        bool and = binary.Operator == "&&";
        List<Output> rightTails = [new(branch, and ? "true" : "false")];
        Output right = Expression(binary.Right, ref rightTails);
        GlyphNodeInstance evaluated = Node("flow.capture_bool", binary.Span, new() { ["slot"] = slot });
        Wire(right, evaluated, "value");
        Connect(rightTails, evaluated);
        GlyphNodeInstance skipped = Node("flow.capture_bool", binary.Span, new() { ["slot"] = slot, ["value"] = and ? "false" : "true" });
        Connect([new(branch, and ? "false" : "true")], skipped);
        tails = [new(evaluated, "exec_out"), new(skipped, "exec_out")];
        return new(Node("getter.captured_bool", binary.Span, new() { ["slot"] = slot }), "value");
    }

    private Output Expression(BoundExpression expression, ref List<Output> tails)
    {
        switch (expression)
        {
            case BoundIteratorTerminal terminal:
                return LowerIterator(terminal.Iterator, terminal.Operation, terminal.Span, ref tails);
            case BoundCollectionOperation collection:
            {
                var node = CollectionNode(collection.Operation, collection.CollectionType, collection.Type, collection.Span);
                foreach (var argument in collection.Arguments) Wire(Expression(argument.Value, ref tails), node, argument.Key);
                return new(node, "value");
            }
            case BoundListLiteral list:
            {
                var element = list.Type.ElementType!;
                var items = new List<Output>();
                foreach (var item in list.Values)
                {
                    int slot = _temporary--;
                    tails = Write(slot, element, item, item.Span, tails);
                    items.Add(Local(slot, element, item.Span));
                }
                Output output = new(CollectionNode("new", list.Type, list.Type, list.Span), "value");
                foreach (var item in items)
                {
                    var append = CollectionNode("append", list.Type, list.Type, list.Span);
                    Wire(output, append, "collection"); Wire(item, append, "value"); output = new(append, "value");
                }
                return output;
            }
            case BoundFunctionCall call:
            {
                foreach (var argument in call.Arguments) tails = Statement(argument, tails);
                GlyphNodeInstance function = Node("flow.function", call.Span);
                Connect(tails, function);
                _returns.Push((call.ResultSymbol, call.Type));
                Block(call.Body, [new(function, "body")]);
                _returns.Pop();
                tails = [new(function, "completed")];
                return call.ResultSymbol is { } symbol ? Local(symbol, call.Type, call.Span) : new(function, "completed");
            }
            case BoundVariableRead variable: return Local(variable.SymbolId, variable.Type, variable.Span);
            case BoundStruct structure: return Aggregate(structure.Definition.Name, null, structure.Fields, structure.Definition.Fields, structure.Span, ref tails);
            case BoundVariant variant: return Aggregate(variant.Definition.Name, variant.Variant.Name, variant.Fields, variant.Variant.Fields, variant.Span, ref tails);
            case BoundAggregateField field:
            {
                GlyphNodeInstance access = Node("aggregate.field_" + Suffix(field.Type), field.Span, new() { ["field"] = field.Field, ["nominal"] = field.Type.Name });
                Wire(Expression(field.Receiver, ref tails), access, "aggregate");
                return new(access, "value");
            }
            case BoundLiteral literal:
                string type = literal.Type == GlyphTypeSymbol.Int ? "int" : literal.Type == GlyphTypeSymbol.Float ? "float" : literal.Type == GlyphTypeSymbol.Bool ? "bool" : literal.Type == GlyphTypeSymbol.Object ? "object" : "string";
                string value = Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "";
                return new(Node("constant." + type, literal.Span, new() { ["value"] = value }), "out");
            case BoundContext context: return new(_entries[context.EntryTypeId], context.Pin);
            case BoundLoopElement element: return _stored.TryGetValue(element.SymbolId, out var pipelineElement) ? pipelineElement : new(_loops[element.SymbolId], "element");
            case BoundSequence sequence:
                foreach (var let in sequence.Prefix) _stored[let.SymbolId] = Expression(let.Value, ref tails);
                return Expression(sequence.Value, ref tails);
            case BoundStoredValue stored: return _stored[stored.SymbolId];
            case BoundCall call:
                GlyphNodeInstance called = Call(call, ref tails);
                if (call.Symbol.Strategy == GlyphLoweringStrategy.Action)
                {
                    Connect(tails, called);
                    tails = [new(called, "exec_out")];
                }
                return new(called, call.Symbol.OutputPin!);
            case BoundUnary unary:
                if (unary.Operator == "+") return Expression(unary.Operand, ref tails);
                GlyphNodeInstance negation = Node(unary.Operator == "!" ? "math.not" : unary.Type == GlyphTypeSymbol.Int ? "math.int_op" : "math.math_op", unary.Span,
                    unary.Operator == "-" ? new() { ["a"] = "0", ["operator"] = "-" } : null);
                Wire(Expression(unary.Operand, ref tails), negation, unary.Operator == "!" ? "value" : "b");
                return new(negation, "result");
            case BoundBinary binary when binary.Operator is "&&" or "||":
                return ShortCircuit(binary, ref tails);
            case BoundBinary binary:
                bool boolean = binary.Operator is "&&" or "||";
                string intrinsic = binary.Operator is "==" or "!=" && binary.Left.Type.RuntimeType is GlyphDataType.String or GlyphDataType.Bool or GlyphDataType.NwObject
                    ? "math.equal_" + (binary.Left.Type.RuntimeType == GlyphDataType.NwObject ? "object" : binary.Left.Type.Name.ToLowerInvariant())
                    : boolean ? "math.boolean_op" : binary.Type == GlyphTypeSymbol.Bool ? "math.compare" : binary.Type == GlyphTypeSymbol.Int ? "math.int_op" : "math.math_op";
                string op = binary.Operator switch { "&&" => "AND", "||" => "OR", _ => binary.Operator };
                GlyphNodeInstance operation = Node(intrinsic, binary.Span, new() { ["operator"] = op });
                Wire(Expression(binary.Left, ref tails), operation, "a"); Wire(Expression(binary.Right, ref tails), operation, "b");
                return new(operation, "result");
            default: throw new InvalidOperationException("Only successfully bound expressions can be lowered.");
        }
    }
}
