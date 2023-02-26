using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using stackoverflow.core;
using System.Net.Http.Json;

namespace stackoverflow_integration_test;

[TestClass]
public class DatabaseConcurrencyTest
{
    private WebApplicationFactory<Program> _factory;

    public DatabaseConcurrencyTest()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var integrationConfig = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .Build();

                config.AddConfiguration(integrationConfig);
            });
        });
    }

    private async Task ResetVoteMadeByUsersAsync(int postId, string[] displayNames)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        await queryRepo.ExecuteAsync(@"UPDATE V SET 
                        V.VoteTypeId = 3
                    FROM dbo.Votes V INNER JOIN dbo.Users U
                    ON V.UserId = U.Id AND V.VoteTypeId = 2
                    WHERE V.PostId = @postId AND U.[DisplayName] IN @displayNames",
                commandType: System.Data.CommandType.Text,
                parameters: new { postId = postId, displayNames = displayNames });
    }

    private async Task RemoveVoteMadeByUsersAsync(int postId, string[] displayNames)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        await queryRepo.ExecuteAsync(@"DELETE V FROM dbo.Votes V INNER JOIN dbo.Users U
                    ON V.UserId = U.Id AND V.VoteTypeId = 2
                    WHERE V.PostId = @postId AND U.[DisplayName] IN @displayNames",
                commandType: System.Data.CommandType.Text,
                parameters: new { postId = postId, displayNames = displayNames });
    }

    private async Task<int> GetActualScoreOfPostAsync(int id)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        var result = await queryRepo.QueryAsync($"SELECT Score=SUM(CASE VoteTypeId WHEN 2 THEN 1 ELSE -1 END) FROM dbo.Votes WHERE PostId={id} AND VoteTypeId IN (2,3)",
            commandType: System.Data.CommandType.Text,
            mapItems: new List<OutputResultTranform> { new OutputResultTranform(typeof(int), TransformCategory.Single, "Score"),
        });
        return (int)result["Score"];
    }

    private async Task<int> GetVoteCountOnPostByUserAsync(int id, string userDisplayName)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        var result = await queryRepo.QueryAsync(@$"SELECT COUNT(1) FROM dbo.Votes V INNER JOIN dbo.Users U ON V.UserId=U.Id 
                WHERE PostId=@id AND VoteTypeId IN (2,3) AND U.DisplayName = @userDisplayName",
            parameters: new { id, userDisplayName },
            commandType: System.Data.CommandType.Text,
            mapItems: new List<OutputResultTranform> { new OutputResultTranform(typeof(int), TransformCategory.Single, "Vote"),
        });
        return (int)result["Vote"];
    }

    private async Task<int> GetRecordedScoreOfPostAsync(int id)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        var result = await queryRepo.QueryAsync($"SELECT Score FROM dbo.Posts WHERE Id={id}",
            commandType: System.Data.CommandType.Text,
            mapItems: new List<OutputResultTranform> { new OutputResultTranform(typeof(int), TransformCategory.Single, "Score"),
        });
        return (int)result["Score"];
    }

    [TestMethod]
    public async Task Should_HaveCorrectScore_When_MultipleUsersConcurrentyChangeTheirVoteOnSamePost()
    {
        const int postId = 4175774;
        string UpVoteRequestUri = $"/posts/{postId}/vote/2";
        var selectedUsersDisplayNames = new[] { "Jeff Atwood", "Geoff Dalgas" };
        await ResetVoteMadeByUsersAsync(postId, selectedUsersDisplayNames);

        var upVotingTasks = await DoConcurrentUpVotingRequests(UpVoteRequestUri, selectedUsersDisplayNames);
        var recordedScore = await GetRecordedScoreOfPostAsync(postId);
        var actualScore = await GetActualScoreOfPostAsync(postId);

        upVotingTasks.Should().AllSatisfy(response => response.Result.IsSuccessStatusCode.Should().BeTrue());
        recordedScore.Should().Be(actualScore);
    }


    [TestMethod]
    public async Task Should_HaveSingleVotePerUser_When_UserDoConcurrentVotingOnSamePost()
    {
        const int postId = 4175774;
        string UpVoteRequestUri = $"/posts/{postId}/vote/2";
        var selectedUsersDisplayNames = new[] { "Jarrod Dixon", "Jarrod Dixon" };
        await RemoveVoteMadeByUsersAsync(postId, selectedUsersDisplayNames);

        var votesCountBefore = await GetVoteCountOnPostByUserAsync(postId, selectedUsersDisplayNames[0]);
        var upVotingTasks = await DoConcurrentUpVotingRequests(UpVoteRequestUri, selectedUsersDisplayNames);
        var votesCountAfter = await GetVoteCountOnPostByUserAsync(postId, selectedUsersDisplayNames[0]);

        upVotingTasks.Should().AllSatisfy(response => response.Result.IsSuccessStatusCode.Should().BeTrue());
        votesCountBefore.Should().Be(0);
        votesCountAfter.Should().Be(1);
    }

    private async Task<IEnumerable<Task<HttpResponseMessage>>> DoConcurrentUpVotingRequests(string UpVoteRequestUri, string[] usersDisplayNames)
    {
        var upVotingTasks = (await Task.WhenAll(usersDisplayNames
                    .Select(async userDisplayName => await CreateUniqueUserSessionAsync(userDisplayName))))
                    .Select(userHttpClient => userHttpClient.PostAsync(UpVoteRequestUri, null));

        await Task.WhenAll(upVotingTasks);
        return upVotingTasks;
    }

    private async Task<HttpClient> CreateUniqueUserSessionAsync(string userDisplayName)
    {
        var user1HttpClient = _factory.CreateClient();
        var user1Token = await (await user1HttpClient.PostAsJsonAsync($"/authorize", new { Username = userDisplayName, Password = "stackoverflow" })).Content.ReadFromJsonAsync<string>();
        user1HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {user1Token}");
        return user1HttpClient;
    }
}

internal enum VoteTestData
{
    UP_VOTE = 2,
    DOWN_VOTE = 3
}