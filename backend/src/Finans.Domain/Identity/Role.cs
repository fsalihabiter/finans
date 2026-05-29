using Finans.Domain.Common;
using Finans.Domain.Enums;

namespace Finans.Domain.Identity;

/// <summary>Rol tanımı — UserRole değerleri (User, Admin). Admin yetkisi (11 §3).</summary>
public class Role : Entity
{
    public UserRole Name { get; set; }

    public ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
}
