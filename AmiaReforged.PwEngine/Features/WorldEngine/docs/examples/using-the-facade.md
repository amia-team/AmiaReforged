# Using `IWorldEngineFacade` from another Anvil service

The façade is the single injectable entry point to every subsystem and the CQRS dispatchers. Inject it in any Anvil-bound class and everything else hangs off it.

Source: [`IWorldEngineFacade`](../../IWorldEngineFacade.cs), [`WorldEngineFacade`](../../WorldEngineFacade.cs).

## Inject and go

```csharp
using AmiaReforged.PwEngine.Features.WorldEngine;
using Anvil.Services;

[ServiceBinding(typeof(MyFeatureService))]
public sealed class MyFeatureService
{
    private readonly IWorldEngineFacade _world;

    public MyFeatureService(IWorldEngineFacade world) => _world = world;

    public async Task DoSomethingAsync(CharacterId characterId)
    {
        // Direct subsystem access
        var traits = await _world.Traits.GetCharacterTraitsAsync(characterId);

        // Dispatch a query through the façade
        var recipes = await _world.QueryAsync<GetAvailableRecipesQuery, List<Recipe>>(
            new GetAvailableRecipesQuery
            {
                CharacterId = characterId,
                IndustryTag = new IndustryTag("smithing")
            });

        // Dispatch a command
        var result = await _world.ExecuteAsync(new AddRecipeToIndustryCommand
        {
            IndustryTag = new IndustryTag("smithing"),
            Recipe      = someRecipe
        });

        if (!result.Success)
        {
            // Handle result.ErrorMessage
        }
    }
}
```

## Rules of thumb

- **Prefer the dispatchers** (`ExecuteAsync` / `QueryAsync`) for anything that already has a command or query handler. It keeps side-effects routed through the event bus and gives consistent error reporting.
- **Use subsystem properties** (`_world.Economy`, `_world.Organizations`, …) when the operation has no CQRS wrapper or belongs to a sub-façade like `_world.Economy.Shops`.
- **Pass value objects**, never raw GUIDs or strings (`CharacterId`, `OrganizationId`, `IndustryTag`, …). The compiler will stop you from mixing up identifiers.
- **Batch when you can**. Use `ExecuteBatchAsync` with [`BatchExecutionOptions`](../../SharedKernel/Commands/BatchExecutionOptions.cs) for multi-command workflows.

## Resolving the façade inside a controller

Route handlers get the service provider on `RouteContext.Services`:

```csharp
[HttpGet("/api/worldengine/example/whoami/{id}")]
public static async Task<ApiResult> WhoAmI(RouteContext ctx)
{
    var world = ctx.Services!.GetRequiredService<IWorldEngineFacade>();
    var persona = await world.Personas.GetPersonaAsync(
        PersonaId.FromCharacter(new CharacterId(Guid.Parse(ctx.GetRouteValue("id")))));

    return persona is null
        ? new ApiResult(404, new ErrorResponse("Not found", "No persona"))
        : new ApiResult(200, persona);
}
```

## Avoid

- Injecting individual subsystems **and** the façade into the same class — pick one.
- Calling `new CommandDispatcher(...)` or `new WorldEngineFacade(...)` by hand. Let Anvil construct them.
- Holding an `NwCreature` or NWN object reference beyond a single script frame — use `CharacterId` plus a lookup when you need it.
