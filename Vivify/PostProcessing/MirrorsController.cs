using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Vivify.PostProcessing
{
    internal static class MirrorsController
    {
        private static Mirror[] _mirrors = Array.Empty<Mirror>();

        private static List<Mirror> _enabledMirrors = new();

        private static MirrorEnableEventHandler[] _eventHandlers = Array.Empty<MirrorEnableEventHandler>();

        internal static IEnumerable<Mirror> EnabledMirrors
        {
            get
            {
                if (_enabledMirrors.Any(n => n == null))
                {
                    UpdateMirrors();
                }

                return _enabledMirrors;
            }
        }

        internal static void UpdateMirrors()
        {
            foreach (MirrorEnableEventHandler handler in _eventHandlers)
            {
                handler.Dispose();
            }

            _mirrors = Resources.FindObjectsOfTypeAll<Mirror>();
            _enabledMirrors = _mirrors.Where(n => n.isEnabled).ToList();
            _eventHandlers = _mirrors.Select(n => new MirrorEnableEventHandler(n)).ToArray();
        }

        private class MirrorEnableEventHandler : IDisposable
        {
            private readonly Mirror _mirror;

            internal MirrorEnableEventHandler(Mirror mirror)
            {
                _mirror = mirror;
                mirror.mirrorDidChangeEnabledStateEvent += OnMirrorEnabledStateChanged;
            }

            public void Dispose()
            {
                _mirror.mirrorDidChangeEnabledStateEvent -= OnMirrorEnabledStateChanged;
            }

            private void OnMirrorEnabledStateChanged(bool enabled)
            {
                if (enabled)
                {
                    _enabledMirrors.Add(_mirror);
                }
                else
                {
                    _enabledMirrors.Remove(_mirror);
                }
            }
        }
    }
}
