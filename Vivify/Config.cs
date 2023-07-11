using BepInEx.Configuration;
using JetBrains.Annotations;

namespace Vivify
{
    [UsedImplicitly]
    internal class Config
    {
        internal Config(ConfigFile configFile)
        {
            AllowDownload = configFile.Bind(
                VivifyController.ID,
                "Allow Download",
                false,
                "Allows Vivify to automatically download necessary assets.");
            BundleRepository = configFile.Bind(
                VivifyController.ID,
                "Bundle Repository",
                "https://aeroluna.dev/bundles/",
                "The URL to download bundles from.");
        }

        internal ConfigEntry<bool> AllowDownload { get; }

        internal ConfigEntry<string> BundleRepository { get; }
    }
}
