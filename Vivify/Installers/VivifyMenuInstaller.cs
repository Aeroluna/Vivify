using JetBrains.Annotations;
using Vivify.Controllers;
using Vivify.Settings;
using Zenject;

namespace Vivify.Installers;

[UsedImplicitly]
internal class VivifyMenuInstaller : Installer
{
    public override void InstallBindings()
    {
        Container.BindInterfacesTo<SettingsMenu>().AsSingle();
        Container
            .Bind<AssetBundleDownloadViewController.AssetDownloader>()
            .FromNewComponentOnNewGameObject()
            .AsSingle();
        Container.BindInterfacesTo<AssetBundleDownloadViewController>().FromNewComponentAsViewController().AsSingle();
    }
}
