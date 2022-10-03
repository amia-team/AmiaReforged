using System;
using System.Collections.Generic;
using NWN.Core;

namespace NWN.Amia.Main.Managed.Feats.Types
{
    public interface IEffectCollector
    {
        List<IntPtr> GatherEffectsForObject(uint objectId);
    }
}