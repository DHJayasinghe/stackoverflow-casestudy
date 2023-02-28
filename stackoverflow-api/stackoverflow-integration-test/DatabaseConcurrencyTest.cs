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

    private async Task ResetVoteMadeByUsersAsync(int postId, int[] userIds)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        await queryRepo.ExecuteAsync(@"UPDATE V SET 
                        V.VoteTypeId = 3
                    FROM dbo.Votes V
                    WHERE V.VoteTypeId = 2 AND V.PostId = @postId AND V.[UserId] IN @userIds",
                commandType: System.Data.CommandType.Text,
                parameters: new { postId = postId, userIds = userIds });
    }

    private async Task RemoveVoteMadeByUsersAsync(int postId, int[] userIds)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        await queryRepo.ExecuteAsync(@"DELETE V FROM dbo.Votes V
                    WHERE V.VoteTypeId = 2 AND V.PostId = @postId AND V.[UserId] IN @userIds",
                commandType: System.Data.CommandType.Text,
                parameters: new { postId = postId, userIds = userIds });
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

    private async Task<bool> PostVoteAsync(int postId, int voteTypeId, int userId)
    {
        try
        {
            using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
            var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
            var query_params = new
            {
                @Id = postId,
                @VoteTypeId = voteTypeId,
                @UserId = userId
            };
            await queryRepo.ExecuteAsync("dbo.sp_posts_vote", parameters: query_params, commandType: System.Data.CommandType.StoredProcedure);
            return true;
        }
        catch (Exception) { return false; }
    }

    private async Task<int> GetVoteCountOnPostByUserAsync(int id, int userId)
    {
        using var scope = _factory.Services.GetService<IServiceScopeFactory>().CreateScope();
        var queryRepo = scope.ServiceProvider.GetRequiredService<IDbQueryRepository>();
        var result = await queryRepo.QueryAsync(@$"SELECT COUNT(1) FROM dbo.Votes V 
                WHERE PostId=@id AND VoteTypeId IN (2,3) AND UserId = @userId",
            parameters: new { id, userId },
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
        const int voteTypeId = 2;
        var selectedUserIds = new[] { 1, 2 };
        await ResetVoteMadeByUsersAsync(postId, selectedUserIds);

        var upVotingTasks = await DoConcurrentUpVotingRequests(postId, voteTypeId, selectedUserIds);
        var recordedScore = await GetRecordedScoreOfPostAsync(postId);
        var actualScore = await GetActualScoreOfPostAsync(postId);

        upVotingTasks.Should().AllSatisfy(response => response.Should().BeTrue());
        recordedScore.Should().Be(actualScore);
    }

    [TestMethod]
    public async Task Should_HaveSingleVotePerUser_When_UserDoConcurrentVotingOnSamePost()
    {
        const int postId = 4175774;
        const int voteTypeId = 2;
        var selectedUserIds = new[] { 3, 4 };
        await RemoveVoteMadeByUsersAsync(postId, selectedUserIds);

        var votesCountBefore = await GetVoteCountOnPostByUserAsync(postId, selectedUserIds[0]);
        var upVotingTasks = await DoConcurrentUpVotingRequests(postId, voteTypeId, selectedUserIds);
        var votesCountAfter = await GetVoteCountOnPostByUserAsync(postId, selectedUserIds[0]);

        upVotingTasks.Should().AllSatisfy(response => response.Should().BeTrue());
        votesCountBefore.Should().Be(0);
        votesCountAfter.Should().Be(1);
    }

    private async Task<IEnumerable<bool>> DoConcurrentUpVotingRequests(int postId, int voteTypeId, int[] userIds)
    {
        var upVotingTasks = (await Task.WhenAll(userIds.Select(userId => PostVoteAsync(postId, voteTypeId, userId))));
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