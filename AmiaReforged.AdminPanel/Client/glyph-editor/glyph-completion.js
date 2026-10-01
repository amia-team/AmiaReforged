import { ensureSyntaxTree, syntaxTree } from "@codemirror/language";
import { snippetCompletion } from "@codemirror/autocomplete";

function inside(node, pos, closing) {
  return (
    node.from < pos &&
    (pos < node.to || (pos === node.to && node.lastChild?.name !== closing))
  );
}
function text(state, node) {
  return node ? state.sliceDoc(node.from, node.to) : "";
}

// Node names that may stand as the receiver of a member access (`receiver.`).
const EXPR_NODES = new Set([
  "VariableName",
  "Number",
  "String",
  "UnterminatedString",
  "Boolean",
  "ParenthesizedExpression",
  "UnaryExpression",
  "BinaryExpression",
  "MemberExpression",
  "IndexExpression",
  "CallExpression",
]);

export function completionScope(state, pos, from = pos, suppress = true) {
  const tree = ensureSyntaxTree(state, pos, 50) || syntaxTree(state);
  const leaf = tree.resolveInner(pos, -1);
  for (let node = leaf; node; node = node.parent) {
    if (
      suppress && node.from < pos &&
      pos <= node.to &&
      (node.name === "LineComment" ||
        node.name === "UnterminatedString" ||
        (node.name === "String" &&
          (pos < node.to || state.sliceDoc(node.to - 1, node.to) !== '"')))
    )
      return { suppressed: true };
  }
  const event = text(state, tree.topNode.getChild("EventName"));
  let functionNode = null;
  for (let node = leaf; node; node = node.parent) if (node.name === "FunctionDeclaration") { functionNode = node; break; }
  const inFunction = !!functionNode;
  const functionBody = functionNode?.getChild("Block");
  const inFunctionBody = !!functionBody && inside(functionBody, pos, "}");
  const functionReturnType = text(state, functionNode?.getChild("TypeName"));
  const moduleBody = tree.topNode.getChild("ModuleDeclaration")?.getChild("ModuleBody");
  const blocks = [];
  let argumentsNode = null,
    inLoop = false,
    stage = null;
  tree.iterate({
    enter(ref) {
      const node = ref.node;
      if (node.from > pos) return false;
      if (node.name === "Block" && inside(node, pos, "}")) {
        blocks.push(node);
        if (node.parent?.name === "StageDeclaration")
          stage = text(state, node.parent.getChild("StageName"));
        if (["ForeachStatement", "ForStatement", "WhileStatement"].includes(node.parent?.name)) inLoop = true;
      }
      if (node.name === "ArgumentList" && inside(node, pos, ")"))
        argumentsNode = node;
    },
  });
  const locals = new Map();
  for (const parameter of functionNode?.getChild("Parameters")?.getChildren("Parameter") || []) {
    const name = text(state, parameter.getChild("ParameterName"));
    if (name) locals.set(name, { label: name, type: text(state, parameter.getChild("TypeName")), detail: "Function parameter", boost: 20 });
  }
  for (const block of blocks) {
    if (["ForeachStatement", "ForStatement"].includes(block.parent?.name)) {
      const name = text(state, block.parent.getChild("BindingName"));
      // Derive the element from the compiler-owned list return type.
      if (name)
        locals.set(name, {
          label: name,
          type: null,
          init: firstExprChild(block.parent),
          elementOf: !block.parent.getChild("RangeOperator"),
          range: !!block.parent.getChild("RangeOperator"),
          detail: "Loop variable",
          boost: 20,
        });
    }
    for (let child = block.firstChild; child; child = child.nextSibling) {
      if (!["LetStatement", "VarStatement"].includes(child.name) || child.to > from) continue;
      let incomplete = false;
      child.toTree().iterate({
        enter(ref) {
          if (ref.type.isError) incomplete = true;
        },
      });
      if (incomplete) continue;
      const name = text(state, child.getChild("BindingName"));
      if (!name) continue;
      // Record the initializer so the receiver type can be derived from metadata.
      locals.set(name, {
        label: name,
        type: null,
        detail: child.name === "VarStatement" ? "Mutable local" : "Local binding",
        boost: 20,
        init: letInitializer(child),
      });
    }
  }
  return {
    event,
    stage,
    inLoop,
    inFunction,
    inFunctionBody,
    functionReturnType,
    moduleBody,
    blocks,
    argumentsNode,
    locals: [...locals.values()],
    localsByName: locals,
    tree,
  };
}

