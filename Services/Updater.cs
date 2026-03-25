using Velopack;

namespace Felweed.Services;

public static class Updater
{
    public static async Task UpdateAsync()
    {
#if !DEBUG
        var mgr = new UpdateManager("https://strigefleur.github.io//h2o-dispatcher-client/felweed/");

        // check for new version
        var newVersion = await mgr.CheckForUpdatesAsync();
        if (newVersion == null)
            return; // no update available

        // download new version
        await mgr.DownloadUpdatesAsync(newVersion);

        // install new version and restart app
        mgr.ApplyUpdatesAndRestart(newVersion);
#endif
    }
}