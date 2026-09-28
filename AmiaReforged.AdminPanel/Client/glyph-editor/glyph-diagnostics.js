// Compiler and CodeMirror offsets both count UTF-16 code units. Use offsets,
// rather than columns, so multiline and non-ASCII diagnostics select precisely.
export function diagnosticRange(span, length) {
    const from = Math.max(0, Math.min(length, span.start));
    return { from, to: Math.max(from, Math.min(length, from + Math.max(0, span.length))) };
}
export function compilerDiagnostics(diagnostics, length) {
    return diagnostics.map(d => ({ ...diagnosticRange(d.span, length), severity: 'error',
        source: d.code, message: d.message }));
}
