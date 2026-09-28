import { ensureSyntaxTree, syntaxTree } from '@codemirror/language';
import { snippetCompletion } from '@codemirror/autocomplete';

function inside(node, pos, closing) {
    return node.from < pos && (pos < node.to || (pos === node.to && node.lastChild?.name !== closing));
}
function text(state, node) { return node ? state.sliceDoc(node.from, node.to) : ''; }

export function completionScope(state, pos, from = pos) {
    const tree = ensureSyntaxTree(state, pos, 50) || syntaxTree(state);
    const leaf = tree.resolveInner(pos, -1);
    for (let node = leaf; node; node = node.parent) {
        if (node.from < pos && pos <= node.to && (
            node.name === 'LineComment' || node.name === 'UnterminatedString' ||
            (node.name === 'String' && (pos < node.to || state.sliceDoc(node.to - 1, node.to) !== '"'))))
            return { suppressed: true };
    }
    const event = text(state, tree.topNode.getChild('EventName'));
    const blocks = [];
    let argumentsNode = null, inLoop = false, stage = null;
    tree.iterate({ enter(ref) {
        const node = ref.node;
        if (node.from > pos) return false;
        if (node.name === 'Block' && inside(node, pos, '}')) {
            blocks.push(node);
            if (node.parent?.name === 'StageDeclaration') stage = text(state, node.parent.getChild('StageName'));
            if (node.parent?.name === 'ForeachStatement') inLoop = true;
        }
        if (node.name === 'ArgumentList' && inside(node, pos, ')')) argumentsNode = node;
    } });
    const locals = new Map();
    for (const block of blocks) {
        if (block.parent?.name === 'ForeachStatement') {
            const name = text(state, block.parent.getChild('BindingName'));
            if (name) locals.set(name, { label: name, type: 'variable', detail: 'Loop variable', boost: 20 });
        }
        for (let child = block.firstChild; child; child = child.nextSibling) {
            if (child.name !== 'LetStatement' || child.to > from) continue;
            let incomplete = false;
            child.toTree().iterate({ enter(ref) { if (ref.type.isError) incomplete = true; } });
            if (incomplete) continue;
            const name = text(state, child.getChild('BindingName'));
            if (name) locals.set(name, { label: name, type: 'variable', detail: 'Local binding', boost: 20 });
        }
    }
    return { event, stage, inLoop, blocks, argumentsNode, locals: [...locals.values()], tree };
}

const statementSnippets = [
    snippetCompletion('let ${name} = ${value}', { label: 'let', type: 'keyword' }),
    snippetCompletion('if ${condition} {\n\t${}\n}', { label: 'if', type: 'keyword' }),
    snippetCompletion('else {\n\t${}\n}', { label: 'else', type: 'keyword' }),
    snippetCompletion('foreach ${member} in ${objects} {\n\t${}\n}', { label: 'foreach', type: 'keyword' })
];
function signature(fn) {
    return `(${fn.parameters.map(p => `${p.name}: ${p.type}${p.required ? '' : ` = ${p.defaultValue}`}`).join(', ')}) → ${fn.returnType}`;
}
function available(fn, scope) {
    return !scope.event || fn.availableIn.some(s => s.event === scope.event && (!scope.stage || s.stage === scope.stage));
}

export function glyphCompletions(metadata) {
    return context => {
        const word = context.matchBefore(/[\p{L}\p{Nd}_.]*/u);
        const from = word?.from ?? context.pos;
        const scope = completionScope(context.state, context.pos, from);
        if (scope.suppressed || /^\p{Nd}/u.test(word?.text || '')) return null;
        const prefix = context.state.sliceDoc(0, from);
        const argumentStart = scope.argumentsNode && /[(,]\s*$/.test(prefix);
        if (!word?.text && !context.explicit && !argumentStart) return null;
        let options = [];
        const body = scope.tree.topNode.getChild('Block');
        if ((!body || context.pos <= body.from) && /:\s*[\p{L}\p{Nd}_.]*$/u.test(context.state.sliceDoc(0, context.pos))) {
            options = (metadata?.events || []).map(e => ({ label: e.name, type: 'type', detail: e.category }));
        } else if (!body || context.pos <= body.from) {
            if (!prefix.trim()) options = [snippetCompletion('glyph ${name} : ${interaction} {\n\t${}\n}', { label: 'glyph', type: 'keyword' })];
        } else if (scope.event === 'interaction' && !scope.stage && scope.blocks.length === 1) {
            const declared = new Set(body.getChildren('StageDeclaration').map(n => text(context.state, n.getChild('StageName'))));
            options = (metadata?.events.find(e => e.name === 'interaction')?.stages || ['attempted', 'started', 'tick', 'completed'])
                .filter(stage => !declared.has(stage))
                .map(stage => snippetCompletion(`${stage} {\n\t\${}\n}`, { label: stage, type: 'keyword' }));
        } else {
            const knownEvent = metadata?.events.some(e => e.name === scope.event);
            const functions = (metadata?.functions || []).filter(f => (!knownEvent || available(f, scope)) && !(scope.inLoop && f.canonicalName === 'fail'));
            const fields = (metadata?.contexts || []).filter(c => c.event === scope.event && c.stage === scope.stage).flatMap(c => c.fields);
            options.push(...fields.map(f => ({ label: f.name, type: 'property', detail: f.type, info: f.description })));
            for (const fn of functions) {
                const followsParen = /^\s*\(/.test(context.state.sliceDoc(context.pos));
                const required = fn.parameters.filter(p => p.required);
                const template = `${fn.name}(${required.map(p => '${' + p.name + '}').join(', ')})`;
                const completion = { label: fn.name, type: 'function', detail: signature(fn), info: fn.description };
                options.push(followsParen ? { ...completion, apply: fn.name } : snippetCompletion(template, completion));
            }
            options.push(...scope.locals, { label: 'true', type: 'keyword' }, { label: 'false', type: 'keyword' });
            if (!scope.argumentsNode) {
                options.push(...statementSnippets);
                if (scope.inLoop) options.push({ label: 'break', type: 'keyword' });
            } else {
                const call = scope.argumentsNode.parent;
                const fn = functions.find(f => f.name === text(context.state, call.firstChild));
                if (fn) {
                    const used = new Set();
                    let positional = 0;
                    for (const arg of scope.argumentsNode.getChildren('Argument')) {
                        const name = arg.getChild('ArgumentName');
                        if (name && name.to < from) used.add(text(context.state, name));
                        else if (!name && arg.to < from) used.add(fn.parameters[positional++]?.name);
                    }
                    // Named parameters only belong at the beginning of an argument.
                    if (argumentStart) options.unshift(...fn.parameters.filter(p => !used.has(p.name)).map(p => ({
                        label: p.name + ':', type: 'property', boost: 30,
                        apply: /^\s*:/.test(context.state.sliceDoc(context.pos)) ? p.name : p.name + ': ',
                        detail: `${p.type}${p.required ? ' (required)' : ` = ${p.defaultValue}`}`,
                        info: `${fn.name}${signature(fn)}\n${fn.description}`
                    })));
                }
            }
        }
        const memberPrefix = word?.text.includes('.') ? word.text.slice(0, word.text.lastIndexOf('.') + 1) : null;
        if (memberPrefix) options = options.filter(o => o.label.startsWith(memberPrefix));
        // Property shorthand wins over a same-named call; locals win over context fields.
        const unique = new Map();
        for (const option of options) {
            const previous = unique.get(option.label);
            if (!previous || (option.boost || 0) > (previous.boost || 0)) unique.set(option.label, option);
        }
        return { from, options: [...unique.values()] };
    };
}
