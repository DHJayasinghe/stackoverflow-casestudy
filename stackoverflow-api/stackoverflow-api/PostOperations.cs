using stackoverflow.api;
using stackoverflow.core;

namespace stackoverflow_api;

public sealed class PostOperations
{
    internal static async Task<PaginationResult<Post>> GetPostsAsync(
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
        var result = await dbQueryRepo.QueryAsync("dbo.sp_posts_search",
            parameters: query_params,
            commandType: System.Data.CommandType.StoredProcedure,
            mapItems: new List<OutputResultTranform> {
                new OutputResultTranform(typeof(Post), TransformCategory.List, "Questions"),
                new OutputResultTranform(typeof(int), TransformCategory.Single, "TotalRecords"),
        });

        return new PaginationResult<Post>()
        {
            Data = ((List<object>)result["Questions"]).ConvertAll(item => (Post)item),
            TotalRecords = (int)result["TotalRecords"]
        };
    }

    internal static async Task<IResult> VotePostAsync(
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
        await dbQueryRepo.ExecuteAsync("dbo.sp_posts_vote", parameters: query_params, commandType: System.Data.CommandType.StoredProcedure);

        return Results.Ok();
    }
}