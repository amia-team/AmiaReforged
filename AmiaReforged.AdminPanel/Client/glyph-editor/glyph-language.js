import { LRLanguage, LanguageSupport, HighlightStyle, syntaxHighlighting } from '@codemirror/language';
import { styleTags, tags } from '@lezer/highlight';
import { parser } from './glyph-parser.js';

export const glyphLanguage = LRLanguage.define({
    name: 'glyph',
    parser: parser.configure({ props: [styleTags({
        'glyph struct type let var fn const mod using pub impl': tags.definitionKeyword,
        'if else while for foreach in step match break continue return': tags.controlKeyword,
        'StageName/...': tags.keyword,
        'ProgramName/... ModuleName/...': tags.definition(tags.variableName),
        'EventName/... TypeName/... TypeParameters/... CollectionKind/... VariantName/...': tags.typeName,
        'BindingName/... VariableName/...': tags.variableName,
        'VariantPattern/Identifier': tags.typeName,
        'PropertyName/... ArgumentName/... FieldName/...': tags.propertyName,
        'CallExpression/VariableName/...': tags.function(tags.variableName),
        'CallExpression/MemberExpression/PropertyName/...': tags.function(tags.propertyName),
        FailKeyword: tags.function(tags.variableName),
        'Boolean/...': tags.bool,
        'WildcardPattern/...': tags.keyword,
        Number: tags.number,
        'String UnterminatedString': tags.string,
        LineComment: tags.lineComment,
        'RangeOperator/... AssignmentOperator/... MultiplyOperator CompareOperator EqualityOperator + - "!" && ||': tags.operator,
        '( )': tags.paren,
        '{ }': tags.brace,
        '[ ]': tags.squareBracket,
        '. , : ;': tags.punctuation
    })] }),
    languageData: { commentTokens: { line: '//' } }
});

export const glyphHighlightStyle = HighlightStyle.define([
    { tag: [tags.keyword, tags.definitionKeyword, tags.controlKeyword], class: 'glyph-keyword' },
    { tag: tags.definition(tags.variableName), class: 'glyph-definition' },
    { tag: tags.typeName, class: 'glyph-event' },
    { tag: [tags.function(tags.variableName), tags.function(tags.propertyName)], class: 'glyph-function' },
    { tag: tags.propertyName, class: 'glyph-property' },
    { tag: [tags.number, tags.bool], class: 'glyph-literal' },
    { tag: tags.string, class: 'glyph-string' },
    { tag: tags.lineComment, class: 'glyph-comment' },
    { tag: tags.operator, class: 'glyph-operator' },
    { tag: [tags.punctuation, tags.bracket], class: 'glyph-punctuation' }
]);

export function glyph() {
    return new LanguageSupport(glyphLanguage, syntaxHighlighting(glyphHighlightStyle));
}
