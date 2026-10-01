import { test } from "node:test";
import assert from "node:assert/strict";
import { EditorState } from "@codemirror/state";
import { CompletionContext } from "@codemirror/autocomplete";
import { glyph } from "./glyph-language.js";
import { glyphCompletions } from "./glyph-completion.js";
import { metadata } from "./test-metadata.js";
import { compilerDiagnostics, diagnosticRange } from "./glyph-diagnostics.js";

function complete(marked, explicit = true, catalog = metadata) {
  const pos = marked.indexOf("|");
  const source = marked.replace("|", "");
  const state = EditorState.create({
    doc: source,
    extensions: [glyph()],
  });

  return glyphCompletions(catalog)(
    new CompletionContext(state, pos, explicit),
  );
}

function labels(source) {
  return complete(source)?.options.map((o) => o.label) || [];
}

function completionQuery(marked, result) {
  const pos = marked.indexOf("|");
  const source = marked.replace("|", "");

  return source.slice(result.from, pos);
}

test("members and functions respect event/stage restrictions and signatures", () => {
  // Object-typed receivers expose curated Object methods alongside
  // namespace/alias members. Member-mode labels are relative to the final dot.
  const playerMembers = labels(
    "glyph g : interaction { attempted { player.| } }",
  );

  assert.ok(playerMembers.includes("has_item"));
  assert.ok(playerMembers.includes("get_nearest_object_by_type"));
  assert.ok(playerMembers.includes("is_player"));
  assert.ok(playerMembers.includes("get_distance"));

  const creatureMembers = labels(
    "glyph g : interaction { tick { creature.| } }",
  );

  assert.ok(creatureMembers.includes("hp"));
  assert.ok(creatureMembers.includes("get_nearest_object_by_type"));
  assert.ok(creatureMembers.includes("is_player"));
  assert.ok(creatureMembers.includes("get_distance"));

  // Namespace member labels are also relative.
  assert.ok(
    !labels(
      "glyph g : interaction { attempted { context.| } }",
    ).includes("session_id"),
  );

  assert.ok(
    labels(
      "glyph g : interaction { tick { context.| } }",
    ).includes("session_id"),
  );

  // Ordinary top-level completion remains fully named.
  assert.ok(
    !labels(
      "glyph g : interaction { attempted { | } }",
    ).includes("set_progress"),
  );

  assert.ok(
    labels(
      "glyph g : interaction { tick { | } }",
    ).includes("set_progress"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { | } }",
    ).includes("spawn.cancel"),
  );

  assert.ok(
    labels(
      "glyph g : encounter.before_group_spawn { | }",
    ).includes("spawn.cancel"),
  );

  const option = complete(
    "glyph g : interaction { tick { mess| } }",
  ).options.find((o) => o.label === "message");

  assert.match(
    option.detail,
    /creature: Object, message: String/,
  );

  assert.equal(typeof option.apply, "function");
});

test("typed receiver completion resolves the receiver Glyph type", () => {
  // Context field typed Object.
  const contextCreature = labels(
    "glyph g : interaction { attempted { context.creature.| } }",
  );

  assert.ok(
    contextCreature.includes("get_nearest_object_by_type"),
  );

  assert.ok(contextCreature.includes("is_player"));

  // Partial member:
  // CompletionResult.from must begin immediately after the final dot.
  const partialSource =
    "glyph g : interaction { attempted { context.creature.get_| } }";

  const partial = complete(partialSource);

  assert.ok(
    partial.options
      .map((o) => o.label)
      .includes("get_nearest_object_by_type"),
  );

  const expectedFrom =
    "glyph g : interaction { attempted { context.creature.".length;

  assert.equal(partial.from, expectedFrom);

  // This is the important regression assertion:
  // CodeMirror must filter against "get_", not
  // "context.creature.get_".
  assert.equal(completionQuery(partialSource, partial), "get_");

  // Empty member prefix should likewise begin after the dot.
  const emptyMemberSource =
    "glyph g : interaction { attempted { context.creature.| } }";

  const emptyMember = complete(emptyMemberSource);

  assert.equal(completionQuery(emptyMemberSource, emptyMember), "");

  // Local inferred Object from an Object-returning static call.
  const localFromStatic = labels(
    'glyph g : interaction { tick { let target = Object.nearest_object_by_type(player, "creature")\ntarget.| } }',
  );

  assert.ok(
    localFromStatic.includes("get_nearest_object_by_type"),
  );

  assert.ok(localFromStatic.includes("is_player"));

  // Local inferred Object from a receiver-method result.
  const localFromReceiver = labels(
    'glyph g : interaction { tick { let target = player.get_nearest_object_by_type("creature")\ntarget.| } }',
  );

  assert.ok(
    localFromReceiver.includes("get_nearest_object_by_type"),
  );

  assert.ok(localFromReceiver.includes("is_player"));

  // Chaining: an Object-returning receiver call again offers
  // Object methods.
  const chained = labels(
    'glyph g : interaction { attempted { player.get_nearest_object_by_type("creature").| } }',
  );
  // Partial member on a named Object alias: get_distance appears alongside the other Object methods.
  const playerGet = labels(
    "glyph g : interaction { attempted { player.get_| } }",
  );
  assert.ok(playerGet.includes("get_distance"));
  assert.ok(playerGet.includes("get_nearest_object_by_type"));


  assert.ok(
    chained.includes("get_nearest_object_by_type"),
  );

  assert.ok(chained.includes("is_player"));

  // Foreach element typed Object.
  const foreach = labels(
    "glyph g : interaction { tick { foreach member in party.members { member.| } } }",
  );

  assert.ok(
    foreach.includes("get_nearest_object_by_type"),
  );

  assert.ok(foreach.includes("is_player"));

  // Wrong receiver type: Int and String must not get Object methods.
  assert.ok(
    !labels(
      "glyph g : interaction { tick { party.size.| } }",
    ).includes("is_player"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { context.character_id.| } }",
    ).includes("get_nearest_object_by_type"),
  );
});

