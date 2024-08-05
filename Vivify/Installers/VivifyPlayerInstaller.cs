using JetBrains.Annotations;
using Vivify.Events;
using Vivify.Managers;
using Vivify.ObjectPrefab.Managers;
using Zenject;

namespace Vivify.Installers;

[UsedImplicitly]
internal class VivifyPlayerInstaller : Installer
{
    private readonly FeaturesModule _featuresModule;

    private VivifyPlayerInstaller(FeaturesModule featuresModule)
    {
        _featuresModule = featuresModule;
    }

    public override void InstallBindings()
    {
        if (!_featuresModule.Active)
        {
            return;
        }

        Container.BindInterfacesAndSelfTo<AssetBundleManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<PrefabManager>().AsSingle();

        Container.BindInterfacesAndSelfTo<NotePrefabManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<DebrisPrefabManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<SaberPrefabManager>().AsSingle();
        Container.BindInterfacesAndSelfTo<BeatmapObjectPrefabManager>().AsSingle();

        Container.BindInterfacesTo<QualitySettingsManager>().AsSingle();

        // Custom Events
        Container.BindInterfacesTo<ApplyPostProcessing>().AsSingle();
        Container.BindInterfacesTo<AssignObjectPrefab>().AsSingle();
        Container.BindInterfacesTo<DeclareCullingMask>().AsSingle();
        Container.BindInterfacesTo<DeclareRenderTexture>().AsSingle();
        Container.BindInterfacesTo<DestroyPrefab>().AsSingle();
        Container.BindInterfacesTo<Events.InstantiatePrefab>().AsSingle();
        Container.BindInterfacesTo<SetAnimatorProperty>().AsSingle();
        Container.BindInterfacesTo<SetCameraProperty>().AsSingle();
        Container.BindInterfacesTo<SetGlobalProperty>().AsSingle();
        Container.BindInterfacesAndSelfTo<SetMaterialProperty>().AsSingle();
        Container.BindInterfacesTo<SetRenderSetting>().AsSingle();
    }
}
