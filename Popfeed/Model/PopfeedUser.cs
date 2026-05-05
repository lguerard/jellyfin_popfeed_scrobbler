#pragma warning disable CA1819

using System;

namespace Popfeed.Model;

public class PopfeedUser
{
    public PopfeedUser()
    {
        LinkedMbUserId = Guid.Empty;
        BlueskyHandle = string.Empty;
        BlueskyAppPassword = string.Empty;
        PostToBluesky = false;
        MarkWatchedOnPopfeed = true;
        ExtraLogging = false;
        PostEachEpisode = true;
        PostOnSeasonComplete = false;
    }

    public Guid LinkedMbUserId { get; set; }

    public string BlueskyHandle { get; set; }

    public string BlueskyAppPassword { get; set; }

    public bool PostToBluesky { get; set; }

    public bool MarkWatchedOnPopfeed { get; set; }

    public bool PostEachEpisode { get; set; }

    public bool PostOnSeasonComplete { get; set; }

    public bool ExtraLogging { get; set; }
}