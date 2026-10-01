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
  // Object handles expose deliberate domain aliases. Member-mode labels are relative to the final dot.
  const playerMembers = labels(
    "glyph g : interaction { attempted { player.| } }",
  );

  assert.ok(playerMembers.includes("has_item"));
  assert.ok(!playerMembers.includes("get_nearest_object_by_type"));
  assert.ok(!playerMembers.includes("is_player"));
  assert.ok(!playerMembers.includes("get_distance"));

  const creatureMembers = labels(
    "glyph g : interaction { tick { creature.| } }",
  );

  assert.ok(!creatureMembers.includes("hp"));
  assert.ok(!creatureMembers.includes("get_nearest_object_by_type"));
  assert.ok(!creatureMembers.includes("is_player"));
  assert.ok(!creatureMembers.includes("get_distance"));

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

test("only typed values receive member completions; nwn exposes procedures", () => {
  for (const expression of ["player", "context.creature", "nwn.nearest_object_by_kind(player, \"creature\")"]) {
    const names = labels(`glyph g : interaction { tick { (${expression}).| } }`);
    for (const removed of ["get_distance", "is_player", "get_ability_score", "set_local_int", "action_attack"])
      assert.ok(!names.includes(removed), expression + "." + removed);
  }
  assert.ok(!labels("glyph g : interaction { tick { Object.| } }").includes("is_player"));
  const namespace = complete("glyph g : interaction { tick { nw| } }").options.find(o => o.label === "nwn");
  assert.equal(namespace.type, "namespace");
  assert.equal(namespace.apply, "nwn.");
  const procedures = labels("glyph g : interaction { tick { nwn.| } }");
  for (const name of ["get_distance_between", "get_location", "get_ability_score", "set_local_int", "action_attack"])
    assert.ok(procedures.includes(name), name);
  const source = "glyph g : interaction { tick { let loc = nwn.get_location(player) loc.get_| } }";
  const partial = complete(source);
  assert.ok(partial.options.some(o => o.label === "get_x"));
  assert.equal(completionQuery(source, partial), "get_");
  assert.ok(labels("glyph g : interaction { tick { nwn.get_location(player).| } }").includes("get_x"));
  assert.ok(labels("glyph g : interaction { tick { let aura = effect.haste() aura.| } }").includes("get_effect_type"));
  assert.ok(!labels("glyph g : interaction { tick { player.| } }").includes("get_x"));
});

test("typed receiver arguments hide the receiver; command arguments include actor", () => {
  const coordinate = labels("glyph g : interaction { tick { let loc = nwn.get_location(player) loc.get_x(|) } }");
  assert.ok(!coordinate.includes("location:"));
  const action = labels("glyph g : interaction { tick { nwn.action_attack(|) } }");
  assert.ok(action.includes("actor:"));
  assert.ok(action.includes("target:"));
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
  const attempted = complete("glyph g : interaction { attempted { nwn.get_location(player).| } }", true, catalog);
  assert.ok(!attempted.options.some(o => o.label === "tick_only"));
  const tick = complete("glyph g : interaction { tick { nwn.get_location(player).| } }", true, catalog);
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
  assert.ok(labels("glyph g : interaction { tick { var current = nwn.get_location(player) current.| } }").includes("get_x"));
  for (const loop of ["while true", "for i in 0..3", "for item in party.members", "foreach item in party.members"]) {
    const options = labels(`glyph g : interaction { tick { ${loop} { | } } }`);
    assert.ok(options.includes("break"), loop);
    assert.ok(options.includes("continue"), loop);
  }
  assert.ok(!labels("glyph g : interaction { tick { for item in party.members { item.| } } }").includes("is_player"));
  const outside = labels("glyph g : interaction { tick { if true { var hidden = player } | } }");
  assert.ok(!outside.includes("hidden"));
  assert.ok(!outside.includes("continue"));
  for (const keyword of ["var", "while", "for", "match"])
    assert.ok(outside.includes(keyword), keyword);
});
