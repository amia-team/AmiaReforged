using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.Core
{
    public partial class DmLogin
    {
        public int LoginNumber { get; set; }
        public string? CdKey { get; set; }
        public string? LoginName { get; set; }
        public DateTime SessionStart { get; set; }
        public DateTime? SessionEnd { get; set; }
    }
}
