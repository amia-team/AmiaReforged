namespace AmiaReforged.Core.Models
{
    public partial class Player
    {
        public string CdKey { get; set; } = null!;

        public virtual DreamcoinRecord? DreamcoinRecord { get; set; }
    }
}
