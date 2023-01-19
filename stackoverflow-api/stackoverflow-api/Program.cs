using stackoverflow.core;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

// Add services to the container.
builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")));

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/questions", GetQuestionsAsync).Produces<PaginationResult<Question>>();

static async Task<PaginationResult<Question>> GetQuestionsAsync(
    IDbQueryRepository dbQueryRepo,
    string searchTerm = null,
    int pageSize = 15,
    int start = 0
)
{
    var searchTerms = searchTerm.Split(",",StringSplitOptions.RemoveEmptyEntries);
    bool IsMultiSearch() => searchTerms.Length > 1;

    var query_params = new
    {
        SearchTerm = searchTerms,
        Start = start,
        PageSize = pageSize
    };
    string predicate = !string.IsNullOrEmpty(searchTerm) ? (IsMultiSearch() ? " WHERE TagName IN @SearchTerm" : $" WHERE TagName = @SearchTerm") : "";
    var totalCountQuery = @$"SELECT COUNT(1) FROM dbo.vwPostTags with(noexpand) {predicate}";
    var totalRecords = (await dbQueryRepo.QueryAsync<int>(totalCountQuery, query_params)).First();
    var query = @$"SELECT
            P.Id,
            P.Title,
            Substring(P.Body, 1, 200)[Description],
            P.Tags,
            P.CreationDate AskedDateTime,
            P.OwnerUserId AskedById,
            Own.DisplayName AskedByDisplayName,
            V.Votes NoOfVotes
        FROM dbo.Posts P INNER JOIN(SELECT PostId, TagId, CreationDate
        FROM dbo.vwPostTags with(noexpand) {predicate}
        ORDER BY CreationDate
        OFFSET(@Start) ROWS FETCH NEXT(@PageSize) ROWS ONLY) PT
        ON P.Id = PT.PostId LEFT JOIN (SELECT V.PostId,COUNT(1) Votes FROM dbo.Votes V GROUP BY V.PostId) V 
        ON P.Id = V.PostId LEFT JOIN dbo.Users Own
        ON P.OwnerUserId = Own.Id";

    var result = await dbQueryRepo.QueryAsync<Question>(query, query_params);
    return new PaginationResult<Question>()
    {
        Data = result,
        TotalRecords = totalRecords
    };
}

app.Run();