const functionSnippetCompletion = snippetCompletion("fn ${name}(${value}: ${Int}): ${Int} {\n\treturn ${value}\n}", { label: "fn", type: "keyword" });
const statementSnippets = [
  snippetCompletion("var ${name} = ${value}", { label: "var", type: "keyword" }),
  snippetCompletion("while ${condition} {\n\t${}\n}", { label: "while", type: "keyword" }),
  snippetCompletion("for ${item} in ${values} {\n\t${}\n}", { label: "for", type: "keyword" }),
  snippetCompletion("match ${value} {\n\t_ {\n\t\t${}\n\t}\n}", { label: "match", type: "keyword" }),
  snippetCompletion("let ${name} = ${value}", {
    label: "let",
    type: "keyword",
  }),
  snippetCompletion("if ${condition} {\n\t${}\n}", {
    label: "if",
    type: "keyword",
  }),
  snippetCompletion("else {\n\t${}\n}", { label: "else", type: "keyword" }),
  snippetCompletion("foreach ${member} in ${objects} {\n\t${}\n}", {
    label: "foreach",
    type: "keyword",
  }),
];
export function signature(fn) {
  return `(${fn.parameters.map((p) => `${p.name}: ${p.type}${p.required ? "" : ` = ${p.defaultValue}`}`).join(", ")}) → ${fn.returnType}`;
}
function available(fn, scope) {
  return (
    !scope.event ||
    fn.availableIn.some(
      (s) =>
        s.event === scope.event && (!scope.stage || s.stage === scope.stage),
    )
  );
}

function letInitializer(letNode) {
  const eq = letNode.getChild("=");
  if (!eq) return null;
  for (let child = eq.nextSibling; child; child = child.nextSibling) {
    if (EXPR_NODES.has(child.name)) return child;
  }
  return null;
}

// A focused, completion-only expression-type resolver. It mirrors just enough of the binder to
// derive the Glyph type of a receiver expression; unknown expressions resolve to null.
function localType(scope, name) {
  return scope.localsByName.get(name)?.type ?? null;
}
function contextField(metadata, scope, name) {
  const ctx = (metadata?.contexts || []).find(
    (c) => c.event === scope.event && c.stage === scope.stage,
  );
  return ctx?.fields.find((f) => f.name === name) ?? null;
}
function receiverMethod(metadata, name, type) {
  return (metadata?.receiverMethods || []).find((r) => r.name === name && r.receiverType === type) ?? null;
}
function functionByName(metadata, name) {
  return (
    (metadata?.functions || []).find(
      (fn) => fn.name === name || fn.canonicalName === name,
    ) ?? null
  );
}
function memberReceiver(memberNode) {
  return memberNode.firstChild;
}
function memberName(state, memberNode) {
  return text(state, memberNode.getChild("PropertyName"));
}
function firstExprChild(node) {
  for (let child = node.firstChild; child; child = child.nextSibling) {
    if (EXPR_NODES.has(child.name)) return child;
  }
  return null;
}
function expressionType(state, node, scope, metadata) {
  if (!node || !EXPR_NODES.has(node.name)) return null;
  switch (node.name) {
    case "Number":
      return text(state, node).includes(".") ? "Float" : "Int";
    case "String":
    case "UnterminatedString":
      return "String";
    case "Boolean":
      return "Bool";
    case "VariableName":
      return (
        localType(scope, text(state, node)) ??
        contextField(metadata, scope, text(state, node))?.type ??
        null
      );
    case "ParenthesizedExpression":
      return expressionType(state, firstExprChild(node), scope, metadata);
    case "MemberExpression": {
      const name = text(state, node);
      const local = localType(scope, name);
      if (local != null) return local;
      const receiverType = expressionType(state, memberReceiver(node), scope, metadata);
      const field = (metadata?.aggregates || []).find(a => a.name === receiverType)?.fields?.find(f => f.name === memberName(state, node));
      return field?.typeName ?? contextField(metadata, scope, name)?.type ?? (metadata?.constants || []).find(c => c.name === name)?.type ?? null;
    }
    case "IndexExpression":
      return null;
    case "CallExpression": {
      const callee = node.firstChild;
      if (!callee) return null;
      const direct = functionByName(metadata, text(state, callee));
      if (direct) return direct.returnType;
      if (callee.name === "MemberExpression") {
        const type = expressionType(state, memberReceiver(callee), scope, metadata);
        const rm = receiverMethod(metadata, memberName(state, callee), type);
        if (rm) {
          const receiverType = expressionType(
            state,
            memberReceiver(callee),
            scope,
            metadata,
          );
          return receiverType === rm.receiverType ? rm.returnType : null;
        }
      }
      return functionByName(metadata, text(state, callee))?.returnType ?? null;
    }
    default:
      return null;
  }
}

