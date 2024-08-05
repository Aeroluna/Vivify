using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using JetBrains.Annotations;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace Vivify;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class Config
{
    public bool AllowDownload { get; set; }

    public string BundleRepository { get; set; } = "https://aeroluna.dev/bundles/";
}
