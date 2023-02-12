using stackoverflow.api;
using stackoverflow.core;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/posts", GetQuestionsAsync).Produces<PaginationResult<Post>>();
app.MapPost("/posts/{postId:int}/vote/{voteTypeId:int}", VoteQuestionAsync).Produces<object>();

static async Task<PaginationResult<Post>> GetQuestionsAsync(
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

static async Task<object> VoteQuestionAsync(
    IDbQueryRepository dbQueryRepo,
    int postId,
    int voteTypeId)
{
    if (voteTypeId is not (2 or 3))
        return new
        {
            NoOfChanges = 0
        };

    var query_params = new
    {
        @Id = postId,
        @VoteTypeId = voteTypeId,
        @UserId = 1
    };
    var result = await dbQueryRepo.QueryAsync("dbo.sp_posts_vote", query_params, new List<OutputResultTranform> {
        new OutputResultTranform(typeof(int), TransformCategory.Single, "NoOfChanges")
    });

    return new
    {
        NoOfChanges = result["NoOfChanges"]
    };
}

app.Run();