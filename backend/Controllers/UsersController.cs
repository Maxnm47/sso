using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public UsersController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var httpClient = _httpClientFactory.CreateClient();
        var adminUsername = "admin"; // Your admin username
        var adminPassword = "admin"; // Your admin password
        var keycloakServerUrl = "http://localhost:8210"; // Make sure this is reachable within your Docker network
        var realm = "clients";

        // Get admin token
        var tokenResponse = await httpClient.PostAsync(
            $"{keycloakServerUrl}/auth/realms/master/protocol/openid-connect/token",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    { "client_id", "admin-cli" },
                    { "username", adminUsername },
                    { "password", adminPassword },
                    { "grant_type", "password" }
                }
            )
        );

        var tokenContent = await tokenResponse.Content.ReadAsStringAsync();
        var accessToken = JObject.Parse(tokenContent)["access_token"].ToString();

        // Set bearer token
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        // Get users
        var usersResponse = await httpClient.GetAsync(
            $"{keycloakServerUrl}/auth/admin/realms/{realm}/users"
        );
        var usersContent = await usersResponse.Content.ReadAsStringAsync();
        var users = JArray.Parse(usersContent);

        // Extract user UUIDs
        var userUuids = new List<string>();
        foreach (var user in users)
        {
            var userId = user["id"].ToString();
            userUuids.Add(userId);
        }

        return Ok(userUuids);
    }
}
