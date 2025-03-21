using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AmiaReforged.PwEngine.Database.Entities;

public class OrganizationReputation
{
    [Key] public long Id { get; set; }
    
    public long OrganizationAId { get; set; }
    [ForeignKey("OrganizationAid")] public Organization OrganizationA { get; set; }
    
    public long OrganizationBId { get; set; }
    [ForeignKey("OrganizationBid")] public Organization OrganizationB { get; set; }
    
    // Anything below -50 is considered to be at war
    [Range(-100, 100)] public int Reputation { get; set; }
}