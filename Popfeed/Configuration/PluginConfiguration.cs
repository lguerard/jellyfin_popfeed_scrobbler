#pragma warning disable CA1819

using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Model.Plugins;
using Popfeed.Model;

namespace Popfeed.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public PluginConfiguration()
    {
        PopfeedUsers = Array.Empty<PopfeedUser>();
    }

    public PopfeedUser[] PopfeedUsers { get; set; }

    public void AddUser(Guid userGuid)
    {
        var users = PopfeedUsers.ToList();
        users.Add(new PopfeedUser
        {
            LinkedMbUserId = userGuid
        });
        PopfeedUsers = users.ToArray();
    }

    public void RemoveUser(Guid userGuid)
    {
        var users = PopfeedUsers.ToList();
        users.RemoveAll(user => user.LinkedMbUserId == userGuid);
        PopfeedUsers = users.ToArray();
    }

    public IReadOnlyList<PopfeedUser> GetAllPopfeedUsers()
    {
        return PopfeedUsers.ToList();
    }
}