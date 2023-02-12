using stackoverflow.api;
using stackoverflow.core;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")))
    .AddTransient<ICurrentUser, CurrentUser>()
    .AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/authorize", AuthorizeAsync).Produces<IResult>();
app.MapGet("/posts", GetPostsAsync).Produces<PaginationResult<Post>>();
app.MapPost("/posts/{postId:int}/vote/{voteTypeId:int}", VotePostAsync).Produces<IResult>();
app.MapGet("/posts/{postId:int}/score", GetPostScoreAsync).Produces<int>();

static async Task<PaginationResult<Post>> GetPostsAsync(
    IDbQueryRepository dbQueryRepo,
    string searchTerm = null,
    int pageSize = 15,
    int start = 0,
    SearchType searchType = SearchType.NEWEST
)
{
    var query_params = new
    {
        @Tags = searchTerm,
        @SearchType = searchType.ToString(),
        @Start = start,
        @PageSize = pageSize
    };
    var result = await dbQueryRepo.QueryAsync("dbo.sp_posts_search", query_params, new List<OutputResultTranform> {
        new OutputResultTranform(typeof(Post), TransformCategory.List, "Questions"),
        new OutputResultTranform(typeof(int), TransformCategory.Single, "TotalRecords"),
    });

    return new PaginationResult<Post>()
    {
        Data = ((List<object>)result["Questions"]).ConvertAll(item => (Post)item),
        TotalRecords = (int)result["TotalRecords"]
    };
}

static async Task<IResult> VotePostAsync(
    IDbQueryRepository dbQueryRepo,
    ICurrentUser currentUser,
    int postId,
    int voteTypeId)
{
    if (!currentUser.IsAuthenticated) return Results.Unauthorized();
    if (voteTypeId is not (2 or 3)) return Results.BadRequest("Unsupported vote type");

    var query_params = new
    {
        @Id = postId,
        @VoteTypeId = voteTypeId,
        @UserId = currentUser.Id
    };
    var result = await dbQueryRepo.QueryAsync("dbo.sp_posts_vote", query_params, new List<OutputResultTranform> {
        new OutputResultTranform(typeof(int), TransformCategory.Single, "NoOfChanges")
    });

    return Results.Ok();
}

static async Task<int> GetPostScoreAsync(
    IDbQueryRepository dbQueryRepo,
    int postId
)
{
    var result = await dbQueryRepo.QueryAsync<int>($"SELECT Score FROM dbo.Posts WHERE Id={postId}", commandType: System.Data.CommandType.Text);
    return result.First();
}

static async Task<IResult> AuthorizeAsync(
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

app.Run();

public partial class Program
{
    public const string ENCRYPYION_KEY = "b14ca5898a4e4133bbce2ea2315a1916";
}