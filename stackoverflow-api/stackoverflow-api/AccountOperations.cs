using stackoverflow.api;
using stackoverflow.core;

namespace stackoverflow_api;

public sealed class AccountOperations
{
    internal static async Task<IResult> AuthorizeAsync(
         IDbQueryRepository dbQueryRepo,
         AuthRequest request)
    {
        if (request.Password != "stackoverflow")
            return Results.BadRequest("Username or password is incorrect");

        var query_params = new
        {
            @Username = request.Username
        };
        var userId = (await dbQueryRepo.QueryAsync<int>($"SELECT TOP 1 Id FROM dbo.Users WHERE DisplayName=@Username", query_params, commandType: System.Data.CommandType.Text)).FirstOrDefault();
        return Results.Ok(SymmetricEncryptionDecryptionManager.Encrypt(userId.ToString(), Program.ENCRYPYION_KEY));
    }
}