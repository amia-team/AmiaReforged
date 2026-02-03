using Anvil.API;

namespace AmiaReforged.PwEngine.Features.CharacterTools.VfxTools;

public sealed class VfxToolModel
{
    private readonly NwPlayer _player;
    private readonly bool _isDm;
    private NwGameObject? _targetObject;
    private Location? _targetLocation;
    private int _currentVfxId;
    private List<int> _validVfxIds = new();

    public VfxToolModel(NwPlayer player, bool isDm, NwGameObject? initialTarget = null)
    {
        _player = player;
        _isDm = isDm;
        _targetObject = initialTarget;

        InitializeValidVfxIds();
    }

    private void InitializeValidVfxIds()
    {
        // Build list of valid VFX IDs from the visual effect table
        _validVfxIds = NwGameTables.VisualEffectTable.Rows
            .Where(row => !string.IsNullOrEmpty(row.Label))
            .Select(row => row.RowIndex)
            .ToList();

        if (_validVfxIds.Count > 0)
        {
            _currentVfxId = _validVfxIds[0];
        }
    }

    // ...existing code...

    public void SetTarget(NwGameObject? target)
    {
        _targetObject = target;
        _targetLocation = null; // Clear location when object is set
    }

    public void SetTargetLocation(Location location)
    {
        if (!_isDm)
        {
            _player.SendServerMessage("Only DMs can target locations.", ColorConstants.Orange);
            return;
        }

        _targetLocation = location;
        _targetObject = null; // Clear object when location is set
    }

    public bool IsLocationTarget()
    {
        return _targetLocation != null;
    }

    public NwGameObject? GetTarget()
    {
        return _targetObject;
    }

    public void FindNearestVfxDoll()
    {
        if (_player.ControlledCreature == null) return;

        // Find nearest creature with tag "vfx_doll"
        var dollsNearby = _player.ControlledCreature.Area?.Objects
            .Where(o => o is NwCreature && o.Tag == "vfx_doll")
            .OrderBy(o => _player.ControlledCreature.Distance(o))
            .FirstOrDefault();

        _targetObject = dollsNearby as NwGameObject;
    }

    public int GetCurrentVfxId()
    {
        return _currentVfxId;
    }

    public string GetCurrentVfxLabel()
    {
        VisualEffectTableEntry? entry = NwGameTables.VisualEffectTable[_currentVfxId];
        return entry?.Label ?? "UNKNOWN";
    }

    public void NextVfx()
    {
        if (_validVfxIds.Count == 0) return;

        int currentIndex = _validVfxIds.IndexOf(_currentVfxId);
        currentIndex = (currentIndex + 1) % _validVfxIds.Count;
        _currentVfxId = _validVfxIds[currentIndex];
    }

    public void PreviousVfx()
    {
        if (_validVfxIds.Count == 0) return;

        int currentIndex = _validVfxIds.IndexOf(_currentVfxId);
        currentIndex = (currentIndex - 1 + _validVfxIds.Count) % _validVfxIds.Count;
        _currentVfxId = _validVfxIds[currentIndex];
    }

    public void SetVfxById(int vfxId)
    {
        if (_validVfxIds.Contains(vfxId))
        {
            _currentVfxId = vfxId;
        }
    }

