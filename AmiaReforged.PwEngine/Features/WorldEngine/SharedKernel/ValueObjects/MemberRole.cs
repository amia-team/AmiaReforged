using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Represents a specific role within an organization (e.g., "Treasurer", "Recruiter", "Diplomat")
/// </summary>
public readonly record struct MemberRole
{
    public string Value { get; }

    [JsonConstructor]
    public MemberRole(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(value));

        Value = value;
    }

    // Common predefined roles
    public static MemberRole Leader => new("Leader");
    public static MemberRole Officer => new("Officer");
    public static MemberRole Treasurer => new("Treasurer");
    public static MemberRole Recruiter => new("Recruiter");
    public static MemberRole Diplomat => new("Diplomat");
    public static MemberRole Member => new("Member");

    public static implicit operator string(MemberRole role) => role.Value;
    public static explicit operator MemberRole(string value) => new(value);

    public override string ToString() => Value;
}

