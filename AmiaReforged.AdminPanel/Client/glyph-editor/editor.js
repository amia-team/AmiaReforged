import { Compartment, EditorState } from '@codemirror/state';
import { EditorView, keymap, lineNumbers, highlightActiveLine, drawSelection } from '@codemirror/view';
import { defaultKeymap, history, historyKeymap } from '@codemirror/commands';

import { glyph } from './glyph-language.js';

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
    '.cm-cursor': { borderLeftColor: 'var(--text-primary, #d0ccc2)' }
}, { dark: true });

export function create(host, callback, source, readOnly) {
    destroy(host);
    const editable = new Compartment();
    const entry = { revision: 0, disposed: false, editable, view: null, observer: null };
    try {
        entry.view = new EditorView({ parent: host, state: EditorState.create({
            doc: source,
            extensions: [
                glyph(), lineNumbers(), history(), drawSelection(), highlightActiveLine(),
                keymap.of([...defaultKeymap, ...historyKeymap]), theme,
                EditorState.tabSize.of(4),
                EditorView.contentAttributes.of({ 'aria-label': 'Glyph source', 'spellcheck': 'false' }),
                editable.of([EditorState.readOnly.of(readOnly), EditorView.editable.of(!readOnly)]),
                EditorView.updateListener.of(update => {
                    if (!update.docChanged || entry.disposed) return;
                    const revision = ++entry.revision;
                    callback.invokeMethodAsync('OnEditorChanged', update.state.doc.toString(), revision)
                        .catch(() => { /* A disconnected Blazor circuit cannot receive edits. */ });
                })
            ]
        }) });
        editors.set(host, entry);
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
