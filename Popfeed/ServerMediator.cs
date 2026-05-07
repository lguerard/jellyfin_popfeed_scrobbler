using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Popfeed.Api;
using Popfeed.Configuration;
using Popfeed.Model;

namespace Popfeed;

public class ServerMediator : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly IUserDataManager _userDataManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<ServerMediator> _logger;
    private readonly Plugin _plugin;

    public ServerMediator(
        ISessionManager sessionManager,
        IUserDataManager userDataManager,
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory,
        Plugin plugin)
    {
        _sessionManager = sessionManager;
        _userDataManager = userDataManager;
        _libraryManager = libraryManager;
        _logger = loggerFactory.CreateLogger<ServerMediator>();
        _plugin = plugin;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStopped += OnPlaybackStopped;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackStopped -= OnPlaybackStopped;
        return Task.CompletedTask;
    }

    private async void OnPlaybackStopped(object sender, PlaybackStopEventArgs e)
    {
        try
        {
            if (e?.Item == null || e.Users == null || e.Users.Count == 0)
            {
                return;
            }

            foreach (var user in e.Users)
            {
                var userId = user.Id;

                var userConfig = _plugin.PluginConfiguration.GetAllPopfeedUsers()
                    .FirstOrDefault(u => u.LinkedMbUserId == userId);

                if (userConfig == null)
                {
                    continue;
                }

                var userData = _userDataManager.GetUserData(user, e.Item);

                if (userData?.Played == true)
                {
                    await HandleWatchedItem(userConfig, e.Item, user);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing playback stop event");
        }
    }

    private async Task HandleWatchedItem(PopfeedUser userConfig, BaseItem item, Jellyfin.Database.Implementations.Entities.User user)
    {
        var title = item.Name;
        var year = item.ProductionYear?.ToString();

        var isMovie = item is MediaBrowser.Controller.Entities.Movies.Movie ||
                      item is MediaBrowser.Controller.Entities.Trailer;

        var isEpisode = item is Episode;
        var isSeason = item is Season;
        var isSeries = item is Series;

        using var httpClient = new HttpClient();
        var blueskyService = new BlueskyService(httpClient);

        bool shouldPost = false;

        if (userConfig.MarkWatchedOnPopfeed && !string.IsNullOrEmpty(userConfig.BlueskyHandle) && !string.IsNullOrEmpty(userConfig.BlueskyAppPassword))
        {
            if (isMovie)
            {
                shouldPost = true;
            }
            else if (isEpisode)
            {
                if (userConfig.PostOnSeasonComplete)
                {
                    var episode = item as Episode;
                    if (episode != null)
                    {
                        var season = episode.Season;
                        if (season != null)
                        {
                            shouldPost = await CheckSeasonComplete(user, season, episode);
                        }
                    }
                }
                else if (userConfig.PostEachEpisode)
                {
                    shouldPost = true;
                }
            }

            if (shouldPost)
            {
                await blueskyService.MarkWatchedOnPopfeedAsync(userConfig, title, year, null, isMovie);
            }
        }

        if (userConfig.PostToBluesky && !string.IsNullOrEmpty(userConfig.BlueskyHandle) && !string.IsNullOrEmpty(userConfig.BlueskyAppPassword))
        {
            bool shouldBlueskyPost = false;
            
            if (isMovie)
            {
                shouldBlueskyPost = true;
            }
            else if (isEpisode)
            {
                if (userConfig.PostOnSeasonComplete)
                {
                    var episode = item as Episode;
                    if (episode != null)
                    {
                        var season = episode.Season;
                        if (season != null)
                        {
                            shouldBlueskyPost = await CheckSeasonComplete(user, season, episode);
                        }
                    }
                }
                else if (userConfig.PostEachEpisode)
                {
                    shouldBlueskyPost = true;
                }
            }
            
            if (shouldBlueskyPost)
            {
                await blueskyService.PostToBlueskyAsync(userConfig, title, year, null, isMovie);
            }
        }
    }

    private async Task<bool> CheckSeasonComplete(Jellyfin.Database.Implementations.Entities.User user, Season season, Episode watchedEpisode)
    {
        try
        {
            var series = season.Series;
            if (series == null)
            {
                return false;
            }

            var children = season.GetEpisodes();
            if (children == null || children.Count == 0)
            {
                return false;
            }

            int totalEpisodes = children.Count;
            int watchedCount = 0;

            foreach (var episode in children)
            {
                var ud = _userDataManager.GetUserData(user, episode);
                if (ud?.Played == true)
                {
                    watchedCount++;
                }
            }

            return watchedCount >= totalEpisodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking season completion status");
            return false;
        }
    }
}