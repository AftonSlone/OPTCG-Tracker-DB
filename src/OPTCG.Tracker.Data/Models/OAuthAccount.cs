namespace OPTCG.Tracker.Data.Models;

public class OAuthAccount
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public OAuthProvider Provider { get; set; }

    public string ProviderUserId { get; set; } = string.Empty;

    public string? ProviderEmail { get; set; }

    public string? ProviderDisplayName { get; set; }

    public string? ProviderAvatarUrl { get; set; }

    public string? AccessTokenEncrypted { get; set; }

    public string? RefreshTokenEncrypted { get; set; }

    public DateTime? TokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
