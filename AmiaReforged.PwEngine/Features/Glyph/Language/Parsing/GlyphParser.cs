using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;

public sealed class GlyphParser(IReadOnlyList<GlyphToken> tokens)
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];
    private int _position, _depth;

    private GlyphToken Current => tokens[Math.Min(_position, tokens.Count - 1)];
    private bool At(string kind) => Current.Kind == kind;
    private GlyphToken Take() { var token = Current; if (!At("eof")) _position++; return token; }

    private GlyphToken Expect(string kind)
    {
        if (At(kind)) return Take();
        Diagnostics.Add(new("GLYPH1001", $"Expected '{kind}', found '{Current.Text}'.", Current.Span));
        return new(kind, "", Current.Span);
    }

    private bool Eat(string kind)
    {
        if (!At(kind)) return false;
        Take();
        return true;
    }

    private SourceSpan Through(SourceSpan start) => start with
    {
        Length = Math.Max(
            0,
            tokens[Math.Max(0, _position - 1)].Span.Start +
            tokens[Math.Max(0, _position - 1)].Span.Length -
            start.Start)
    };

    private void Enter()
    {
        if (++_depth > 128) throw new SyntaxDepthException();
    }

    public GlyphCompilationUnitSyntax? Parse()
    {
        try
        {
            List<TypeDeclarationSyntax> declarations = [];
            SourceSpan unitStart = Current.Span;

            while (At("struct") || At("type"))
            {
                declarations.Add(TypeDeclaration());
                Eat(";");
            }

            SourceSpan glyphStart = Expect("glyph").Span;
            if (declarations.Count == 0) unitStart = glyphStart;

            string name = Expect("identifier").Text;
            Expect(":");
            string evt = Expect("identifier").Text;
            while (Eat(".")) evt += "." + Expect("identifier").Text;

            BlockStatementSyntax body = Block();
            Expect("eof");

            return new(declarations, name, evt, body, Through(unitStart));
        }
        catch (SyntaxDepthException)
        {
            Diagnostics.Add(new("GLYPH1006", "Syntax nesting exceeds 128 levels.", Current.Span));
            return null;
        }
    }

    private TypeDeclarationSyntax TypeDeclaration()
    {
        SourceSpan start = Current.Span;

        if (Eat("struct"))
        {
            string name = Expect("identifier").Text;
            IReadOnlyList<GlyphFieldDeclarationSyntax> fields = FieldBlock();
            return new StructDeclarationSyntax(name, fields, Through(start));
        }

        Expect("type");
        string adtName = Expect("identifier").Text;
        Expect("{");

        List<GlyphVariantDeclarationSyntax> variants = [];
        while (!At("}") && !At("eof"))
        {
            SourceSpan variantStart = Current.Span;
            string variantName = Expect("identifier").Text;
            IReadOnlyList<GlyphFieldDeclarationSyntax> fields = FieldBlock();
            variants.Add(new(variantName, fields, Through(variantStart)));
            Eat(",");
            Eat(";");
        }

        Expect("}");
        return new AdtDeclarationSyntax(adtName, variants, Through(start));
    }

    private IReadOnlyList<GlyphFieldDeclarationSyntax> FieldBlock()
    {
        Expect("{");
        List<GlyphFieldDeclarationSyntax> fields = [];

        while (!At("}") && !At("eof"))
        {
            SourceSpan fieldStart = Current.Span;
            string fieldName = Expect("identifier").Text;
            Expect(":");
            string typeName = Expect("identifier").Text;
            fields.Add(new(fieldName, typeName, Through(fieldStart)));

            Eat(",");
            Eat(";");
        }

        Expect("}");
        return fields;
    }

    private BlockStatementSyntax Block()
    {
        Enter();
        SourceSpan start = Expect("{").Span;
        List<StatementSyntax> statements = [];

        while (!At("}") && !At("eof"))
        {
            int before = _position;
            if (Eat(";")) continue;

            statements.Add(Statement());
            Eat(";");

            if (_position == before) Take();
        }

        Expect("}");
        _depth--;
        return new(statements, Through(start));
    }

    private StatementSyntax Statement()
    {
        Enter();
        try { return StatementCore(); }
        finally { _depth--; }
    }

    private StatementSyntax StatementCore()
    {
        SourceSpan start = Current.Span;

        if (At("{")) return Block();

        if (At("attempted") || At("started") || At("tick") || At("completed"))
        {
            string stage = Take().Kind;
            return new StageDeclarationSyntax(stage, Block(), Through(start));
        }

        if (Eat("let"))
        {
            string name = Expect("identifier").Text;
            Expect("=");
            return new LetStatementSyntax(name, Expression(), Through(start));
        }

        if (Eat("if"))
        {
            ExpressionSyntax condition = Expression();
            BlockStatementSyntax then = Block();
            StatementSyntax? otherwise = Eat("else")
                ? At("if") ? Statement() : Block()
                : null;
            return new IfStatementSyntax(condition, then, otherwise, Through(start));
        }

        if (Eat("foreach"))
        {
            string name = Expect("identifier").Text;
            Expect("in");
            ExpressionSyntax list = Expression();
            return new ForeachStatementSyntax(name, list, Block(), Through(start));
        }

        if (Eat("match"))
        {
            ExpressionSyntax value = Expression();
            Expect("{");
            List<MatchArmSyntax> arms = [];

            while (!At("}") && !At("eof"))
            {
                SourceSpan armStart = Current.Span;
                string variant = Expect("identifier").Text;

                Expect("{");
                List<string> bindings = [];
                while (!At("}") && !At("eof"))
                {
                    bindings.Add(Expect("identifier").Text);
                    if (!Eat(",")) break;
                }
                Expect("}");

                BlockStatementSyntax body = Block();
                arms.Add(new(variant, bindings, body, Through(armStart)));
                Eat(",");
                Eat(";");
            }

            Expect("}");
            return new MatchStatementSyntax(value, arms, Through(start));
        }

        if (Eat("break")) return new BreakStatementSyntax(Through(start));

        if (Current.Text == "fail" &&
            _position + 1 < tokens.Count &&
            tokens[_position + 1].Kind != "(")
        {
            Take();
            ExpressionSyntax message = Expression();
            return new ExpressionStatementSyntax(
                new InvocationExpressionSyntax(
                    new NameExpressionSyntax("fail", start),
                    [new(null, message, message.Span)],
                    Through(start)),
                Through(start));
        }

        ExpressionSyntax expr = Expression();
        if (At("=") || At("+=") || At("-="))
        {
            string op = Take().Kind;
            return new AssignmentStatementSyntax(expr, op, Expression(), Through(start));
        }

        return new ExpressionStatementSyntax(expr, Through(start));
    }

    private ExpressionSyntax Expression(int minimum = 0)
    {
        Enter();
        SourceSpan start = Current.Span;
        ExpressionSyntax left;

        if (At("!") || At("-") || At("+"))
        {
            string op = Take().Kind;
            left = new UnaryExpressionSyntax(op, Expression(7), Through(start));
        }
        else if (Eat("("))
        {
            left = Expression();
            Expect(")");
        }
        else if (Current.Value != null)
        {
            var token = Take();
            left = new LiteralExpressionSyntax(token.Value!, token.Span);
        }
        else
        {
            left = new NameExpressionSyntax(Expect("identifier").Text, start);
        }

        int chainLength = 0;
        while (true)
        {
            if (++chainLength > 128) throw new SyntaxDepthException();

            if (Eat("."))
            {
                left = new MemberAccessExpressionSyntax(left, Expect("identifier").Text, Through(start));
                continue;
            }

            if (Eat("["))
            {
                var index = Expression();
                Expect("]");
                left = new IndexExpressionSyntax(left, index, Through(start));
                continue;
            }

            if (Eat("("))
            {
                List<ArgumentSyntax> args = [];
                while (!At(")") && !At("eof"))
                {
                    int before = _position;
                    SourceSpan argStart = Current.Span;
                    string? name = null;

                    if (At("identifier") &&
                        tokens[Math.Min(_position + 1, tokens.Count - 1)].Kind == ":")
                    {
                        name = Take().Text;
                        Take();
                    }

                    args.Add(new(name, Expression(), Through(argStart)));
                    if (_position == before || !Eat(",")) break;
                }

                Expect(")");
                left = new InvocationExpressionSyntax(left, args, Through(start));
                continue;
            }

            int precedence = Current.Kind switch
            {
                "||" => 1,
                "&&" => 2,
                "==" or "!=" => 3,
                "<" or "<=" or ">" or ">=" => 4,
                "+" or "-" => 5,
                "*" or "/" or "%" => 6,
                _ => 0
            };

            if (precedence == 0 || precedence < minimum) break;

            string binary = Take().Kind;
            left = new BinaryExpressionSyntax(
                left,
                binary,
                Expression(precedence + 1),
                Through(start));
        }

        _depth--;
        return left;
    }

    private sealed class SyntaxDepthException : Exception;
}
