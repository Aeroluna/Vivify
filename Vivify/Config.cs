using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Vivify;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class Config
{
    public int MaxCamera2Cams { get; set; } = 2;

    public bool AllowDownload { get; set; }

    public string BundleRepository { get; set; } = "https://repo.totalbs.dev/api/v1/bundles/";
}
