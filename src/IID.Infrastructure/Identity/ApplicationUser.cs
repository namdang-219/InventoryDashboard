namespace IID.Infrastructure.Identity;

public sealed class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser
{
    public ApplicationUser() { }
    public ApplicationUser(string userName) : base(userName) { }

    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
