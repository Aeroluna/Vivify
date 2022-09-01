using JetBrains.Annotations;
using Vivify.Events;
using Zenject;

namespace Vivify.Installers
{
    [UsedImplicitly]
    internal class VivifyPlayerInstaller : Installer
    {
        public override void InstallBindings()
        {
            if (!VivifyController.FeaturesPatcher.Enabled)
            {
                return;
            }

            Container.BindInterfacesAndSelfTo<AssetBundleController>().AsSingle();

            // Events
            Container.BindInterfacesTo<EventController>().AsSingle();
        }
    }
}
