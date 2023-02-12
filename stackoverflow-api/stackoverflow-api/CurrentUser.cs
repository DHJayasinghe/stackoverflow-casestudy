using Microsoft.Extensions.Primitives;

public sealed class CurrentUser : ICurrentUser
{
    private readonly HttpContext _httpContext;
    public CurrentUser(IHttpContextAccessor httpContextAccessor) => _httpContext = httpContextAccessor.HttpContext;
    public CurrentUser(HttpContext httpContext) => _httpContext = httpContext;

    public bool IsAuthenticated => Id > -1;

    public int Id
    {
        get
        {
            _httpContext.Request.Headers.TryGetValue("Authorization", out StringValues authHeader);

            if (authHeader.FirstOrDefault()?.IndexOf("Bearer") != -1)
            {
                var token = authHeader[0]["Bearer ".Length..];
                var userId = Convert.ToInt32(SymmetricEncryptionDecryptionManager.Decrypt(token, Program.ENCRYPYION_KEY));
                return userId;
            }

            return -1;
        }
    }
}
