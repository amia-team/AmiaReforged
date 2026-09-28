import { ExternalTokenizer } from '@lezer/lr';
import { Identifier } from './glyph-parser.terms.js';

// GlyphLexer uses char.IsLetter/IsLetterOrDigit on UTF-16 code units.
const letter = /^[\p{L}_]$/u;
const continuation = /^[\p{L}\p{Nd}_]$/u;
export const identifiers = new ExternalTokenizer(input => {
    if (input.next < 0 || !letter.test(String.fromCharCode(input.next))) return;
    input.advance();
    while (input.next >= 0 && continuation.test(String.fromCharCode(input.next))) input.advance();
    input.acceptToken(Identifier);
});
