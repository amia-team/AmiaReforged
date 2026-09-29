// Representative wire-format fixture. Server tests verify catalog and binder parity.
const stages = ["attempted", "started", "tick", "completed"];
const scopes = stages.map((stage) => ({ event: "interaction", stage }));
const parameter = (name, type, required = true, defaultValue = null) => ({ name, type, required, defaultValue });
const fn = (name, parameters, availableIn = scopes, returnType = "Void") => ({
  name,
  canonicalName: name,
  parameters,
  availableIn,
  returnType,
  description: `Documentation for ${name}`,
  kind: returnType === "Void" ? "Action" : "Value",
});
export const metadata = {
  languageVersion: 1,
  events: [
    { name: "interaction", category: "Interaction", stages },
    { name: "encounter.before_group_spawn", category: "Encounter", stages: [] },
  ],
  functions: [
    fn("message", [parameter("creature", "Object"), parameter("message", "String")]),
    fn("player.has_item", [parameter("tag", "String")], scopes, "Bool"),
    fn(
      "skill_check",
      [parameter("creature", "Object"), parameter("skill", "String", false, '"Lore"'), parameter("dc", "Int", false, "10")],
      scopes,
      "Bool",
    ),
    fn(
      "set_progress",
      [parameter("value", "Int")],
      scopes.filter((s) => ["started", "tick"].includes(s.stage)),
    ),
    fn("spawn.cancel", [], [{ event: "encounter.before_group_spawn", stage: null }]),
    fn("Object.nearest_object_by_type", [parameter("origin", "Object"), parameter("type", "String")], scopes, "Object"),
    fn("Object.is_player", [parameter("origin", "Object")], scopes, "Bool"),
    fn("Object.get_distance", [parameter("object_a", "Object"), parameter("object_b", "Object")], scopes, "Float"),
    fn("fail", [parameter("message", "String")]),
  ],
  contexts: scopes.map((scope) => ({
    ...scope,
    fields: [
      { name: "player", type: "Object", description: "Player" },
      { name: "creature", type: "Object", description: "Creature" },
      { name: "context.creature", type: "Object", description: "The interaction creature" },
      { name: "creature.hp", type: "Int", description: "Hit points" },
      { name: "context.character_id", type: "String", description: "Character ID" },
      { name: "party.size", type: "Int", description: "Party size" },
      { name: "party.members", type: "List<Object>", description: "Party members" },
      ...(scope.stage === "attempted" ? [] : [{ name: "context.session_id", type: "String", description: "Session ID" }]),
    ],
  })),
  receiverMethods: [
    {
      name: "get_nearest_object_by_type",
      receiverType: "Object",
      canonicalName: "Object.nearest_object_by_type",
      description: "Get the nearest object of a curated type.",
      returnType: "Object",
      kind: "Value",
      parameters: [parameter("type", "String")],
      availableIn: scopes,
    },
    {
      name: "is_player",
      receiverType: "Object",
      canonicalName: "Object.is_player",
      description: "Whether the object is a player.",
      returnType: "Bool",
      kind: "Value",
      parameters: [],
      availableIn: scopes,
    },
    {
      name: "get_distance",
      receiverType: "Object",
      canonicalName: "Object.get_distance",
      description: "Returns the distance in meters between this object and another object.",
      returnType: "Float",
      kind: "Value",
      parameters: [parameter("object_b", "Object")],
      availableIn: scopes,
    },
  ],
  indexers: [{ name: "metadata", getter: "metadata", setter: "set_metadata" }],
};
