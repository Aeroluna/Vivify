using JetBrains.Annotations;
using Vivify.Events;
using Vivify.Managers;
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

            Container.BindInterfacesAndSelfTo<AssetBundleManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<PrefabManager>().AsSingle();

            // Events
            Container.BindInterfacesTo<EventController>().AsSingle();
        }
    }
}