// The expression node (VariableName/MemberExpression/CallExpression/…) that ends exactly at the
// dot, i.e. the receiver of `<expression>.`.
function findReceiverNode(tree, dotPos) {
  if (!tree) return null;
  let best = null;
  tree.iterate({
    enter(ref) {
      const node = ref.node;
      if (node.from >= dotPos) return false;
      if (node.to === dotPos && EXPR_NODES.has(node.name)) best = node;
    },
  });
  return best;
}

function fieldCompletion(field) {
  return {
    label: field.name,
    type: "property",
    detail: field.type,
    info: field.description,
  };
}
export function functionSnippet(fn, name = fn.name) {
  return `${name}(${fn.parameters.filter(p => p.required).map(p => "${" + p.name + "}").join(", ")})`;
}

function functionCompletion(fn, context) {
  const followsParen = /^\s*\(/.test(context.state.sliceDoc(context.pos));
  const template = functionSnippet(fn);
  const completion = {
    label: fn.name,
    type: "function",
    detail: signature(fn),
    info: fn.description,
  };
  return followsParen
    ? { ...completion, apply: fn.name }
    : snippetCompletion(template, completion);
}

// Receiver-member completion replaces only the text after the final dot so the receiver text is
// preserved and the popup shows the bare member name.
function receiverCompletion(rm, context) {
  const followsParen = /^\s*\(/.test(context.state.sliceDoc(context.pos));
  const template = functionSnippet(rm);

  const completion = {
    label: rm.name,
    type: "function",
    detail: signature(rm),
    info: rm.description,
  };

  return followsParen
    ? { ...completion, apply: rm.name }
    : snippetCompletion(template, completion);
}

// Typed member completion: offer receiver methods whose ReceiverType matches the receiver
// expression's Glyph type, plus namespace-style `receiver.` completions (context fields, static
// functions) for untyped prefixes such as `context.` or `nwn.`.
function memberCompletions(context, word, scope, functions, fields, metadata) {
  const state = context.state;
  const lastDot = word.text.lastIndexOf(".");
  const receiverText = word.text.slice(0, lastDot);
  const memberPrefix = word.text.slice(lastDot + 1);
  const dotPos = word.from + lastDot;

  const receiverNode = findReceiverNode(scope.tree, dotPos);
  const receiverType = receiverNode
    ? expressionType(state, receiverNode, scope, metadata)
    : null;

  const completions = [];

  if (receiverType) {
    for (const rm of metadata?.receiverMethods || []) {
      if (
        rm.receiverType === receiverType &&
        (!metadata?.events?.some(e => e.name === scope.event) || available(rm, scope)) &&
        rm.name.startsWith(memberPrefix)
      ) {
        completions.push(receiverCompletion(rm, context));
      }
    }
  }

  const namespacePrefix = receiverText + ".";

  for (const fn of functions) {
    if (fn.name.startsWith(namespacePrefix)) {
      const memberName = fn.name.slice(namespacePrefix.length);

      if (memberName.startsWith(memberPrefix)) {
        completions.push(
          functionCompletion(
            {
              ...fn,
              name: memberName,
            },
            context,
          ),
        );
      }
    }
  }

  for (const constant of metadata?.constants || []) {
    if (constant.name.startsWith(namespacePrefix)) {
      const name = constant.name.slice(namespacePrefix.length);
      if (name.startsWith(memberPrefix)) completions.push({ label: name, type: "constant",
        detail: `${constant.type} = ${constant.value}`, info: `${constant.description} (${constant.source})`, apply: name });
    }
  }

  for (const field of fields) {
    if (field.name.startsWith(namespacePrefix)) {
      const memberName = field.name.slice(namespacePrefix.length);

      if (memberName.startsWith(memberPrefix)) {
        completions.push(
          fieldCompletion({
            ...field,
            name: memberName,
          }),
        );
      }
    }
  }

  const aggregate = (metadata?.aggregates || []).find(a => a.name === receiverType);
  for (const field of aggregate?.fields || []) completions.push({ label: field.name, type: "property", detail: field.typeName });
  return {
    from: dotPos + 1,
    options: completions,
  };
}

// Resolve the parameter list of the call enclosing an argument cursor, stripping the injected
// receiver for receiver-method calls so `origin` is never suggested.
function callBinding(call, state, scope, metadata) {
  if (!call) return null;
  const callee = call.firstChild;
  if (!callee) return null;
  const direct = functionByName(metadata, text(state, callee));
  if (direct) return { name: direct.name, signature: signature(direct), parameters: direct.parameters, description: direct.description };
  if (callee.name === "MemberExpression") {
    const type = expressionType(state, memberReceiver(callee), scope, metadata);
    const rm = receiverMethod(metadata, memberName(state, callee), type);
    if (rm)
      return {
        name: rm.name,
        signature: signature(rm),
        parameters: rm.parameters,
        description: rm.description,
      };
  }
  const fn = functionByName(metadata, text(state, callee));
  if (fn)
    return {
      name: fn.name,
      signature: signature(fn),
      parameters: fn.parameters,
      description: fn.description,
    };
  return null;
}

export function glyphCompletions(metadata) {
  return (context) => {
    const word = context.matchBefore(/[\p{L}\p{Nd}_.]*/u);
    const from = word?.from ?? context.pos;
    const scope = completionScope(context.state, context.pos, from);

    if (scope.suppressed || /^\p{Nd}/u.test(word?.text || "")) return null;

    const prefix = context.state.sliceDoc(0, from);
    const argumentStart =
      scope.argumentsNode && /[(,]\s*$/.test(prefix);

    if (!word?.text && !context.explicit && !argumentStart) return null;

    let options = [];
    const body = scope.tree.topNode.getChild("Block") || scope.moduleBody;

    if (/\busing\s+[\p{L}\p{Nd}_]*$/u.test(context.state.sliceDoc(0, context.pos))) {
      options = (metadata?.modules || []).map(name => ({ label: name, type: "namespace", detail: "Published module" }));
    } else if (
      (!body || context.pos <= body.from) &&
      /\bglyph\s+[\p{L}\p{Nd}_]+\s*:\s*[\p{L}\p{Nd}_.]*$/u.test(
        context.state.sliceDoc(0, context.pos),
      )
    ) {
      options = (metadata?.events || []).map((e) => ({
        label: e.name,
        type: "type",
        detail: e.category,
      }));
    } else if (/\b(?:[\p{L}_][\p{L}\p{Nd}_]*)\s*:\s*[\p{L}\p{Nd}_.]*$/u.test(context.state.sliceDoc(0, context.pos)) && !scope.argumentsNode) {
      options = (metadata?.types || []).map(name => ({ label: name, type: "type" }));
    } else if ((!body || context.pos <= body.from) && !scope.inFunction) {
      if (!prefix.trim()) {
        options = [
          functionSnippetCompletion,
          snippetCompletion("mod ${name} {\n\t${}\n}", { label: "mod", type: "keyword" }),
          snippetCompletion("using ${module}", { label: "using", type: "keyword" }),
          snippetCompletion(
            "glyph ${name} : ${interaction} {\n\t${}\n}",
            {
              label: "glyph",
              type: "keyword",
            },
          ),
        ];
      }
    } else if (scope.moduleBody && !scope.inFunction) {
      options = [functionSnippetCompletion, ...["pub", "const", "struct", "type", "using"].map(label => ({ label, type: "keyword" }))];
    } else if (
      (scope.event === "interaction" || metadata?.events.find(e => e.name === scope.event)?.stages?.length > 0) &&
      !scope.stage && !scope.inFunction &&
      scope.blocks.length === 1
    ) {
      const declared = new Set(
        body
          .getChildren("StageDeclaration")
          .map((n) =>
            text(context.state, n.getChild("StageName")),
          ),
      );

      options = (
        metadata?.events.find((e) => e.name === scope.event)
          ?.stages || [
          "attempted",
          "started",
          "tick",
          "completed",
        ]
      )
        .filter((stage) => !declared.has(stage))
        .map((stage) =>
          snippetCompletion(`${stage} {\n\t\${}\n}`, {
            label: stage,
            type: "keyword",
          }),
        );
    } else {
      const knownEvent = metadata?.events.some(
        (e) => e.name === scope.event,
      );

      const functions = (metadata?.functions || []).filter(
        (f) =>
          (!knownEvent || available(f, scope)) &&
          !(scope.inLoop && f.canonicalName === "fail"),
      );

      const fields = (metadata?.contexts || [])
        .filter(
          (c) =>
            c.event === scope.event &&
            c.stage === scope.stage,
        )
        .flatMap((c) => c.fields);

      // Derive local binding types from their initializers now that
      // metadata is available.
      resolveLocalTypes(context.state, scope, metadata);

      // Member completion owns its own replacement range.
      //
      // Example:
      //
      //   context.creature.get_
      //                    ^ result.from
      //
      // CodeMirror should filter against "get_", not the entire
      // "context.creature.get_" expression.
      if (word?.text.includes(".")) {
        const memberResult = memberCompletions(
          context,
          word,
          scope,
          functions,
          fields,
          metadata,
        );

        // Property shorthand wins over a same-named call.
        const unique = new Map();

        for (const option of memberResult.options) {
          const previous = unique.get(option.label);

          if (
            !previous ||
            (option.boost || 0) > (previous.boost || 0)
          ) {
            unique.set(option.label, option);
          }
        }

        return {
          from: memberResult.from,
          options: [...unique.values()],
        };
      }

      options.push(
        ...fields.map((f) => ({
          label: f.name,
          type: "property",
          detail: f.type,
          info: f.description,
        })),
      );

      for (const fn of functions) options.push(functionCompletion(fn, context));

      if (functions.some(fn => fn.name.startsWith("nwn."))) {
        options.push({ label: "nwn", type: "namespace", detail: "NWN procedures", apply: "nwn.", boost: 10 });
      }

      for (const constant of metadata?.constants || []) if (!constant.name.includes(".")) options.push({ label: constant.name, type: "constant", detail: constant.type, info: constant.description });
      for (const aggregate of metadata?.aggregates || []) if (!aggregate.name.includes(".")) options.push({ label: aggregate.name, type: "type" });
      for (const domain of metadata?.constantDomains || []) options.push({ label: domain.name, type: "namespace", detail: `${domain.count} constants`, apply: domain.name + "." });
      options.push(
        ...scope.locals,
        { label: "true", type: "keyword" },
        { label: "false", type: "keyword" },
      );

      if (!scope.argumentsNode && (!scope.inFunction || scope.inFunctionBody)) {
        options.push(...statementSnippets);
        if (scope.inFunctionBody) options.push(scope.functionReturnType === "Void"
          ? { label: "return", type: "keyword", apply: "return;" }
          : snippetCompletion("return ${value}", { label: "return", type: "keyword" }));
        for (const state of metadata?.writableState || []) {
          if (!knownEvent || available(state, scope)) {
            options.push({ label: state.name, type: "variable", detail: `${state.type} (writable)`,
              info: `Assignment calls ${state.setter}` });
          }
        }

        if (scope.inLoop) {
          options.push({ label: "break", type: "keyword" }, { label: "continue", type: "keyword" });
        }
      } else if (scope.argumentsNode) {
        const call = scope.argumentsNode.parent;
        const binding = callBinding(
          call,
          context.state,
          scope,
          metadata,
        );

        if (binding) {
          const used = new Set();
          let positional = 0;

          for (const arg of scope.argumentsNode.getChildren(
            "Argument",
          )) {
            const name = arg.getChild("ArgumentName");

            if (name && name.to < from) {
              used.add(text(context.state, name));
            } else if (!name && arg.to < from) {
              used.add(
                binding.parameters[positional++]?.name,
              );
            }
          }

          // Named parameters only belong at the beginning
          // of an argument.
          if (argumentStart) {
            options.unshift(
              ...binding.parameters
                .filter((p) => !used.has(p.name))
                .map((p) => ({
                  label: p.name + ":",
                  type: "property",
                  boost: 30,
                  apply: /^\s*:/.test(
                    context.state.sliceDoc(context.pos),
                  )
                    ? p.name
                    : p.name + ": ",
                  detail: `${p.type}${
                    p.required
                      ? " (required)"
                      : ` = ${p.defaultValue}`
                  }`,
                  info:
                    `${binding.name}${binding.signature}\n` +
                    binding.description,
                })),
            );
          }
        }
      }
    }

    // Property shorthand wins over a same-named call;
    // locals win over context fields.
    const unique = new Map();

    for (const option of options) {
      const previous = unique.get(option.label);

      if (
        !previous ||
        (option.boost || 0) > (previous.boost || 0)
      ) {
        unique.set(option.label, option);
      }
    }

    return {
      from,
      options: [...unique.values()],
    };
  };
}

// Shared syntax/type lookup for completion and documentation. Unknown receivers remain unknown.
function resolveLocalTypes(state, scope, metadata) {
  for (const local of scope.locals) {
    if (local.init) {
      const type = expressionType(state, local.init, scope, metadata);
      local.type = local.range ? "Int" : local.elementOf ? /^List<(.+)>$/.exec(type || "")?.[1] ?? null : type;
    }
  }
}

export function resolveFunctionAt(state, pos, metadata) {
  if (!metadata) return null;
  const scope = completionScope(state, pos);
  if (scope.suppressed) return null;
  let leaf = scope.tree.resolveInner(pos, -1);
  for (let node = leaf; node; node = node.parent)
    if (["LineComment", "String", "UnterminatedString"].includes(node.name)) return null;
  resolveLocalTypes(state, scope, metadata);
  for (let node = leaf; node; node = node.parent) {
    if (!["VariableName", "MemberExpression"].includes(node.name) || pos < node.from || pos > node.to) continue;
    const direct = functionByName(metadata, text(state, node));
    if (direct) return { from: node.from, to: node.to, fn: direct, canonical: direct.canonicalName };
    if (node.name === "MemberExpression") {
      const property = node.getChild("PropertyName");
      if (!property || pos < property.from || pos > property.to) continue;
      const type = expressionType(state, memberReceiver(node), scope, metadata);
      const member = receiverMethod(metadata, memberName(state, node), type);
      if (member) {
        const target = functionByName(metadata, member.canonicalName);
        return { from: property.from, to: property.to,
          fn: { ...target, ...member, name: `${member.receiverType}.${member.name}` }, canonical: member.canonicalName };
      }
    }
  }
  return null;
}
