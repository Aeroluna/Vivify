using JetBrains.Annotations;
using Vivify.Controllers;
using Zenject;

namespace Vivify.Installers
{
    [UsedImplicitly]
    internal class VivifyMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<AssetBundleDownloadViewController.CoroutineBastard>().FromNewComponentOnNewGameObject().AsSingle();
            Container.BindInterfacesTo<AssetBundleDownloadViewController>().FromNewComponentAsViewController().AsSingle();
        }
    }
}
