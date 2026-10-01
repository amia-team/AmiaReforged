import { Compartment, EditorState, EditorSelection } from '@codemirror/state';
import { EditorView, keymap, lineNumbers, highlightActiveLine, drawSelection } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap } from '@codemirror/commands';

import { autocompletion, closeCompletion, snippet } from '@codemirror/autocomplete';
import { lintGutter, setDiagnostics, setDiagnosticsEffect } from '@codemirror/lint';
import { glyphCompletions, completionScope, functionSnippet } from './glyph-completion.js';
import { compilerDiagnostics, diagnosticRange } from './glyph-diagnostics.js';
import { glyph } from './glyph-language.js';
import { glyphDocumentation } from './glyph-documentation.js';

const editors = new WeakMap();
const theme = EditorView.theme({
    '&': { backgroundColor: 'var(--bg-input, #33302a)', color: 'var(--text-primary, #d0ccc2)', border: '1px solid var(--text-secondary, #8a8477)' },
    '&.cm-focused': { outline: '2px solid var(--accent-gold, #c9a84c)' },
    '.cm-scroller': { fontFamily: 'monospace', overflow: 'auto' },
    '.cm-content': { minHeight: '24rem', caretColor: 'var(--text-primary, #d0ccc2)' },
    '.cm-gutters': { backgroundColor: 'var(--bg-card, #26241f)', color: 'var(--text-secondary, #8a8477)', border: 'none' },
    '.cm-activeLine': { backgroundColor: '#ffffff08' },
    '&.cm-focused .cm-selectionBackground, .cm-selectionBackground': { backgroundColor: '#75643280' },
    '.glyph-keyword': { color: '#ddc06a' },
    '.glyph-definition': { color: '#dcdcaa' },
    '.glyph-event': { color: '#8cd4c3' },
    '.glyph-function': { color: '#8dcbea' },
    '.glyph-property': { color: '#b9d7ed' },
    '.glyph-literal': { color: '#c5a5e8' },
    '.glyph-string': { color: '#b6d792' },
    '.glyph-comment': { color: '#a7a18f', fontStyle: 'italic' },
    '.glyph-operator': { color: '#e5b992' },
    '.glyph-punctuation': { color: '#d0ccc2' },
    '.cm-tooltip': { backgroundColor: 'var(--bg-card, #26241f)', color: 'var(--text-primary, #d0ccc2)' },
    '.cm-tooltip-autocomplete ul li[aria-selected]': { backgroundColor: '#756432', color: '#fff' },
    '.cm-cursor': { borderLeftColor: 'var(--text-primary, #d0ccc2)' }
}, { dark: true });

export function create(host, callback, source, readOnly) {
    destroy(host);
    const editable = new Compartment();
    const completion = new Compartment();
    const documentation = new Compartment();
    const entry = { revision: 0, disposed: false, editable, completion, documentation, callback, view: null, observer: null, context: null, contextRevision: 0 };
    try {
        entry.view = new EditorView({ parent: host, state: EditorState.create({
            doc: source,
            extensions: [
                glyph(), lintGutter(), documentation.of([]),
                completion.of(autocompletion({ override: [glyphCompletions(null)] })),
                EditorState.transactionExtender.of(transaction => transaction.docChanged
                    ? { effects: setDiagnosticsEffect.of([]) } : null),
                lineNumbers(), history(), drawSelection(), highlightActiveLine(),
                keymap.of([...defaultKeymap, ...historyKeymap]), theme,
                EditorState.tabSize.of(4),
                EditorView.contentAttributes.of({ 'aria-label': 'Glyph source', 'spellcheck': 'false' }),
                editable.of([EditorState.readOnly.of(readOnly), EditorView.editable.of(!readOnly)]),
                EditorView.updateListener.of(update => {
                    if (entry.disposed) return;
                    if (update.docChanged || update.selectionSet) notifyContext(entry, callback);
                    if (!update.docChanged) return;
                    const revision = ++entry.revision;
                    callback.invokeMethodAsync('OnEditorChanged', update.state.doc.toString(), revision)
                        .catch(() => { /* A disconnected Blazor circuit cannot receive edits. */ });
                })
            ]
        }) });
        editors.set(host, entry);
        notifyContext(entry, callback);
        // DOM removal also cleans up when the server circuit cannot call DisposeAsync.
        entry.observer = new MutationObserver(() => {
            if (!host.isConnected) destroy(host);
        });
        entry.observer.observe(document.body, { childList: true, subtree: true });
    } catch (error) {
        entry.view?.destroy();
        entry.observer?.disconnect();
        editors.delete(host);
        throw error;
    }
}

