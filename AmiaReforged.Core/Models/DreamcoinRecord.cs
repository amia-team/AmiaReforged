namespace AmiaReforged.Core.Models
{
    public partial class DreamcoinRecord
    {
        public string CdKey { get; set; } = null!;
        public int? Amount { get; set; }

        public virtual Player CdKeyNavigation { get; set; } = null!;
    }
}
