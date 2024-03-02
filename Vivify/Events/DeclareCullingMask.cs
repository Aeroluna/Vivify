using System;
using System.Collections.Generic;
using CustomJSONData.CustomBeatmap;
using Heck;
using Heck.Event;
using SiraUtil.Logging;
using Vivify.PostProcessing;
using Vivify.TrackGameObject;
using Zenject;
using static Vivify.VivifyController;

namespace Vivify.Events
{
    [CustomEvent(DECLARE_CULLING_TEXTURE)]
    internal class DeclareCullingMask : ICustomEvent, IDisposable
    {
        private readonly SiraLog _log;
        private readonly DeserializedData _deserializedData;

        private readonly HashSet<IDisposable> _disposables = new();

        private DeclareCullingMask(
            SiraLog log,
            [Inject(Id = ID)] DeserializedData deserializedData)
        {
            _log = log;
            _deserializedData = deserializedData;
        }

        public void Dispose()
        {
            foreach (IDisposable disposable in _disposables)
            {
                disposable.Dispose();
            }
        }

        public void Callback(CustomEventData customEventData)
        {
            if (!_deserializedData.Resolve(customEventData, out DeclareCullingMaskData? data))
            {
                return;
            }

            string name = data.Name;
            CullingTextureData textureData = new(data.Tracks, data.Whitelist, data.DepthTexture);
            _disposables.Add(textureData);
            PostProcessingController.CullingTextureDatas.Add(name, textureData);
            _log.Debug($"Created culling mask [{name}]");
            /*
                GameObject[] gameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
                List<int> layers = new List<int>();
                gameObjects.Select(n => n.layer).ToList().ForEach(n =>
                {
                    if (!layers.Contains(n))
                    {
                        layers.Add(n);
                    }
                });
                layers.Sort();
                Plugin.Logger.Log($"used layers: {string.Join(", ", layers)}");*/
        }
    }
}
