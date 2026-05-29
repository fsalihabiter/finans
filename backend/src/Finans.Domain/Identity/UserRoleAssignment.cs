namespace Finans.Domain.Identity;

/// <summary>
/// Users *—* Roles join (tablo "UserRoles", 03 §B). Bileşik PK (UserId, RoleId).
/// Sınıf adı enum Enums.UserRole ile karışmasın diye "Assignment" soneki.
/// </summary>
public class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
