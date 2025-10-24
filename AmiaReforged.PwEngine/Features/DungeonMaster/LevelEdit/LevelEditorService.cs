using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.DungeonMaster.AreaEdit;
using AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit.AreaEdit;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LevelEdit;

[ServiceBinding(typeof(LevelEditorService))]
public sealed class LevelEditorService
{
    private readonly ConcurrentDictionary<string, LevelEditSession> _sessions = new();

    public LevelEditSession GetOrCreateSessionForArea(NwArea area)
    {
        if (area is null) throw new ArgumentNullException(nameof(area));

        string key = area.ResRef;
        return _sessions.GetOrAdd(key, _ => new LevelEditSession(area));
    }

    public bool TryGetSessionForArea(NwArea area, out LevelEditSession? session)
    {
        if (area is null)
        {
            session = null;
            return false;
        }

        return _sessions.TryGetValue(area.ResRef, out session);
    }

    public IEnumerable<LevelEditSession> ActiveSessions => _sessions.Values;
}

public sealed class LevelEditSession
{
    public LevelEditSession(NwArea area)
    {
        Area = area ?? throw new ArgumentNullException(nameof(area));
        State = new AreaEditorState
        {
            SelectedArea = area
        };
    }

    public NwArea Area { get; }
    public AreaEditorState State { get; }

    private readonly List<IScryPresenter> _openPresenters = new();

    public void RegisterPresenter(IScryPresenter presenter)
    {
        if (presenter == null) return;
        if (!_openPresenters.Contains(presenter)) _openPresenters.Add(presenter);
    }

    public void UnregisterPresenter(IScryPresenter presenter)
    {
        if (presenter == null) return;
        _openPresenters.Remove(presenter);
    }

    public IReadOnlyList<IScryPresenter> OpenPresenters => _openPresenters;
}
