﻿using JetBrains.Annotations;
using Vivify.HarmonyPatches;
using Vivify.Managers;
using Zenject;

namespace Vivify.Installers;

[UsedImplicitly]
internal class VivifyAppInstaller : Installer
{
    private readonly Config _config;

    private VivifyAppInstaller(Config config)
    {
        _config = config;
    }

    public override void InstallBindings()
    {
        Container.BindInstance(_config);
        Container.BindInterfacesAndSelfTo<DepthShaderManager>().AsSingle();
        Container.BindInterfacesTo<AddComponentsToCamera>().AsSingle();
        Container.BindInterfacesAndSelfTo<FeaturesModule>().AsSingle();
    }
}
