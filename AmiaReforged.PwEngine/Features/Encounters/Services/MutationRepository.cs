using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.Encounters.Services;

[ServiceBinding(typeof(IMutationRepository))]
public class MutationRepository : IMutationRepository
{
    private readonly IDbContextFactory<PwEngineContext> _factory;

    public MutationRepository(IDbContextFactory<PwEngineContext> factory)
    {
        _factory = factory;
    }

    // === Template Operations ===

    public async Task<List<MutationTemplate>> GetAllAsync()
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.MutationTemplates
            .Include(m => m.Effects)
            .OrderBy(m => m.Prefix)
            .ToListAsync();
    }

    public async Task<List<MutationTemplate>> GetAllActiveAsync()
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.MutationTemplates
            .Include(m => m.Effects)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Prefix)
            .ToListAsync();
    }

    public async Task<MutationTemplate?> GetByIdAsync(Guid id)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.MutationTemplates
            .Include(m => m.Effects)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MutationTemplate> CreateAsync(MutationTemplate template)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.MutationTemplates.Add(template);
        await ctx.SaveChangesAsync();
        return template;
    }

    public async Task<MutationTemplate> UpdateAsync(MutationTemplate template)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.MutationTemplates.Update(template);
        await ctx.SaveChangesAsync();
        return template;
    }

    public async Task DeleteAsync(Guid id)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        MutationTemplate? template = await ctx.MutationTemplates.FindAsync(id);
        if (template != null)
        {
            ctx.MutationTemplates.Remove(template);
            await ctx.SaveChangesAsync();
        }
    }

    // === Effect Operations ===

    public async Task<MutationEffect?> GetEffectByIdAsync(Guid effectId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        return await ctx.MutationEffects.FindAsync(effectId);
    }

    public async Task<MutationEffect> AddEffectAsync(Guid templateId, MutationEffect effect)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        effect.MutationTemplateId = templateId;
        ctx.MutationEffects.Add(effect);
        await ctx.SaveChangesAsync();
        return effect;
    }

    public async Task<MutationEffect> UpdateEffectAsync(MutationEffect effect)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        ctx.MutationEffects.Update(effect);
        await ctx.SaveChangesAsync();
        return effect;
    }

    public async Task DeleteEffectAsync(Guid effectId)
    {
        await using PwEngineContext ctx = await _factory.CreateDbContextAsync();
        MutationEffect? effect = await ctx.MutationEffects.FindAsync(effectId);
        if (effect != null)
        {
            ctx.MutationEffects.Remove(effect);
            await ctx.SaveChangesAsync();
        }
    }
}
