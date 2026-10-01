# Reusable Glyph modules

Modules require language version 2. Existing version 1 scripts and published histories
remain supported; the editor retains a legacy script's version until a module import is added. Generated standard functions, constants, context, and documentation
are automatically available; an authored `global.glyph` is not required.

## Define a library

Each library source contains one module. Its identifier is the name used by imports.
Names are case-sensitive, unique per server, and cannot use a system namespace such as
`nwn`. The name of a saved module is stable; create another module to rename its interface.

```glyph
mod interaction_helpers {
    const TARGET_TAG = "todo"

    fn find_target(actor: Object): Object =
        nwn.get_nearest_object_by_tag(TARGET_TAG, actor)

    pub fn prospecting_target(actor: Object): Object = find_target(actor)
    pub const EMPTY_MESSAGE = "There is nothing to prospect."
}
```

Every declaration is private unless marked `pub`. An exported function can call private
helpers and read private constants in its own module. Its body sees its parameters and
its declaring module's symbols, rather than the caller's local variables.

Functions retain Glyph's existing expression-body syntax: `fn name(args): Type = expression`.
They are expanded by the compiler into existing executable IR. Block-bodied functions,
recursion, and runtime initialization are not supported.

## Import a module

```glyph
using interaction_helpers

glyph prospect : interaction {
    attempted {
        let target = prospecting_target(context.creature)
        if target == OBJECT.INVALID {
            fail EMPTY_MESSAGE
        }
        nwn.set_local_object(context.creature, "prospecting_target", target)
    }
}
```

Unique public names can be used directly. Qualified names also work:
`interaction_helpers.prospecting_target(context.creature)` and
`interaction_helpers.EMPTY_MESSAGE`. Access requires a `using` declaration even when
qualified. Private declarations such as `interaction_helpers.find_target` are unavailable.

A module may declare its own imports. They remain local to that module; consumers must
explicitly import another module to name its declarations. Imports can occur in any
order. Duplicate imports, missing published modules, and import cycles are diagnosed.

If two imports export the same name, qualify it when using it. Existing local and standard
names retain their meaning, so an imported declaration that collides with either needs
qualification. The editor offers published module names and visible imported symbols,
including function signatures, context availability, and module revision origins.

## Export structs and ADTs

```glyph
mod prospect_types {
    pub struct Prospect {
        target: Object
    }

    pub type Result {
        Found { prospect: Prospect }
        Missing { reason: String }
    }

    pub fn found(actor: Object): Result = Result.Found(Prospect(actor))
}
```

Public structs expose all their fields. Public ADTs expose all variants and payload fields.
Public signatures, fields, and variant payloads cannot expose private types.
Field-level visibility is not part of this version.

Consumers can name `prospect_types.Prospect` in function signatures and call
`prospect_types.Result.Missing(reason: "No target")`. ADT matching accepts either
unqualified variant names or qualified names such as `prospect_types.Result.Found`.
Types have module identity: `a.Result` and `b.Result` are distinct even if their fields match.

Functions can read context and use registered intrinsics where available, but imports
cannot bypass event, stage, type, or loop restrictions. Prefer explicit parameters for
helpers that should work across multiple events. Module validation checks every function,
including unused private functions, against the real supported event/stage contexts.

## Save, publish, and update

1. Open **Glyph scripts → Manage modules**, enter a module name, and create a draft.
2. Add declarations, save the draft, and select **Compile / validate**.
3. Select **Publish module**. A saved draft is not importable until published.
4. Add `using module_name` to a script, validate it, and activate it.

Each publication creates an immutable module revision and pins its imported revisions.
Scripts likewise retain their complete dependency closure on activation, including the
module source needed for restart. Updating a module affects future compilations; active
scripts and executions in progress keep their existing behavior. Republish a dependent
module explicitly to adopt a newer dependency, then recompile and activate its consumers.

Validation returns a compilation fingerprint. Publication and activation reject changed
source or dependencies with a revalidation error. A graph requiring two revisions of the
same module is rejected; republish its dependents against one revision before retrying.

Module rollback selects the preceding published revision for future imports. Script
rollback restores its retained executable and dependency closure. Archiving prevents
new direct imports while retaining revisions used by published modules and scripts.
The API exposes archive/restore operations rather than deletion of retained revisions.

`SourceHash` remains the hash of the root source. `CompilationHash` includes language
version and the selected module revision/hash entries. Module diagnostics and runtime
source maps retain the module name, revision, line, and column. Expansion diagnostics
also identify the call sites leading to an invalid helper.

## Validate local files

Keep standalone libraries in an explicit module directory:

```sh
dotnet run --project tools/Glyph.Cli -- \
  --module-root AmiaReforged.PwEngine/Features/Glyph/Language/Examples/Modules \
  AmiaReforged.PwEngine/Features/Glyph/Language/Examples/prospect_with_module.glyph
```

The CLI discovers `.glyph` libraries under that directory by their `mod` declarations,
validates all libraries, and uses the same resolver/binder as the server. It does not start
NWN or a database. A standalone library may also be passed as a file for validation.
Use `--language-version 1` when validating legacy source that uses the new keywords as identifiers.
The server's library store is managed through the AdminPanel/API, not a runtime filesystem loader.

A source is limited to 128 KiB. A dependency closure allows at most 64 modules, depth 32,
and 1 MiB of module source. Constant dependency depth is limited to 128, function expansion
depth to 64, and binding to 65,536 expressions. Existing expression and 4,096-operation
executable limits still apply.

## Server API

Modules use `/api/worldengine/glyph-modules` for listing and draft creation, and
`/{id}` for reading/editing. `/compile` validates a named source; `/{id}/publish` accepts
source and `ExpectedCompilationHash`; `/{id}/rollback` selects the preceding revision.
`DELETE /{id}` archives a module, and an update with `IsArchived: false` restores it.

`POST /api/worldengine/glyphs/module-metadata` supplies document-scoped visible symbols.
The generated standard catalog and NWScript Lexicon remain on the existing cached
`/language-metadata` endpoint. Compile and activation responses expose dependency
references and compilation fingerprints. Script activation with modules requires the
fingerprint from validation.

The `GlyphModules` EF migration adds module drafts and retained publication history.
Published script history stores immutable dependency snapshots alongside existing fields;
legacy histories without dependencies restore with an empty lock.
