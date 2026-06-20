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
        var client = factory.CreateClient();
        var creds = new { email = "roundtrip@x.com", password = "Secret123" };
        await client.PostAsJsonAsync("/api/v1/auth/register", creds);
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", creds);
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

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

    private record LoginResponse(string AccessToken);
}
