using AmiaReforged.PwEngine.Features.Glyph.Language.Diagnostics;
using AmiaReforged.PwEngine.Features.Glyph.Language.Syntax;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;

public sealed class GlyphParser(IReadOnlyList<GlyphToken> tokens, int languageVersion = Compilation.GlyphLanguageVersion.Current)
{
    public List<GlyphDiagnostic> Diagnostics { get; } = [];
    private int _position, _depth;

    private GlyphToken Current => tokens[Math.Min(_position, tokens.Count - 1)];
    private bool At(string kind) => Current.Kind == kind;

    // A token is "identifier-shaped" when its text is a valid identifier spelling and it is
    // either a bare identifier or a keyword whose kind equals its own text (e.g. `type`, `let`).
    // Named argument labels accept identifier-shaped words even when the word is otherwise a
    // Glyph keyword, while every other production keeps its exact token expectations.
    private static bool IsIdentifierShape(GlyphToken token)
    {
        bool identifierOrKeyword = token.Kind == "identifier" || token.Kind == token.Text;
        if (!identifierOrKeyword) return false;

        string text = token.Text;
        if (text.Length == 0) return false;
        if (!(char.IsLetter(text[0]) || text[0] == '_')) return false;
        for (int i = 1; i < text.Length; i++)
            if (!(char.IsLetterOrDigit(text[i]) || text[i] == '_')) return false;
        return true;
    }
    private GlyphToken Take() { var token = Current; if (!At("eof")) _position++; return token; }

