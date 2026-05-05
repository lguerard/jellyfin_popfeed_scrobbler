# Popfeed for Jellyfin

A Jellyfin plugin that syncs your watch history with Popfeed and optionally posts to Bluesky when you finish watching.

## Features

- Mark items as watched on Popfeed when watched in Jellyfin (properly marks as #finished)
- Optional posting to Bluesky when items are watched
- Per-user configuration - link different Jellyfin users to their Bluesky accounts

## Installation

### Option 1: Add to Jellyfin (Recommended)

1. Go to your Jellyfin server admin dashboard
2. Navigate to **Plugins** → **Repositories**
3. Click **Add Repository**
4. Add this manifest URL:

```
https://raw.githubusercontent.com/lguerard/jellyfin-plugin-popfeed/main/manifest.json
```

5. Go to **Plugins** → **Catalog**
6. Find "Popfeed" and click **Install**

### Option 2: Manual Installation

1. Download the latest release from [GitHub Releases](https://github.com/lguerard/jellyfin-plugin-popfeed/releases)
2. Upload the zip file in Jellyfin: **Plugins** → **Upload**

## Configuration

1. After installation, go to **Plugins** → **Popfeed**
2. Add a user and configure:
   - **Jellyfin User** - Select which Jellyfin user to link
   - **Bluesky Handle** - Your Bluesky handle (e.g., `yourname.bsky.social`)
   - **Bluesky App Password** - Create at [bsky.app/settings](https://bsky.app/settings) → Advanced → App passwords
   - **Popfeed Username** - (Optional) Your Popfeed username for marking items as watched
   - **Post to Bluesky** - Enable posting to Bluesky when items are watched
   - **Mark as watched on Popfeed** - Enable marking items as watched on Popfeed

## Building

```bash
dotnet publish --configuration Release --output bin
```

## License

MIT