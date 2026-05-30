using Finans.Application.Common;

namespace Finans.Api.Auth;

/// <summary>
/// Faz 1 <see cref="ICurrentUser"/> uygulaması: kimlik henüz yok (04 §1). Kullanıcıyı
/// <c>X-User-Id</c> başlığından çözer (IDOR/çok-kullanıcı testleri için, SC-13); başlık
/// yoksa dev varsayılanına (<c>Auth:DevUserId</c>) düşer. **Faz 5'te** bu sınıf yerini
/// JWT claim'lerinden okuyan uygulamaya bırakır — servis/controller'lar değişmez.
/// </summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IConfiguration config) : ICurrentUser
{
    public const string UserHeader = "X-User-Id";

    public Guid UserId
    {
        get
        {
            var ctx = accessor.HttpContext;
            if (ctx is not null &&
                ctx.Request.Headers.TryGetValue(UserHeader, out var header) &&
                Guid.TryParse(header.ToString(), out var fromHeader))
                return fromHeader;

            if (Guid.TryParse(config["Auth:DevUserId"], out var devUser))
                return devUser;

            throw new InvalidOperationException(
                "Kullanıcı kimliği çözümlenemedi (X-User-Id başlığı yok ve Auth:DevUserId ayarlı değil).");
        }
    }
}
