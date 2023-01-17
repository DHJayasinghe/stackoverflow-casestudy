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
    string predicate = !string.IsNullOrEmpty(searchTerm) ? $" WHERE TagName = '{searchTerm}'" : "";
    var totalCountQuery = @$"SELECT COUNT(1) FROM dbo.vwPostTags with(noexpand) {predicate}";
    var totalRecords = (await dbQueryRepo.QueryAsync<int>(totalCountQuery)).First();
    var query = @$"SELECT
            P.Id,
            P.Title,
            Substring(P.Body, 1, 200)[Description],
            P.Tags,
            P.CreationDate AskedDateTime,
            P.OwnerUserId AskedById,
            Own.DisplayName AskedByDisplayName,
            1 Votes
        FROM dbo.Posts P INNER JOIN(SELECT PostId, TagId, CreationDate
        FROM dbo.vwPostTags with(noexpand) {predicate}
        ORDER BY CreationDate
        OFFSET({start}) ROWS FETCH NEXT({pageSize}) ROWS ONLY) PT
        ON P.Id = PT.PostId INNER JOIN dbo.Users Own
        ON P.OwnerUserId = Own.Id";

    var result = await dbQueryRepo.QueryAsync<Question>(query);
    return new PaginationResult<Question>()
{
        Data = result,
        TotalRecords = totalRecords
    };
}

app.Run();