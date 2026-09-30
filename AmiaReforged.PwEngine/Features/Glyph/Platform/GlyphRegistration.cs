using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Context;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

/// <summary>Compile-time opt-in. Public CLR members are never automatically exposed to Glyph.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GlyphNodeAttribute : Attribute
{
    public bool Automatic { get; init; } = true;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class GlyphModuleAttribute : Attribute;

public interface IGlyphModule
{
    void Configure(GlyphModuleBuilder glyph);
}

/// <summary>Supports direct construction and factories that close over normally injected dependencies.</summary>
public sealed class GlyphModuleBuilder
{
    private readonly List<IGlyphNodeExecutor> _executors = [];
    public void Add<T>() where T : IGlyphNodeExecutor, new() => Add(new T());
    public void Add(IGlyphNodeExecutor executor) => _executors.Add(executor);
    public void Add(Func<IGlyphNodeExecutor> factory) => Add(factory());

    public List<IGlyphNodeExecutor> Build()
    {
        List<IGlyphNodeExecutor> executors = [.. _executors];
        foreach (IContextNodeProvider provider in _executors.OfType<IContextNodeProvider>())
            foreach (ContextPinDescriptor pin in provider.GetContextPins())
                executors.Add(new ContextGetterExecutor(provider.SourceTypeId, provider.SourceDisplayName,
                    pin, provider.SourceEventType, provider.SourceScriptCategory));
        if (executors.Select(e => e.TypeId).Distinct(StringComparer.Ordinal).Count() != executors.Count)
            throw new InvalidOperationException("Duplicate Glyph executor TypeId in module registration.");
        return executors.OrderBy(e => e.TypeId, StringComparer.Ordinal).ToList();
    }
}