export function setReadOnly(host, readOnly) {
    const entry = editors.get(host);
    if (!entry) return;
    if (readOnly) closeCompletion(entry.view);
    entry.view.dispatch({ effects: entry.editable.reconfigure([
        EditorState.readOnly.of(readOnly), EditorView.editable.of(!readOnly)
    ]) });
}

export function capture(host) {
    const entry = editors.get(host);
    if (!entry) throw new Error('Glyph editor is no longer available.');
    // Freeze input in the same browser turn as the snapshot, before any API work.
    setReadOnly(host, true);
    return { source: entry.view.state.doc.toString(), revision: entry.revision };
}

export function destroy(host) {
    const entry = editors.get(host);
    if (!entry) return;
    entry.disposed = true;
    entry.observer?.disconnect();
    entry.view.destroy();
    editors.delete(host);
}

export function setMetadata(host, metadata) {
    const entry = editors.get(host);
    if (!entry) return;
    entry.standardMetadata = metadata;
    configureMetadata(entry);
}

export function setModuleMetadata(host, metadata) {
    const entry = editors.get(host);
    if (!entry) return;
    entry.moduleMetadata = metadata;
    configureMetadata(entry);
}

function configureMetadata(entry) {
    const standard = entry.standardMetadata;
    const modules = entry.moduleMetadata;
    // The cached documentation pack stays in the browser; only the small module overlay changes.
    const metadata = standard && modules ? { ...standard,
        functions: [...new Map([...(standard.functions || []), ...(modules.functions || [])].map(f => [f.name, f])).values()],
        constants: [...(standard.constants || []), ...(modules.constants || [])],
        types: [...new Set([...(standard.types || []), ...(modules.types || [])])],
        aggregates: modules.aggregates || [], modules: modules.modules || [], sourceLocations: modules.sourceLocations || {}
    } : standard;
    closeCompletion(entry.view);
    entry.view.dispatch({ effects: [entry.completion.reconfigure(autocompletion({
        override: [glyphCompletions(metadata)]
    })), entry.documentation.reconfigure(metadata ? glyphDocumentation(metadata, entry.callback) : [])] });
}

export function showDiagnostics(host, source, diagnostics) {
    const entry = editors.get(host);
    if (!entry || entry.view.state.doc.toString() !== source) return;
    entry.view.dispatch(setDiagnostics(entry.view.state, compilerDiagnostics(diagnostics, entry.view.state.doc.length)));
}

export function focusDiagnostic(host, source, span) {
    const entry = editors.get(host);
    if (!entry || entry.view.state.doc.toString() !== source) return;
    const { from, to } = diagnosticRange(span, entry.view.state.doc.length);
    entry.view.dispatch({ selection: EditorSelection.single(from, to), effects: EditorView.scrollIntoView(from) });
    entry.view.focus();
}

function notifyContext(entry, callback) {
    const scope = completionScope(entry.view.state, entry.view.state.selection.main.head, undefined, false);
    const context = { event: scope.event || null, stage: scope.stage || null };
    const key = JSON.stringify(context);
    if (key === entry.context) return;
    entry.context = key;
    callback.invokeMethodAsync('OnCursorContextChanged', context, ++entry.contextRevision).catch(() => {});
}

export function insertFunction(host, fn) {
    const entry = editors.get(host);
    if (!entry || entry.view.state.readOnly) return;
    closeCompletion(entry.view);
    const { from, to } = entry.view.state.selection.main;
    snippet(functionSnippet(fn))(entry.view, { label: fn.name }, from, to);
    entry.view.focus();
}

export function insertConstant(host, constant) {
    const entry = editors.get(host);
    if (!entry || entry.view.state.readOnly) return;
    closeCompletion(entry.view);
    entry.view.dispatch({ ...entry.view.state.replaceSelection(constant.name), scrollIntoView: true, userEvent: 'input' });
    entry.view.focus();
}

export { setReferenceSearch, setReferenceSearchState, destroyReferenceSearch, clearReferenceSearch } from './glyph-reference.js';
