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
    [CustomEvent(DECLARE_TEXTURE)]
    internal class DestroyTexture : ICustomEvent, IDisposable
    {
        private readonly SiraLog _log;
        private readonly DeserializedData _deserializedData;

        private readonly HashSet<IDisposable> _disposables = new();

        private DestroyTexture(
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
            if (!_deserializedData.Resolve(customEventData, out DestroyTextureData? data))
            {
                return;
            }

            string[] names = data.Name;
            foreach (string name in names)
            {
                if (PostProcessingController.CullingTextureDatas.TryGetValue(name, out CullingTextureData? active))
                {
                    PostProcessingController.CullingTextureDatas.Remove(name);
                    active.Dispose();
                    _disposables.Remove(active);
                    _log.Debug($"Destroyed culling texture [{name}]");
                }
                else if (PostProcessingController.DeclaredTextureDatas.Remove(name))
                {
                    _log.Debug($"Destroyed render texture [{name}]");
                }
                else
                {
                    _log.Error($"Could not find [{name}]");
                }
            }
        }
    }
}
