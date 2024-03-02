﻿using JetBrains.Annotations;
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

            Container.BindInterfacesAndSelfTo<BeatmapObjectPrefabManager>().AsSingle();

            Container.BindInterfacesTo<QualitySettingsManager>().AsSingle();

            // Custom Events
            Container.BindInterfacesTo<ApplyPostProcessing>().AsSingle();
            Container.BindInterfacesTo<AssignTrackPrefab>().AsSingle();
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
}
