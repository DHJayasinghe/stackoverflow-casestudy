using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http.Json;

namespace stackoverflow_integration_test;

[TestClass]
public class DatabaseConcurrencyTest
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _httpClient;

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
        _httpClient = _factory.CreateClient();
    }

    [TestMethod]
    public async Task Should_HaveCorrectScore_When_MultipleUsersConcurrentyVoteTheSamePost()
    {
        int postId = 4175774;

        var user1HttpClient = _factory.CreateClient();
        var user1Token = await (await user1HttpClient.PostAsJsonAsync($"/authorize", new { Username = "Jeff Atwood", Password = "stackoverflow" })).Content.ReadFromJsonAsync<string>();
        user1HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {user1Token}");

        var user2HttpClient = _factory.CreateClient();
        var user2Token = await (await user2HttpClient.PostAsJsonAsync($"/authorize", new { Username = "Geoff Dalgas", Password = "stackoverflow" })).Content.ReadFromJsonAsync<string>();
        user2HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {user2Token}");

        int previousScore = await _httpClient.GetFromJsonAsync<int>($"/posts/{postId}/score");

        var votingTasks = new List<Task<HttpResponseMessage>>()
        {
            user1HttpClient.PostAsJsonAsync($"/posts/{postId}/vote/{VoteTestData.UP_VOTE}",new{}),
            user2HttpClient.PostAsJsonAsync($"/posts/{postId}/vote/{VoteTestData.DOWN_VOTE}",new{ })
        };
        await Task.WhenAll(votingTasks);

        int newScore = await _httpClient.GetFromJsonAsync<int>($"/posts/{postId}/score");
        newScore.Should().Be(previousScore);
    }
}

internal enum VoteTestData
{
    UP_VOTE = 2,
    DOWN_VOTE = 3
}