test("receiver completions carry receiver-stripped signatures and named arguments", () => {
  const option = complete(
    "glyph g : interaction { attempted { context.creature.get_| } }",
  ).options.find(
    (o) => o.label === "get_nearest_object_by_type",
  );

  assert.match(option.detail, /\(type: String\) → Object/);
  assert.ok(!/origin/.test(option.detail));

  const isPlayer = complete(
    "glyph g : interaction { attempted { context.creature.is_| } }",
  ).options.find((o) => o.label === "is_player");

  assert.match(isPlayer.detail, /^\(\) → Bool/);

  // Object.get_distance receiver: only the second object is exposed; object_a is injected.
  const getDistance = complete(
    "glyph g : interaction { attempted { context.creature.get_| } }",
  ).options.find((o) => o.label === "get_distance");

  assert.match(getDistance.detail, /^\(object_b: Object\) → Float/);
  assert.ok(!/object_a/.test(getDistance.detail));

  // Named arguments omit the injected object_a.
  const distanceNamed = labels(
    "glyph g : interaction { attempted { context.creature.get_distance(|) } }",
  );
  assert.ok(distanceNamed.includes("object_b:"));
  assert.ok(!distanceNamed.includes("object_a:"));

  // Named arguments omit the injected origin parameter.
  const named = labels(
    "glyph g : interaction { attempted { context.creature.get_nearest_object_by_type(|) } }",
  );

  assert.ok(named.includes("type:"));
  assert.ok(!named.includes("origin:"));

  const noArgs = labels(
    "glyph g : interaction { attempted { context.creature.is_player(|) } }",
  );

  assert.ok(!noArgs.includes("origin:"));
});

test("static Object namespace and curated-only surface are preserved", () => {
  const object = labels(
    "glyph g : interaction { tick { Object.| } }",
  );

  // Static namespace member-mode labels are relative to Object.
  assert.ok(object.includes("nearest_object_by_type"));
  assert.ok(object.includes("is_player"));
  assert.ok(object.includes("get_distance"));

  assert.ok(
    !object.includes("Object.nearest_object_by_type"),
  );

  assert.ok(!object.includes("Object.is_player"));

  const contextCreature = labels(
    "glyph g : interaction { attempted { context.creature.| } }",
  );

  // Only curated receiver methods, never raw NWScript/.NET/Anvil members.
  for (const leaked of [
    "Destroy",
    "Area",
    "ObjectId",
    "GetObjectVariable",
    "ApplyEffect",
  ]) {
    assert.ok(
      !contextCreature.includes(leaked),
      `unexpected leaked member ${leaked}`,
    );
  }
});

test("named arguments omit parameters already supplied positionally or by name", () => {
  assert.ok(
    labels(
      "glyph g : interaction { tick { if skill_check(|) {} } }",
    ).includes("creature:"),
  );

  const options = labels(
    'glyph g : interaction { tick { if skill_check(player, "search", |) {} } }',
  );

  assert.ok(options.includes("dc:"));
  assert.ok(!options.includes("creature:"));
  assert.ok(!options.includes("skill:"));

  assert.ok(
    !labels(
      "glyph g : interaction { tick { if skill_check(creature: player, |) {} } }",
    ).includes("creature:"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { if skill_check(creature: |) {} } }",
    ).includes("dc:"),
  );

  assert.ok(
    labels(
      "glyph g : interaction { tick { if skill_check(|) {} } }",
    ).includes("dc:"),
  );

  const result = complete(
    "glyph g : interaction { tick { if skill_check(player, dc|: 12) {} } }",
  );

  assert.equal(
    result.options.find((o) => o.label === "dc:").apply,
    "dc",
  );
});

