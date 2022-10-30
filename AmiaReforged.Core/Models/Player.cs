using System;
using System.Collections.Generic;

namespace AmiaReforged.Core
{
    public partial class Player
    {
        public string CdKey { get; set; } = null!;

        public virtual DreamcoinRecord? DreamcoinRecord { get; set; }
    }
}
