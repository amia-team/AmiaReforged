using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class AggregateTypeLanguageTests
{
    private GlyphBootstrap _runtime = null!;

    [SetUp]
    public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());

    private GlyphExecutable Compile(string source)
    {
        GlyphCompilationResult result = _runtime.Compiler.Compile(source);
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
        return result.Executable!;
    }

    private GlyphCompilationResult CompileResult(string source) =>
        _runtime.Compiler.Compile(source);

    [Test]
    public void struct_constructor_and_field_access_compile()
    {
        Compile("""
            struct HarvestRequest {
                resource: String,
                amount: Int,
                harvester: Object,
            }

            glyph harvest : interaction {
                tick {
                    let request = HarvestRequest(
                        resource: "iron",
                        amount: 5,
                        harvester: player
                    )

                    message(request.harvester, request.resource)
                }
            }
            """);
    }

    [Test]
    public void declarations_are_two_pass_and_nested_structs_compile()
    {
        Compile("""
            struct Outer {
                inner: Inner,
            }

            struct Inner {
                value: String,
            }

            glyph nested : interaction {
                tick {
                    let outer = Outer(inner: Inner(value: "hello"))
                    message(player, outer.inner.value)
                }
            }
            """);
    }

    [Test]
    public void struct_field_can_feed_object_receiver_method()
    {
        Compile("""
            struct Target {
                object: Object,
            }

            glyph receiver : interaction {
                tick {
                    let target = Target(object: player)

                    if nwn.is_player(target.object) {
                        message(target.object, "pc")
                    }
                }
            }
            """);
    }

    [Test]
    public void adt_constructor_and_exhaustive_match_compile()
    {
        GlyphExecutable executable = Compile("""
            type SearchResult {
                Found {
                    target: Object,
                    text: String,
                },

                Missing {
                    reason: String,
                },
            }

            glyph search : interaction {
                tick {
                    let result = SearchResult.Found(
                        target: player,
                        text: "found"
                    )

                    match result {
                        Found { target, text } {
                            message(target, text)
                        }

                        Missing { reason } {
                            message(player, reason)
                        }
                    }
                }
            }
            """);

        int messages = executable.CreateExecutionGraph()
            .Nodes.Count(n => n.TypeId == "action.send_message");

        // Runtime dispatch retains both arm bodies in the IR.
        Assert.That(messages, Is.EqualTo(2));
    }

    [Test]
    public void empty_variant_compiles()
    {
        Compile("""
            type Result {
                Found {
                    target: Object,
                },

                Missing {
                },
            }

            glyph search : interaction {
                tick {
                    let result = Result.Missing()

                    match result {
                        Found { target } {
                            message(target, "found")
                        }

                        Missing { } {
                            message(player, "missing")
                        }
                    }
                }
            }
            """);
    }

    [TestCase("""
        struct Request { amount: Int, }
        glyph t : interaction { tick { let x = Request() } }
        """, "GLYPH2003")]
    [TestCase("""
        struct Request { amount: Int, }
        glyph t : interaction { tick { let x = Request(amount: 1, nope: 2) } }
        """, "GLYPH2003")]
    [TestCase("""
        struct Request { amount: Int, }
        glyph t : interaction { tick { let x = Request(amount: "wrong") } }
        """, "GLYPH2004")]
    [TestCase("""
        struct Request { amount: Int, }
        glyph t : interaction { tick { let x = Request(amount: 1); message(player, x.nope) } }
        """, "GLYPH3002")]
    [TestCase("""
        type Result { Found { target: Object, }, Missing { reason: String, }, }
        glyph t : interaction {
            tick {
                let x = Result.Found(target: player)
                match x {
                    Found { target } { message(target, "found") }
                }
            }
        }
        """, "GLYPH2010")]
    [TestCase("""
        type Result { Found { target: Object, }, Missing { reason: String, }, }
        glyph t : interaction {
            tick {
                let x = Result.Found(target: player)
                match x {
                    Found { nope } { message(player, "found") }
                    Missing { reason } { message(player, reason) }
                }
            }
        }
        """, "GLYPH2010")]
    [TestCase("""
        type Result { Found { target: Object, }, }
        glyph t : interaction {
            tick {
                let x = Result.Nope()
            }
        }
        """, "GLYPH2010")]
    public void invalid_aggregate_programs_are_diagnosed(string source, string code)
    {
        GlyphCompilationResult result = CompileResult(source);

        Assert.That(result.Executable, Is.Null);
        Assert.That(
            result.Diagnostics.Any(d => d.Code == code),
            Is.True,
            string.Join("\n", result.Diagnostics));
    }
}
