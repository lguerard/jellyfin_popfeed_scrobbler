using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Popfeed.Configuration;

namespace Popfeed;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public override string Name => "Popfeed";

    public override Guid Id => new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

    public override string Description => "Sync your watch history with Popfeed and post to Bluesky.";

    public static Plugin Instance { get; private set; }

    public PluginConfiguration PluginConfiguration => Configuration;

    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "popfeed",
                EmbeddedResourcePath = GetType().Namespace + ".Web.popfeed.html",
            },
            new PluginPageInfo
            {
                Name = "popfeedjs",
                EmbeddedResourcePath = GetType().Namespace + ".Web.popfeed.js"
            }
        };
    }
}