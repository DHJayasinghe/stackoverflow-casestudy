using stackoverflow.api;
using stackoverflow.core;
using stackoverflow_api;

var builder = WebApplication.CreateBuilder(args);
var configurations = builder.Configuration;

builder.Services
    .AddTransient<IDbQueryRepository>(d => new DbQueryRepository(configurations.GetConnectionString("Default")))
    .AddTransient<ICurrentUser, CurrentUser>()
    .AddHttpContextAccessor();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/authorize", AccountOperations.AuthorizeAsync).Produces<IResult>();
app.MapGet("/posts", PostOperations.GetPostsAsync).Produces<PaginationResult<Post>>();
app.MapPost("/posts/{postId:int}/vote/{voteTypeId:int}", PostOperations.VotePostAsync).Produces<IResult>();

app.Run();

public partial class Program
{
    public const string ENCRYPYION_KEY = "b14ca5898a4e4133bbce2ea2315a1916";
}