    public void ApplyVfx(int vfxId, bool isPermanent = false)
    {
        // If targeting a location, apply directly to location (always temporary)
        if (_targetLocation != null)
        {
            if (!_isDm)
            {
                _player.SendServerMessage("Only DMs can apply VFX to locations.", ColorConstants.Orange);
                return;
            }

            VisualEffectTableEntry entry = NwGameTables.VisualEffectTable[vfxId];
            Effect vfxEffect = Effect.VisualEffect(entry);
            _targetLocation.ApplyEffect(EffectDuration.Temporary, vfxEffect, TimeSpan.FromSeconds(5));
            _player.SendServerMessage($"Applied VFX at location: {entry.Label}", ColorConstants.Cyan);
            return;
        }

        // Otherwise, apply to target object
        if (_targetObject == null || !_targetObject.IsValid)
        {
            _player.SendServerMessage("No valid target selected.", ColorConstants.Orange);
            return;
        }

        VisualEffectTableEntry entry2 = NwGameTables.VisualEffectTable[vfxId];
        Effect vfxEffect2 = Effect.VisualEffect(entry2);

        if (isPermanent && _isDm)
        {
            vfxEffect2.SubType = EffectSubType.Unyielding;
            _targetObject.ApplyEffect(EffectDuration.Permanent, vfxEffect2);
            _player.SendServerMessage($"Applied permanent VFX: {entry2.Label}", ColorConstants.Cyan);
        }
        else
        {
            _targetObject.ApplyEffect(EffectDuration.Temporary, vfxEffect2, TimeSpan.FromSeconds(5));
            _player.SendServerMessage($"Applied temporary VFX: {entry2.Label}", ColorConstants.Cyan);
        }
    }

    public void ApplyVfxWithDuration(int vfxId, int durationSeconds)
    {
        if (!_isDm)
        {
            _player.SendServerMessage("Only DMs can apply VFX with custom durations.", ColorConstants.Orange);
            return;
        }

        // If targeting a location, apply directly to location
        if (_targetLocation != null)
        {
            VisualEffectTableEntry entry = NwGameTables.VisualEffectTable[vfxId];
            Effect vfxEffect = Effect.VisualEffect(entry);
            _targetLocation.ApplyEffect(EffectDuration.Temporary, vfxEffect, TimeSpan.FromSeconds(durationSeconds));
            _player.SendServerMessage($"Applied VFX at location for {durationSeconds} seconds: {entry.Label}", ColorConstants.Cyan);
            return;
        }

        // Otherwise, apply to target object
        if (_targetObject == null || !_targetObject.IsValid)
        {
            _player.SendServerMessage("No valid target selected.", ColorConstants.Orange);
            return;
        }

        VisualEffectTableEntry entry2 = NwGameTables.VisualEffectTable[vfxId];
        Effect vfxEffect2 = Effect.VisualEffect(entry2);
        _targetObject.ApplyEffect(EffectDuration.Temporary, vfxEffect2, TimeSpan.FromSeconds(durationSeconds));
    }

    public List<VfxEffectInfo> GetActiveVfxList()
    {
        List<VfxEffectInfo> activeVfx = new();

        if (_targetObject == null || !_targetObject.IsValid)
            return activeVfx;

        foreach (Effect effect in _targetObject.ActiveEffects)
        {
            if (effect.EffectType != EffectType.VisualEffect) continue;

            int vfxId = effect.IntParams[0];
            VisualEffectTableEntry entry = NwGameTables.VisualEffectTable[vfxId];

            if (entry.Label != null)
            {
                activeVfx.Add(new VfxEffectInfo
                {
                    VfxId = vfxId,
                    Label = entry.Label,
                    Effect = effect
                });
            }
        }

        return activeVfx;
    }

    public void RemoveVfx(Effect effect)
    {
        if (_targetObject == null || !_targetObject.IsValid)
        {
            _player.SendServerMessage("No valid target selected.", ColorConstants.Orange);
            return;
        }

        _targetObject.RemoveEffect(effect);
        _player.SendServerMessage("VFX removed.", ColorConstants.Cyan);
    }

    public bool IsDm()
    {
        return _isDm;
    }

    public string GetTargetName()
    {
        if (_targetLocation != null)
        {
            return $"Ground Location ({_targetLocation.Position.X:F1}, {_targetLocation.Position.Y:F1})";
        }
        return _targetObject?.Name ?? "No Target";
    }
}

public class VfxEffectInfo
{
    public int VfxId { get; set; }
    public string Label { get; set; } = string.Empty;
    public Effect Effect { get; set; } = null!;
}

