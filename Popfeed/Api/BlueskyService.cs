using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Popfeed.Model;

namespace Popfeed.Api;

public class BlueskyService
{
    private readonly HttpClient _httpClient;

    private const string AtProtoApiUrl = "https://atproto.com";
    private const string BskyApiUrl = "https://api.bsky.app";
    private const string PopfeedCollection = "social.popfeed.feed.listItem";

    private string _accessToken = string.Empty;
    private string _did = string.Empty;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public BlueskyService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> AuthenticateAsync(PopfeedUser user)
    {
        try
        {
            var authData = new
            {
                identifier = user.BlueskyHandle,
                password = user.BlueskyAppPassword
            };

            var json = JsonSerializer.Serialize(authData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{AtProtoApiUrl}/xrpc/com.atproto.server.createSession", content);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            _accessToken = root.GetProperty("accessJwt").GetString() ?? string.Empty;
            _did = root.GetProperty("did").GetString() ?? string.Empty;

            if (root.TryGetProperty("expiresAt", out var expiresAt) && expiresAt.ValueKind == JsonValueKind.String)
            {
                DateTime.TryParse(expiresAt.GetString(), out _tokenExpiry);
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddHours(2);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> MarkWatchedOnPopfeedAsync(PopfeedUser user, string mediaTitle, string year, string imdbId, bool isMovie)
    {
        try
        {
            if (string.IsNullOrEmpty(user.BlueskyHandle) || string.IsNullOrEmpty(user.BlueskyAppPassword))
            {
                return false;
            }

            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                if (!await AuthenticateAsync(user))
                {
                    return false;
                }
            }

            var identifiers = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(imdbId))
            {
                identifiers["imdbId"] = imdbId;
            }

            var creativeWorkType = isMovie ? "movie" : "tv_show";

            var popfeedItem = new
            {
                title = mediaTitle,
                identifiers = identifiers.Count > 0 ? identifiers : null,
                creativeWorkType = creativeWorkType,
                status = "#finished",
                addedAt = DateTime.UtcNow.ToString("o"),
                completedAt = DateTime.UtcNow.ToString("o"),
                listType = "watched"
            };

            var recordJson = JsonSerializer.Serialize(popfeedItem);
            var content = new StringContent(recordJson, Encoding.UTF8, "application/json");

            using var apiRequest = new HttpRequestMessage(HttpMethod.Post, $"{AtProtoApiUrl}/xrpc/com.atproto.repo.createRecord")
            {
                Content = content
            };
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var apiResponse = await _httpClient.SendAsync(apiRequest);

            return apiResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PostToBlueskyAsync(PopfeedUser user, string mediaTitle, string year, string posterUrl, bool isMovie)
    {
        try
        {
            if (string.IsNullOrEmpty(_accessToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                if (!await AuthenticateAsync(user))
                {
                    return false;
                }
            }

            var mediaType = isMovie ? "movie" : "episode";
            var postText = $"Just finished watching: {mediaTitle}" + (year != null ? $" ({year})" : "");

            object embed = posterUrl != null ? new Dictionary<string, object>
            {
                ["$type"] = "app.bsky.embed.external",
                ["external"] = new
                {
                    uri = $"https://www.imdb.com/find?q={Uri.EscapeDataString(mediaTitle)}",
                    title = mediaTitle,
                    description = $"Watched a {mediaType} on Jellyfin"
                }
            } : null;

            var postRecord = new
            {
                collection = "app.bsky.feed.post",
                repo = _did,
                record = new
                {
                    createdAt = DateTime.UtcNow.ToString("o"),
                    text = postText,
                    embed = embed
                }
            };

            var json = JsonSerializer.Serialize(postRecord);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{AtProtoApiUrl}/xrpc/com.atproto.repo.createRecord")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            var response = await _httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> TestConnectionAsync(PopfeedUser user)
    {
        return await AuthenticateAsync(user);
    }
}