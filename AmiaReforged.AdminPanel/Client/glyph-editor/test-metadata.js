// Representative wire-format fixture. Server tests verify catalog and binder parity.
const stages = ['attempted', 'started', 'tick', 'completed'];
const scopes = stages.map(stage => ({ event: 'interaction', stage }));
const parameter = (name, type, required = true, defaultValue = null) => ({ name, type, required, defaultValue });
const fn = (name, parameters, availableIn = scopes, returnType = 'Void') => ({
    name, canonicalName: name, parameters, availableIn, returnType,
    description: `Documentation for ${name}`, kind: returnType === 'Void' ? 'Action' : 'Value'
});
export const metadata = {
    languageVersion: 1,
    events: [
        { name: 'interaction', category: 'Interaction', stages },
        { name: 'encounter.before_group_spawn', category: 'Encounter', stages: [] }
    ],
    functions: [
        fn('message', [parameter('creature', 'Object'), parameter('message', 'String')]),
        fn('player.has_item', [parameter('tag', 'String')], scopes, 'Bool'),
        fn('skill_check', [parameter('creature', 'Object'), parameter('skill', 'String', false, '"Lore"'), parameter('dc', 'Int', false, '10')], scopes, 'Bool'),
        fn('set_progress', [parameter('value', 'Int')], scopes.filter(s => ['started', 'tick'].includes(s.stage))),
        fn('spawn.cancel', [], [{ event: 'encounter.before_group_spawn', stage: null }]),
        fn('fail', [parameter('message', 'String')])
    ],
    contexts: scopes.map(scope => ({ ...scope, fields: [
        { name: 'player', type: 'Object', description: 'Player' },
        { name: 'creature.hp', type: 'Int', description: 'Hit points' },
        { name: 'context.character_id', type: 'String', description: 'Character ID' },
        ...(scope.stage === 'attempted' ? [] : [{ name: 'context.session_id', type: 'String', description: 'Session ID' }])
    ] })),
    indexers: [{ name: 'metadata', getter: 'metadata', setter: 'set_metadata' }]
};
