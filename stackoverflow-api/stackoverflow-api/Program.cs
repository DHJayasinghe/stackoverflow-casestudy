using stackoverflow.api;
using stackoverflow.core;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/questions", GetQuestionsAsync).Produces<PaginationResult<Question>>();

static async Task<PaginationResult<Question>> GetQuestionsAsync(
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
        new OutputResultTranform(typeof(Question), TransformCategory.List, "Questions"),
        new OutputResultTranform(typeof(int), TransformCategory.Single, "TotalRecords"),
    });

    return new PaginationResult<Question>()
    {
        Data = ((List<object>)result["Questions"]).ConvertAll(item => (Question)item),
        TotalRecords = (int)result["TotalRecords"]
    };
}

app.Run();