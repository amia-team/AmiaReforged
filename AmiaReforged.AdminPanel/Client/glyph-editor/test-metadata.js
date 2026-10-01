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
    fn("nwn.nearest_object_by_kind", [parameter("origin", "Object"), parameter("type", "String")], scopes, "Object"),
    fn("nwn.is_player", [parameter("origin", "Object")], scopes, "Bool"),
    fn("nwn.get_distance_between", [parameter("object_a", "Object"), parameter("object_b", "Object")], scopes, "Float"),
    fn("nwn.get_location", [parameter("object", "Object")], scopes, "Location"),
    fn("nwn.get_ability_score", [parameter("object", "Object"), parameter("ability", "Int")], scopes, "Int"),
    fn("nwn.set_local_int", [parameter("object", "Object"), parameter("name", "String"), parameter("value", "Int")]),
    fn("nwn.action_attack", [parameter("actor", "Object"), parameter("target", "Object")]),
    fn("nwn.inventory", [parameter("object", "Object")], scopes, "List<Object>"),
    fn("effect.haste", [], scopes, "Effect"),
    fn("nwn.location_x", [parameter("location", "Location")], scopes, "Float"),
    fn("fail", [parameter("message", "String")]),
  ],
  contexts: scopes.map((scope) => ({
    ...scope,
    fields: [
      { name: "player", type: "Object", description: "Player" },
      { name: "creature", type: "Object", description: "Creature" },
      { name: "context.creature", type: "Object", description: "The interaction creature" },
      { name: "context.character_id", type: "String", description: "Character ID" },
      { name: "party.size", type: "Int", description: "Party size" },
      { name: "party.members", type: "List<Object>", description: "Party members" },
      ...(scope.stage === "attempted" ? [] : [{ name: "context.session_id", type: "String", description: "Session ID" }]),
    ],
  })),
  receiverMethods: [
    {
      name: "get_x", receiverType: "Location", policy: "LanguageValue",
      canonicalName: "nwn.location_x", description: "Coordinate of a typed Location.",
      returnType: "Float", kind: "Value", parameters: [], availableIn: scopes,
    },
    {
      name: "get_effect_type", receiverType: "Effect", policy: "LanguageValue",
      canonicalName: "nwn.get_effect_type", description: "Type of a typed Effect.",
      returnType: "Int", kind: "Value", parameters: [], availableIn: scopes,
    },
  ],
  indexers: [{ name: "metadata", getter: "metadata", setter: "set_metadata" }],
};
