using IPA.Utilities;
using UnityEngine;

namespace Vivify.UnityKit.Components.EventEffects
{
    public class LightPairRotation : BaseComponentsMonoBehaviour
    {
        private static readonly FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.Accessor _eventLAccessor =
            FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.GetAccessor("_eventL");

        private static readonly FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.Accessor _eventRAccessor =
            FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.GetAccessor("_eventR");

        private static readonly FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.Accessor _switchOverrideRandomValuesEventAccessor =
            FieldAccessor<LightPairRotationEventEffect, global::BasicBeatmapEventType>.GetAccessor("_switchOverrideRandomValuesEvent");

        private static readonly FieldAccessor<LightPairRotationEventEffect, Vector3>.Accessor _rotationVectorAccessor =
            FieldAccessor<LightPairRotationEventEffect, Vector3>.GetAccessor("_rotationVector");

        private static readonly FieldAccessor<LightPairRotationEventEffect, bool>.Accessor _overrideRandomValuesAccessor =
            FieldAccessor<LightPairRotationEventEffect, bool>.GetAccessor("_overrideRandomValues");

        private static readonly FieldAccessor<LightPairRotationEventEffect, bool>.Accessor _useZPositionForAngleOffsetAccessor =
            FieldAccessor<LightPairRotationEventEffect, bool>.GetAccessor("_useZPositionForAngleOffset");

        private static readonly FieldAccessor<LightPairRotationEventEffect, float>.Accessor _zPositionAngleOffsetScaleAccessor =
            FieldAccessor<LightPairRotationEventEffect, float>.GetAccessor("_zPositionAngleOffsetScale");

        private static readonly FieldAccessor<LightPairRotationEventEffect, float>.Accessor _startRotationAccessor =
            FieldAccessor<LightPairRotationEventEffect, float>.GetAccessor("_startRotation");

        private static readonly FieldAccessor<LightPairRotationEventEffect, Transform>.Accessor _transformLAccessor =
            FieldAccessor<LightPairRotationEventEffect, Transform>.GetAccessor("_transformL");

        private static readonly FieldAccessor<LightPairRotationEventEffect, Transform>.Accessor _transformRAccessor =
            FieldAccessor<LightPairRotationEventEffect, Transform>.GetAccessor("_transformR");

        [SerializeField]
        private InternalTypes.BasicBeatmapEventType _eventL;
        [SerializeField]
        private InternalTypes.BasicBeatmapEventType _eventR;
        [SerializeField]
        private InternalTypes.BasicBeatmapEventType _switchOverrideRandomValuesEvent = InternalTypes.BasicBeatmapEventType.VoidEvent;
        [SerializeField]
        private Vector3 _rotationVector = Vector3.up;

        [Space]
        [SerializeField]
        private bool _overrideRandomValues;
        [SerializeField]
        private bool _useZPositionForAngleOffset;
        [SerializeField]
        private float _zPositionAngleOffsetScale = 1f;
        [SerializeField]
        private float _startRotation;

        [Space]
#pragma warning disable CS8618
        [SerializeField]
        private Transform _transformL;
        [SerializeField]
        private Transform _transformR;
#pragma warning restore CS8618

        protected override void InitializeBaseComponents()
        {
            LightPairRotationEventEffect lightPairRotationEventEffect = AddBaseComponent(GetInstantiator().InstantiateComponent<LightPairRotationEventEffect>(gameObject));
            _eventLAccessor(ref lightPairRotationEventEffect) = (global::BasicBeatmapEventType)_eventL;
            _eventRAccessor(ref lightPairRotationEventEffect) = (global::BasicBeatmapEventType)_eventR;
            _switchOverrideRandomValuesEventAccessor(ref lightPairRotationEventEffect) = (global::BasicBeatmapEventType)_switchOverrideRandomValuesEvent;
            _rotationVectorAccessor(ref lightPairRotationEventEffect) = _rotationVector;
            _overrideRandomValuesAccessor(ref lightPairRotationEventEffect) = _overrideRandomValues;
            _useZPositionForAngleOffsetAccessor(ref lightPairRotationEventEffect) = _useZPositionForAngleOffset;
            _zPositionAngleOffsetScaleAccessor(ref lightPairRotationEventEffect) = _zPositionAngleOffsetScale;
            _startRotationAccessor(ref lightPairRotationEventEffect) = _startRotation;
            _transformLAccessor(ref lightPairRotationEventEffect) = _transformL;
            _transformRAccessor(ref lightPairRotationEventEffect) = _transformR;
        }
    }
}
