using System.Net;
using System.Net.Http.Json;
using Api.Dtos;

namespace Api.Tests;

public class TaskApiIntegrationTests(TestApiFactory factory) : IClassFixture<TestApiFactory>
{
    [Fact]
    public async Task GetTasks_Unauthenticated_Returns401()
    {
        // Arrange
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/tasks");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateThenGet_Authenticated_RoundTrips()
    {
        // Arrange
        var client = await AuthedClientAsync("roundtrip@x.com");

        // Act
        var createResponse = await client.PostAsJsonAsync("/api/v1/tasks",
            new TaskRequest("Write tests", null, null, null));
        var created = await createResponse.Content.ReadFromJsonAsync<TaskResponse>();
        var getResponse = await client.GetAsync($"/api/v1/tasks/{created!.Id}");
        var fetched = await getResponse.Content.ReadFromJsonAsync<TaskResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.Id, fetched!.Id);
        Assert.Equal("Write tests", fetched.Title);
    }

    [Fact]
    public async Task GetTask_OtherUsersTask_Returns404()
    {
        // Arrange
        var alice = await AuthedClientAsync("owner@x.com");
        var bob = await AuthedClientAsync("attacker@x.com");
        var create = await alice.PostAsJsonAsync("/api/v1/tasks",
            new TaskRequest("Alice's secret", null, null, null));
        var task = await create.Content.ReadFromJsonAsync<TaskResponse>();

        // Act
        var response = await bob.GetAsync($"/api/v1/tasks/{task!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Registers + logs in a user and returns a client with the bearer token set.
    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var creds = new { email, password = "Secret123" };
        await client.PostAsJsonAsync("/api/v1/auth/register", creds);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", creds);
        Assert.True(login.IsSuccessStatusCode, "login should succeed");
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        return client;
    }

    private record LoginResponse(string AccessToken);
}
