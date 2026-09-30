// Opt-in integration test against JSON emitted by tools/Glyph.Docs, not a frontend catalog.
import { test } from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { EditorState } from "@codemirror/state";
import { CompletionContext } from "@codemirror/autocomplete";
import { glyph } from "./glyph-language.js";
import { glyphCompletions } from "./glyph-completion.js";

const metadata = JSON.parse(readFileSync(process.env.GLYPH_METADATA_PATH, "utf8"));
function complete(source) {
  const pos = source.indexOf("|");
  const state = EditorState.create({ doc: source.replace("|", ""), extensions: [glyph()] });
  return glyphCompletions(metadata)(new CompletionContext(state, pos, true))?.options || [];
}
function source(scope, body) {
  return `glyph probe : ${scope.event} { ${scope.stage ? `${scope.stage} { ${body} }` : body} }`;
}

test("every compiler-owned function completes with its description and signature", () => {
  for (const fn of metadata.functions) {
    for (const scope of fn.availableIn) {
      const parts = fn.name.split(".");
      const prefix = parts.length === 1 ? "|" : parts.slice(0, -1).join(".") + ".|";
      const label = parts.at(-1);
      const result = complete(source(scope, prefix));
      const option = result.find(o => o.label === (parts.length === 1 ? fn.name : label));
      assert.ok(option, `${fn.name}/${scope.event}/${scope.stage}`);
      // Property shorthand may override a callable spelling, by existing editor policy.
      if (option.type === "function") {
        assert.equal(option.info, fn.description);
        assert.ok(option.detail.includes(`→ ${fn.returnType}`));
        for (const param of fn.parameters) assert.ok(option.detail.includes(`${param.name}: ${param.type}`));
      }
    }
  }
});

test("receivers, context, writable state and stage filtering consume live compiler metadata", () => {
  for (const context of metadata.contexts) {
    for (const field of context.fields.filter(f => f.name.startsWith("context."))) {
      const option = complete(source(context, "context.|" )).find(o => o.label === field.name.slice(8));
      assert.ok(option, `${context.event}/${context.stage}/${field.name}`);
      assert.equal(option.detail, field.type);
    }
  }
  for (const receiver of metadata.receiverMethods) {
    for (const scope of receiver.availableIn) {
      const context = metadata.contexts.find(c => c.event === scope.event && c.stage === scope.stage);
      const object = context.fields.find(f => f.name.startsWith("context.") && f.type === receiver.receiverType);
      const option = complete(source(scope, `${object.name}.|`)).find(o => o.label === receiver.name);
      assert.ok(option, receiver.name);
      assert.equal(option.info, receiver.description);
    }
  }
  for (const state of metadata.writableState) {
    for (const scope of state.availableIn) assert.ok(complete(source(scope, "|")).some(o => o.label === state.name));
  }
  const attempted = complete("glyph p : interaction { attempted { | } }");
  assert.ok(!attempted.some(o => o.label === "set_progress" || o.label === "status"));
  assert.ok(!complete("glyph p : interaction { attempted { context.| } }").some(o => o.label === "session_id"));
  assert.ok(complete("glyph p : interaction { | }").some(o => o.label === "tick"));
});
