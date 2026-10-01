import { hoverTooltip, activateHover, closeHoverTooltips, keymap } from '@codemirror/view';
import { resolveFunctionAt, signature, completionScope } from './glyph-completion.js';

export function functionDocumentation(metadata, fn) {
    return metadata?.documentation?.functions?.[fn?.documentationSource ?? fn?.source] ?? null;
}

// Imported content stays inert: every value is assigned to textContent, never innerHTML.
export function documentationDom(fn, article, open, unavailable = false) {
    const dom = document.createElement('div'); dom.className = 'glyph-documentation-tooltip';
    dom.setAttribute('role', 'tooltip');
    const add = (tag, text, cls) => {
        const element = document.createElement(tag); element.textContent = text;
        if (cls) element.className = cls;
        dom.append(element); return element;
    };
    add('pre', fn.name + signature(fn));
    add('p', article?.summary || fn.description);
    if (article && fn.backend?.toLowerCase().includes('adapter')) add('p', `Glyph binding: ${fn.description}`);
    if (fn.deprecated) add('p', `Deprecated: ${fn.deprecated}`);
    if (unavailable) add('p', 'Unavailable in current context');
    if (article) {
        if (fn.backend === 'NWScript.AssignCommand') add('p', 'Glyph supplies an explicit actor; the native action runs through AssignCommand.');
        else if (fn.backend?.toLowerCase().includes('adapter')) add('p', 'Glyph adapter: use the Glyph signature above. Native semantics are documented in the full reference.');
        for (const parameter of fn.parameters) {
            const native = article.parameters.find(p => p.name === parameter.sourceParameter);
            if (native) add('p', `${parameter.name}: ${native.description}`, 'glyph-documentation-parameter');
        }
        add('small', `NWN Lexicon · ${article.archiveDate} · revision ${article.revision} · GFDL 1.1 or later`);
    }
    if (open) {
        const button = add('button', 'Open full reference'); button.type = 'button';
        button.addEventListener('click', open);
    }
    return dom;
}

export function glyphDocumentation(metadata, callback) {
    const open = binding => callback.invokeMethodAsync('OnDocumentationRequested', binding.canonical).catch(() => {});
    const hover = hoverTooltip((view, pos, side) => {
        const binding = resolveFunctionAt(view.state, pos, metadata);
        if (!binding || pos === binding.from && side < 0 || pos === binding.to && side > 0) return null;
        const scope = completionScope(view.state, pos);
        const unavailable = metadata.events.some(e => e.name === scope.event) && !binding.fn.availableIn.some(a => a.event === scope.event && (!scope.stage || a.stage === scope.stage));
        return { pos: binding.from, end: binding.to, above: true,
            create: () => ({ dom: documentationDom(binding.fn, functionDocumentation(metadata, binding.fn), () => open(binding), unavailable) }) };
    }, { hoverTime: 300, hideOnChange: true });
    return [hover, keymap.of([
        { key: 'Escape', run: view => {
            if (!view.state.field(hover.active).length) return false;
            view.dispatch({ effects: closeHoverTooltips }); return true;
        } },
        { key: 'F1', run: view => {
            const binding = resolveFunctionAt(view.state, view.state.selection.main.head, metadata);
            if (!binding) return false;
            activateHover(view, view.state.selection.main.head, 1, { tooltip: hover, until: tr => tr.docChanged || tr.selection });
            open(binding); return true;
        } }
    ])];
}
