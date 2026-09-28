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
            case BoundBreak br:
                Connect(tails, Node("flow.break", br.Span)); return [];
            case BoundExpressionStatement action:
                GlyphNodeInstance call = Call(action.Call); Connect(tails, call);
                return action.Call.Symbol.Definition.OutputPins.Any(p => p.Id == "exec_out") ? [new(call, "exec_out")] : [];
            case BoundIf conditional:
                GlyphNodeInstance branch;
                string yes = "true", no = "false";
                if (conditional.Condition is BoundCall { Symbol.Strategy: GlyphLoweringStrategy.PredicateBranch } predicate)
                { branch = Call(predicate); yes = "success"; no = "failure"; }
                else
                { branch = Node("flow.branch", conditional.Span); Wire(Expression(conditional.Condition), branch, "condition"); }
                Connect(tails, branch);
                List<Output> trueTails = Statement(conditional.Then, [new(branch, yes)]);
                List<Output> falseTails = conditional.Else == null ? [new(branch, no)] : Statement(conditional.Else, [new(branch, no)]);
                trueTails.AddRange(falseTails); return trueTails;
            case BoundForeach loop:
                GlyphNodeInstance each = Node("flow.for_each", loop.Span);
                Wire(Expression(loop.List), each, "list"); Connect(tails, each);
                _loops[loop.SymbolId] = each;
                Block(loop.Body, [new(each, "loop_body")]);
                _loops.Remove(loop.SymbolId);
                return [new(each, "completed")];
            default: throw new InvalidOperationException($"Unsupported bound statement {statement.GetType().Name}.");
        }
    }
    private GlyphNodeInstance Call(BoundCall call)
    {
        GlyphNodeInstance node = Node(call.Symbol.Definition.TypeId, call.Span);
        foreach (var argument in call.Arguments) Wire(Expression(argument.Value), node, argument.Key);
        return node;
    }
    private Output Expression(BoundExpression expression)
    {
        switch (expression)
        {
            case BoundLiteral literal:
                string type = literal.Type == GlyphTypeSymbol.Int ? "int" : literal.Type == GlyphTypeSymbol.Float ? "float" : literal.Type == GlyphTypeSymbol.Bool ? "bool" : "string";
                string value = Convert.ToString(literal.Value, CultureInfo.InvariantCulture) ?? "";
                return new(Node("constant." + type, literal.Span, new() { ["value"] = value }), "out");
            case BoundContext context: return new(_entries[context.EntryTypeId], context.Pin);
            case BoundLoopElement element: return new(_loops[element.SymbolId], "element");
            case BoundCall call: return new(Call(call), call.Symbol.OutputPin!);
            case BoundUnary unary:
                if (unary.Operator == "+") return Expression(unary.Operand);
                GlyphNodeInstance negation = Node(unary.Operator == "!" ? "math.not" : "math.math_op", unary.Span,
                    unary.Operator == "-" ? new() { ["a"] = "0", ["operator"] = "-" } : null);
                Wire(Expression(unary.Operand), negation, unary.Operator == "!" ? "value" : "b");
                return new(negation, "result");
            case BoundBinary binary:
                bool boolean = binary.Operator is "&&" or "||";
                string intrinsic = boolean ? "math.boolean_op" : binary.Type == GlyphTypeSymbol.Bool ? "math.compare" : "math.math_op";
                string op = binary.Operator switch { "&&" => "AND", "||" => "OR", _ => binary.Operator };
                GlyphNodeInstance operation = Node(intrinsic, binary.Span, new() { ["operator"] = op });
                Wire(Expression(binary.Left), operation, "a"); Wire(Expression(binary.Right), operation, "b");
                return new(operation, "result");
            default: throw new InvalidOperationException("Only successfully bound expressions can be lowered.");
        }
    }
}