    private GlyphToken Expect(string kind)
    {
        if (At(kind)) return Take();
        SourceSpan unexpected = Current.Span;
        Diagnostics.Add(new("GLYPH1001", $"Expected '{kind}', found '{Current.Text}'.", unexpected));
        Take(); // Invalid declarations must make progress, including inside module field blocks.
        return new(kind, "", unexpected);
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
            List<GlobalDeclarationSyntax> globalDeclarations = [];
            SourceSpan unitStart = Current.Span;
            List<GlyphImportSyntax> imports = [];
            List<ImplDeclarationSyntax> implementations = [];
            string? moduleName = null;
            if (Eat("mod"))
            {
                moduleName = Expect("identifier").Text;
                Expect("{");
            }

            // Prelude declarations (const / fn) and type declarations (struct / ADT) may appear in
            // any order before the optional event script. This is what lets a global.glyph file
            // parse as a standalone unit without a `glyph name : event { ... }` body.
            while (At("using") || At("pub") || At("const") || At("fn") || At("struct") || At("type") || At("impl"))
            {
                if (Eat("using"))
                {
                    SourceSpan importStart = tokens[_position - 1].Span;
                    imports.Add(new(Expect("identifier").Text, Through(importStart)));
                    Eat(";");
                    continue;
                }
                SourceSpan declarationStart = Current.Span;
                bool isPublic = Eat("pub");
                if (isPublic && moduleName == null)
                    Diagnostics.Add(new("GLYPH1010", "pub is only allowed on module declarations.", declarationStart));
                if (isPublic && !(At("const") || At("fn") || At("struct") || At("type")))
                {
                    Diagnostics.Add(new("GLYPH1010", "pub requires const, fn, struct, or type.", declarationStart));
                    break;
                }
                if (Eat("const"))
                {
                    string constName = Expect("identifier").Text;
                    while (Eat(".")) constName += "." + Expect("identifier").Text;
                    string? constType = Eat(":") ? TypeName() : null;
                    ExpressionSyntax? initializer = null;
                    if (Eat("="))
                        initializer = Expression();

                    globalDeclarations.Add(new ConstantDeclarationSyntax(constName, initializer, Through(declarationStart), constType) { IsPublic = isPublic });
                }
                else if (Eat("fn"))
                {
                    globalDeclarations.Add(Function(declarationStart) with { IsPublic = isPublic });
                }
                else if (Eat("impl"))
                {
                    RequireVersion4(declarationStart);
                    if (isPublic) Diagnostics.Add(new("GLYPH1010", "Use pub on individual impl functions.", declarationStart));
                    var typeParameters = TypeParameters();
                    string owner = TypeName();
                    Expect("{");
                    while (!At("}") && !At("eof"))
                    {
                        SourceSpan methodStart = Current.Span;
                        bool methodPublic = Eat("pub");
                        Expect("fn");
                        globalDeclarations.Add(Function(methodStart, owner, typeParameters) with { IsPublic = methodPublic });
                        Eat(";");
                    }
                    Expect("}");
                    implementations.Add(new(owner, Through(declarationStart)) { TypeParameters = typeParameters });
                }
                else if (Eat("struct"))
                {
                    string structName = Expect("identifier").Text;
                    var typeParameters = TypeParameters();
                    IReadOnlyList<GlyphFieldDeclarationSyntax> fields = FieldBlock();
                    declarations.Add(new StructDeclarationSyntax(structName, fields, Through(declarationStart)) { IsPublic = isPublic, TypeParameters = typeParameters });
                }
                else if (Eat("type"))
                {
                    string adtName = Expect("identifier").Text;
                    var typeParameters = TypeParameters();
                    Expect("{");

                    List<GlyphVariantDeclarationSyntax> variants = [];
                    while (!At("}") && !At("eof"))
                    {
                        SourceSpan variantStart = Current.Span;
                        string variantName = Expect("identifier").Text;
                        IReadOnlyList<GlyphFieldDeclarationSyntax> fields = FieldBlock();
                        variants.Add(new(variantName, fields, Through(variantStart)));
                        DeclarationSeparator("ADT variants");
                    }

                    Expect("}");
                    declarations.Add(new AdtDeclarationSyntax(adtName, variants, Through(declarationStart)) { IsPublic = isPublic, TypeParameters = typeParameters });
                }

                Eat(";");
            }

            if (moduleName != null) Expect("}");

            // A normal event script follows the prelude/type declarations. When the `glyph`
            // keyword is absent the source is a standalone prelude and no event body is parsed.
            string name = "";
            string evt = "";
            BlockStatementSyntax body = new([], unitStart);
            if (moduleName == null && At("glyph"))
            {
                SourceSpan glyphStart = Expect("glyph").Span;
                if (globalDeclarations.Count == 0 && declarations.Count == 0) unitStart = glyphStart;

                name = Expect("identifier").Text;
                Expect(":");
                evt = Expect("identifier").Text;
                while (Eat(".")) evt += "." + Expect("identifier").Text;

                body = Block();
            }

            Expect("eof");

            return new(globalDeclarations, declarations, name, evt, body, Through(unitStart)) { Imports = imports, Implementations = implementations, ModuleName = moduleName, LanguageVersion = languageVersion };
        }
        catch (SyntaxDepthException)
        {
            Diagnostics.Add(new("GLYPH1006", "Syntax nesting exceeds 128 levels.", Current.Span));
            return null;
        }
    }

    private string QualifiedIdentifier()
    {
        string name = Expect("identifier").Text;
        int count = 0;
        while (Eat("."))
        {
            if (++count > 128) throw new SyntaxDepthException();
            name += "." + Expect("identifier").Text;
        }
        return name;
    }

    private void RequireVersion4(SourceSpan span)
    {
        if (languageVersion < 4) Diagnostics.Add(new("GLYPH1012", "Collections and impl require language version 4.", span));
    }

    private void RequireVersion5(SourceSpan span)
    {
        if (languageVersion < 5) Diagnostics.Add(new("GLYPH1013", "User-defined generics require language version 5.", span));
    }

    private IReadOnlyList<string> TypeParameters()
    {
        if (!Eat("<")) return [];
        RequireVersion5(Current.Span);
        List<string> parameters = [];
        do { parameters.Add(Expect("identifier").Text); } while (Eat(","));
        Expect(">");
        return parameters;
    }

    private string TypeName()
    {
        Enter();
        try
        {
            string name = QualifiedIdentifier();
            if (!At("<")) return name;
            if (name is "List" or "Dictionary") RequireVersion4(Current.Span);
            else RequireVersion5(Current.Span);
            return GlyphTypeNames.Apply(name, TypeArguments());
        }
        finally { _depth--; }
    }

    private IReadOnlyList<string> TypeArguments()
    {
        Expect("<");
        List<string> arguments = [];
        do { arguments.Add(TypeName()); } while (Eat(","));
        Expect(">");
        return arguments;
    }

    private FunctionDeclarationSyntax Function(SourceSpan start, string? owner = null, IReadOnlyList<string>? ownerParameters = null)
    {
        string name = Expect("identifier").Text;
        var typeParameters = TypeParameters();
        Expect("(");
        List<ParameterSyntax> parameters = [];
        while (!At(")") && !At("eof"))
        {
            SourceSpan at = Current.Span;
            string parameter = Expect("identifier").Text;
            string? type = Eat(":") ? TypeName() : null;
            if (owner != null && parameter == "self" && parameters.Count == 0) type ??= owner;
            if (owner != null && type == "Self") type = owner;
            parameters.Add(new(parameter, type, Through(at)));
            if (!Eat(",") && !Eat(";")) break;
        }
        Expect(")"); Expect(":");
        string result = TypeName();
        if (owner != null && result == "Self") result = owner;
        FunctionBodySyntax body;
        if (At("{"))
        {
            if (languageVersion < 3) Diagnostics.Add(new("GLYPH1011", "Statement function bodies require language version 3.", Current.Span));
            body = new BlockFunctionBodySyntax(Block());
        }
        else { Expect("="); body = new ExpressionFunctionBodySyntax(Expression()); }
        return new(owner == null ? name : GlyphTypeNames.Parse(owner).Name + "." + name, parameters, result, body, Through(start))
        { DeclaringType = owner, LanguageVersion = languageVersion, TypeParameters = [..ownerParameters ?? [], ..typeParameters] };
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
            string typeName = TypeName();
            fields.Add(new(fieldName, typeName, Through(fieldStart)));

            DeclarationSeparator("fields");
        }

        Expect("}");
        return fields;
    }

    private void DeclarationSeparator(string items)
    {
        if (Eat(",")) return;
        if (At(";"))
        {
            Diagnostics.Add(new("GLYPH1014", $"Use ',' instead of ';' between {items}.", Current.Span));
            Take();
        }
        else if (!At("}") && !At("eof"))
            Diagnostics.Add(new("GLYPH1014", $"Expected ',' between {items}.", Current.Span));
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
        if (Eat("return"))
            return new ReturnStatementSyntax(At(";") || At("}") ? null : Expression(), Through(start));

        if (At("attempted") || At("started") || At("tick") || At("completed"))
        {
            string stage = Take().Kind;
            return new StageDeclarationSyntax(stage, Block(), Through(start));
        }

        if (At("let") || At("var"))
        {
            bool mutable = Take().Kind == "var";
            string name = Expect("identifier").Text;
            Expect("=");
            ExpressionSyntax initializer = Expression();
            return mutable ? new VarStatementSyntax(name, initializer, Through(start)) : new LetStatementSyntax(name, initializer, Through(start));
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

        if (Eat("while"))
        {
            ExpressionSyntax condition = Expression();
            return new WhileStatementSyntax(condition, Block(), Through(start));
        }

        if (At("foreach") || At("for"))
        {
            bool canonical = Take().Kind == "for";
            string name = Expect("identifier").Text;
            Expect("in");
            ExpressionSyntax list = Expression();
            if (canonical && (At("..") || At("..=")))
            {
                bool inclusive = Take().Kind == "..=";
                ExpressionSyntax end = Expression();
                ExpressionSyntax? step = Eat("step") ? Expression() : null;
                return new ForRangeStatementSyntax(name, list, end, inclusive, step, Block(), Through(start));
            }
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
                MatchPatternSyntax pattern;
                if (Current.Text == "_")
                {
                    Take();
                    pattern = new WildcardPatternSyntax(armStart);
                }
                else if (At("identifier") && LooksLikeVariantPattern())
                {
                    string variant = TypeName();
                    while (Eat(".")) variant += "." + Expect("identifier").Text;
                    Expect("{");
                    List<string> bindings = [];
                    while (!At("}") && !At("eof"))
                    {
                        bindings.Add(Expect("identifier").Text);
                        if (!Eat(",")) break;
                    }
                    Expect("}");
                    pattern = new VariantPatternSyntax(variant, bindings, Through(armStart));
                }
                else pattern = new ValuePatternSyntax(Expression(), Through(armStart));

                BlockStatementSyntax body = Block();
                arms.Add(new(pattern, body, Through(armStart)));
                Eat(",");
                Eat(";");
            }

            Expect("}");
            return new MatchStatementSyntax(value, arms, Through(start));
        }

        if (Eat("continue")) return new ContinueStatementSyntax(Through(start));

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
        if (At("=") || At("+=") || At("-=") || At("*=") || At("/="))
        {
            string op = Take().Kind;
            return new AssignmentStatementSyntax(expr, op, Expression(), Through(start));
        }

        return new ExpressionStatementSyntax(expr, Through(start));
    }

    // A variant pattern has a field block followed by a body block. A bare constant
    // has only its body, so scan the bounded field spelling before choosing the production.
    private bool LooksLikeVariantPattern()
    {
        int i = _position + 1;
        int nesting = 0;
        while (i < tokens.Count)
        {
            string kind = tokens[i].Kind;
            if (kind == "<") nesting++;
            else if (kind == ">") nesting--;
            else if (kind is not ("identifier" or "." or ",")) break;
            if (nesting < 0) return false;
            i++;
        }
        if (nesting != 0) return false;
        if (tokens[Math.Min(i++, tokens.Count - 1)].Kind != "{") return false;
        while (i < tokens.Count && tokens[i].Kind == "identifier")
        {
            i++;
            if (tokens[i].Kind != ",") break;
            i++;
        }
        return i + 1 < tokens.Count && tokens[i].Kind == "}" && tokens[i + 1].Kind == "{";
    }

    private bool LooksLikeTypeApplication()
    {
        int depth = 0;
        for (int i = _position; i < tokens.Count; i++)
        {
            string kind = tokens[i].Kind;
            if (kind == "<") depth++;
            else if (kind == ">")
            {
                if (--depth == 0) return i + 1 < tokens.Count && tokens[i + 1].Kind is "(" or ".";
            }
            else if (kind is not ("identifier" or "." or ",")) return false;
        }
        return false;
    }

    private ExpressionSyntax Expression(int minimum = 0)
    {
        Enter();
        SourceSpan start = Current.Span;
        ExpressionSyntax left;

        if (At("-") && _position + 1 < tokens.Count && tokens[_position + 1].Value is 2147483648L)
        {
            Take(); Take();
            left = new LiteralExpressionSyntax(int.MinValue, Through(start));
        }
        else if (At("!") || At("-") || At("+"))
        {
            string op = Take().Kind;
            left = new UnaryExpressionSyntax(op, Expression(7), Through(start));
        }
        else if (Eat("("))
        {
            left = Expression();
            Expect(")");
        }
        else if (Eat("["))
        {
            RequireVersion4(start);
            List<ExpressionSyntax> values = [];
            while (!At("]") && !At("eof"))
            {
                values.Add(Expression());
                if (!Eat(",")) break;
            }
            Expect("]");
            left = new ListExpressionSyntax(values, Through(start));
        }
        else if (languageVersion >= 4 && Current.Text is ("List" or "Dictionary") && tokens[Math.Min(_position + 1, tokens.Count - 1)].Kind == "<")
        {
            RequireVersion4(start);
            string type = TypeName();
            Expect("("); Expect(")");
            left = new CollectionConstructorSyntax(type, Through(start));
        }
        else if (Current.Value != null)
        {
            var token = Take();
            if (token.Value is long)
                Diagnostics.Add(new("GLYPH1003", "Number is out of range.", token.Span));
            left = new LiteralExpressionSyntax(token.Value is long ? 0 : token.Value!, token.Span);
        }
        else
        {
            left = new NameExpressionSyntax(Expect("identifier").Text, start);
        }

        int chainLength = 0;
        while (true)
        {
            if (++chainLength > 128) throw new SyntaxDepthException();

            if (languageVersion >= 5 && At("<") && LooksLikeTypeApplication())
            {
                RequireVersion5(Current.Span);
                left = new TypeApplicationExpressionSyntax(left, TypeArguments(), Through(start));
                continue;
            }

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

                    if (IsIdentifierShape(Current) &&
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
