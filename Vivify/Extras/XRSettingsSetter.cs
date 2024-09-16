using JetBrains.Annotations;
using UnityEngine.XR;

namespace Vivify.Extras;

// Annoyingly, C# does not allow static types in generics
// ReSharper disable InconsistentNaming
[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class XRSettingsSetter
{
    public static bool useOcclusionMesh
    {
        get => XRSettings.useOcclusionMesh;
        set => XRSettings.useOcclusionMesh = value;
    }
}
