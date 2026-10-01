namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

internal static class GlyphBoundLimits
{
    public static IEnumerable<object> Children(object node) => node switch
    {
        BoundReturn ret => ret.Value == null ? [] : [ret.Value],
        BoundFunctionStatement call => [call.Call],
        BoundFunctionCall call => [.. call.Arguments, call.Body],
        BoundBlock block => block.Statements,
        BoundIf branch => branch.Else == null ? [branch.Condition, branch.Then] : [branch.Condition, branch.Then, branch.Else],
        BoundForeach loop => [loop.List, loop.Body], BoundIteratorForeach loop => [loop.Iterator, loop.Body],
        BoundVar variable => [variable.Value], BoundVariableAssignment assignment => [assignment.Value],
        BoundWhile loop => [loop.Condition, loop.Body],
        BoundForRange loop => loop.Step == null ? [loop.Start, loop.End, loop.Body] : [loop.Start, loop.End, loop.Step, loop.Body],
        BoundMatch match => [match.Value, .. match.Arms], BoundMatchArm arm => [arm.Pattern, arm.Body],
        BoundValuePattern pattern => [pattern.Value], BoundListLiteral list => list.Values,
        BoundAggregateField field => [field.Receiver], BoundSequence sequence => [.. sequence.Prefix, sequence.Value],
        BoundLet let => [let.Value], BoundExpressionStatement action => [action.Call],
        BoundCall call => call.Arguments.Values, BoundCollectionOperation call => call.Arguments.Values,
        BoundUnary unary => [unary.Operand], BoundBinary binary => [binary.Left, binary.Right],
        BoundStruct aggregate => aggregate.Fields.Values, BoundVariant variant => variant.Fields.Values,
        BoundIteratorTerminal terminal => [terminal.Iterator],
        BoundIterator iterator => [iterator.Source, .. iterator.Setup, .. iterator.Steps], BoundIteratorStep step => [step.Body],
        _ => []
    };
    public static IEnumerable<object> Descendants(object root)
    {
        Stack<object> pending = new(); pending.Push(root);
        while (pending.TryPop(out var node))
        {
            yield return node;
            foreach (var child in Children(node)) pending.Push(child);
        }
    }
    public static bool IsWithinLimits(BoundProgram program)
    {
        Stack<(object Node, int Depth)> pending = new();
        foreach (var stage in program.Stages) pending.Push((stage.Body, 0));
        int expanded = 0;
        while (pending.TryPop(out var next))
        {
            if (next.Depth > 128 || ++expanded > 16384) return false;
            foreach (var child in Children(next.Node)) pending.Push((child, next.Depth + 1));
        }
        return true;
    }
}