test("locals obey block scope, declaration position, shadowing and foreach lifetime", () => {
  const source =
    "glyph g : interaction { tick { let outer = 1 if true { let inner = 2 | } let later = 3 } completed { let other = 4 } }";

  const names = labels(source);

  assert.ok(
    names.includes("outer") &&
      names.includes("inner"),
  );

  assert.ok(
    !names.includes("later") &&
      !names.includes("other"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { if true { let hidden = 1 } | } }",
    ).includes("hidden"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { let own = | } }",
    ).includes("own"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { let own = |",
    ).includes("own"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { let own = player.|",
    ).includes("own"),
  );

  assert.ok(
    labels(
      "glyph g : interaction { tick { foreach member in party.members { | } } }",
    ).includes("member"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { foreach member in party.members {} | } }",
    ).includes("member"),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { tick { foreach member in party.members { | } } }",
    ).includes("fail"),
  );

  assert.equal(
    labels(
      "glyph g : interaction { tick { let local = 1 if true { let local = 2 | } } }",
    ).filter((n) => n === "local").length,
    1,
  );
});

test("strings/comments suppress completion, including unfinished tokens", () => {
  for (const source of [
    "glyph g : interaction { tick { // player.|\n } }",
    'glyph g : interaction { tick { message(player, "hello |there") } }',
    'glyph g : interaction { tick { message(player, "unfinished|',
    'glyph g : interaction { tick { message(player, "escaped \\" |") } }',
  ]) {
    assert.equal(complete(source), null, source);
  }
});

test("event/stage snippets and incomplete programs remain useful without metadata", () => {
  assert.ok(
    labels("glyph g : encounter.|").includes(
      "encounter.before_group_spawn",
    ),
  );

  assert.ok(
    labels("glyph g : interaction { | }").includes(
      "attempted",
    ),
  );

  assert.ok(
    !labels(
      "glyph g : interaction { attempted {} | }",
    ).includes("attempted"),
  );

  // Member-mode labels are relative.
  assert.ok(
    labels(
      "glyph g : interaction { tick { player.|",
    ).includes("has_item"),
  );

  assert.ok(
    complete(
      "glyph g : interaction { tick { le|",
      true,
      null,
    ).options.some((o) => o.label === "let"),
  );

  assert.equal(
    complete(
      "glyph g : interaction { tick { |",
      false,
    ),
    null,
  );
});

test("diagnostic offsets preserve UTF-16/multiline ranges and clamp EOF spans", () => {
  const source =
    "glyph café : interaction {\n// 🐈\ninvalid()\n}";

  const start = source.indexOf("invalid");

  const result = compilerDiagnostics(
    [
      {
        code: "GLYPH2002",
        message: "Unknown function",
        span: {
          start,
          length: 7,
        },
      },
    ],
    source.length,
  );

  assert.equal(
    source.slice(result[0].from, result[0].to),
    "invalid",
  );

  assert.equal(result[0].severity, "error");

  assert.deepEqual(
    diagnosticRange(
      {
        start: source.length,
        length: 0,
      },
      source.length,
    ),
    {
      from: source.length,
      to: source.length,
    },
  );

  assert.deepEqual(
    diagnosticRange(
      {
        start: -1,
        length: 999,
      },
      10,
    ),
    {
      from: 0,
      to: 10,
    },
  );
});


test("descriptor-owned receiver and writable-state restrictions filter completion", () => {
  const catalog = {
    ...metadata,
    receiverMethods: [{ ...metadata.receiverMethods[0], name: "tick_only",
      availableIn: [{ event: "interaction", stage: "tick" }] }],
    writableState: [{ name: "new_state", type: "Int", setter: "set_new_state",
      availableIn: [{ event: "interaction", stage: "tick" }] }],
  };
  const attempted = complete("glyph g : interaction { attempted { player.| } }", true, catalog);
  assert.ok(!attempted.options.some(o => o.label === "tick_only"));
  const tick = complete("glyph g : interaction { tick { player.| } }", true, catalog);
  assert.ok(tick.options.some(o => o.label === "tick_only"));
  assert.ok(!complete("glyph g : interaction { attempted { | } }", true, catalog).options.some(o => o.label === "new_state"));
  const state = complete("glyph g : interaction { tick { | } }", true, catalog).options.find(o => o.label === "new_state");
  assert.equal(state.detail, "Int (writable)");
});


test("new pipeline event completions derive their stage list from metadata", () => {
  const catalog = { ...metadata, events: [...metadata.events,
    { name: "sample.pipeline", category: "Narrative", stages: ["tick"] }] };
  const result = complete("glyph g : sample.pipeline { | }", true, catalog);
  assert.deepEqual(result.options.map(o => o.label), ["tick"]);
});


test("mutable locals and new loop forms participate in lexical completion", () => {
  assert.ok(labels("glyph g : interaction { tick { var current = player current.| } }").includes("is_player"));
  for (const loop of ["while true", "for i in 0..3", "for item in party.members", "foreach item in party.members"]) {
    const options = labels(`glyph g : interaction { tick { ${loop} { | } } }`);
    assert.ok(options.includes("break"), loop);
    assert.ok(options.includes("continue"), loop);
  }
  assert.ok(labels("glyph g : interaction { tick { for item in party.members { item.| } } }").includes("is_player"));
  const outside = labels("glyph g : interaction { tick { if true { var hidden = player } | } }");
  assert.ok(!outside.includes("hidden"));
  assert.ok(!outside.includes("continue"));
  for (const keyword of ["var", "while", "for", "match"])
    assert.ok(outside.includes(keyword), keyword);
});
