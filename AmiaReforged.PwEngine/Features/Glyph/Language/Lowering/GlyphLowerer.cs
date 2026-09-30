using System.Globalization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Lowering;

/// <summary>Emits only existing runtime operations. Syntax never reaches the interpreter.</summary>
public sealed class GlyphLowerer
{
    public sealed class LimitExceededException : Exception;
    private readonly GlyphGraph _ir = new() { Id = Guid.Empty };
    private readonly Dictionary<Guid, SourceSpan> _sourceMap = [];
    private readonly Dictionary<string, GlyphNodeInstance> _entries = [];
    private readonly Dictionary<int, GlyphNodeInstance> _loops = [];
    private readonly Dictionary<int, Output> _stored = [];
    private int _identity;
    private readonly record struct Output(GlyphNodeInstance Node, string Pin);
    public (GlyphGraph Ir, IReadOnlyDictionary<Guid, SourceSpan> SourceMap) Lower(BoundProgram program)
    {
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
            case BoundLet let:
                _stored[let.SymbolId] = Expression(let.Value, ref tails);
                return tails;
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
            case BoundForeach loop:
                GlyphNodeInstance each = Node(loop.List.Type.ElementType == GlyphTypeSymbol.Effect ? "flow.for_each_effect" : "flow.for_each", loop.Span);
                Wire(Expression(loop.List, ref tails), each, "list"); Connect(tails, each);
                _loops[loop.SymbolId] = each;
                Block(loop.Body, [new(each, "loop_body")]);
                _loops.Remove(loop.SymbolId);
                return [new(each, "completed")];
            default: throw new InvalidOperationException($"Unsupported bound statement {statement.GetType().Name}.");
        }
    }
    private GlyphNodeInstance Call(BoundCall call, ref List<Output> tails)
    {
        GlyphNodeInstance node = Node(call.Symbol.Definition.TypeId, call.Span);
        foreach (var argument in call.Arguments) Wire(Expression(argument.Value, ref tails), node, argument.Key);
        return node;
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
            case BoundLiteral literal:
                string type = literal.Type == GlyphTypeSymbol.Int ? "int" : literal.Type == GlyphTypeSymbol.Float ? "float" : literal.Type == GlyphTypeSymbol.Bool ? "bool" : literal.Type == GlyphTypeSymbol.Object ? "object" : "string";
                string value = Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "";
                return new(Node("constant." + type, literal.Span, new() { ["value"] = value }), "out");
            case BoundContext context: return new(_entries[context.EntryTypeId], context.Pin);
            case BoundLoopElement element: return new(_loops[element.SymbolId], "element");
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
                GlyphNodeInstance negation = Node(unary.Operator == "!" ? "math.not" : "math.math_op", unary.Span,
                    unary.Operator == "-" ? new() { ["a"] = "0", ["operator"] = "-" } : null);
                Wire(Expression(unary.Operand, ref tails), negation, unary.Operator == "!" ? "value" : "b");
                return new(negation, "result");
            case BoundBinary binary when binary.Operator is "&&" or "||":
                return ShortCircuit(binary, ref tails);
            case BoundBinary binary:
                bool boolean = binary.Operator is "&&" or "||";
                string intrinsic = binary.Operator is "==" or "!=" && binary.Left.Type.RuntimeType is GlyphDataType.String or GlyphDataType.Bool or GlyphDataType.NwObject
                    ? "math.equal_" + (binary.Left.Type.RuntimeType == GlyphDataType.NwObject ? "object" : binary.Left.Type.Name.ToLowerInvariant())
                    : boolean ? "math.boolean_op" : binary.Type == GlyphTypeSymbol.Bool ? "math.compare" : "math.math_op";
                string op = binary.Operator switch { "&&" => "AND", "||" => "OR", _ => binary.Operator };
                GlyphNodeInstance operation = Node(intrinsic, binary.Span, new() { ["operator"] = op });
                Wire(Expression(binary.Left, ref tails), operation, "a"); Wire(Expression(binary.Right, ref tails), operation, "b");
                return new(operation, "result");
            default: throw new InvalidOperationException("Only successfully bound expressions can be lowered.");
        }
    }
}
