namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

internal static class GlyphBoundLimits
{
    public static bool IsWithinLimits(BoundProgram program)
    {
        Stack<(object Node, int Depth)> pending = new();
        foreach (var stage in program.Stages) pending.Push((stage.Body, 0));

        int expanded = 0;

        while (pending.TryPop(out var next))
        {
            if (next.Depth > 128 || ++expanded > 16384) return false;
            void Push(object node) => pending.Push((node, next.Depth + 1));

            switch (next.Node)
            {
                case BoundReturn ret:
                    if (ret.Value != null) Push(ret.Value);
                    break;
                case BoundFunctionStatement call: Push(call.Call); break;
                case BoundFunctionCall call:
                    foreach (var argument in call.Arguments) Push(argument);
                    Push(call.Body);
                    break;
                case BoundBlock block:
                    foreach (var statement in block.Statements) Push(statement);
                    break;
                case BoundIf branch:
                    Push(branch.Condition);
                    Push(branch.Then);
                    if (branch.Else != null) Push(branch.Else);
                    break;
                case BoundForeach loop:
                    Push(loop.List);
                    Push(loop.Body);
                    break;
                case BoundVar variable: Push(variable.Value); break;
                case BoundVariableAssignment assignment: Push(assignment.Value); break;
                case BoundWhile loop: Push(loop.Condition); Push(loop.Body); break;
                case BoundForRange loop:
                    Push(loop.Start); Push(loop.End); if (loop.Step != null) Push(loop.Step); Push(loop.Body); break;
                case BoundMatch match:
                    Push(match.Value); foreach (var arm in match.Arms) Push(arm); break;
                case BoundMatchArm arm:
                    Push(arm.Pattern); Push(arm.Body); break;
                case BoundValuePattern pattern: Push(pattern.Value); break;
                case BoundAggregateField field: Push(field.Receiver); break;
                case BoundSequence sequence:
                    foreach (var let in sequence.Prefix) Push(let);
                    Push(sequence.Value);
                    break;
                case BoundLet let:
                    Push(let.Value);
                    break;
                case BoundExpressionStatement action:
                    Push(action.Call);
                    break;
                case BoundCall call:
                    foreach (var value in call.Arguments.Values) Push(value);
                    break;
                case BoundUnary unary:
                    Push(unary.Operand);
                    break;
                case BoundBinary binary:
                    Push(binary.Left);
                    Push(binary.Right);
                    break;
                case BoundStruct aggregate:
                    foreach (var value in aggregate.Fields.Values) Push(value);
                    break;
                case BoundVariant variant:
                    foreach (var value in variant.Fields.Values) Push(value);
                    break;
            }
        }

        return true;
    }
}
