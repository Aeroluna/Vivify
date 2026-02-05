using UnityEngine;
using Vivify.ObjectPrefab.Pools;

namespace Vivify.ObjectPrefab;

internal class FollowedSaberTrail : SaberTrail
{
    private static readonly int _colorId = Shader.PropertyToID("_Color");

    private readonly SimpleOffsetMovementData _simpleOffsetMovementData = new();

    private SaberTrail? _followed;

    private MaterialPropertyBlock? _materialPropertyBlock;

    internal Material Material { get; set; } = null!;

    internal void Init(SaberTrail followed, Transform parent)
    {
        if (_trailRenderer == null)
        {
#if LATEST
            _trailRenderer = _container.InstantiatePrefabForComponentAt<SaberTrailRenderer>(
                followed._trailRendererPrefab,
                Vector3.zero,
                Quaternion.identity,
                null);
#else
            _trailRenderer = Instantiate(followed._trailRendererPrefab, Vector3.zero, Quaternion.identity);
#endif
            _trailRenderer.transform.SetParent(followed._trailRenderer.transform.parent);
            _trailRenderer._meshRenderer.material = Material;
        }

        _followed = followed;
        _movementData ??= _simpleOffsetMovementData;
        _simpleOffsetMovementData.Init(followed._movementData, parent);
        Init();
    }

    internal void InitProperties(TrailProperties trailProperties)
    {
        _simpleOffsetMovementData.InitProperties(
            trailProperties.TopPos ?? Vector3.forward,
            trailProperties.BottomPos ?? Vector3.zero);
        _trailDuration = trailProperties.Duration ?? 0.4f;
        _samplingFrequency = trailProperties.SamplingFrequency ?? 50;
        _granularity = trailProperties.Granularity ?? 60;
    }

#if LATEST
    private new void Start()
    {
    }
#else
    private new void Awake()
    {
    }
#endif

    private void Update()
    {
        if (_followed == null || _color == _followed._color)
        {
            return;
        }

        _color = _followed._color;
        _materialPropertyBlock ??= new MaterialPropertyBlock();
        _materialPropertyBlock.SetColor(_colorId, _color);
        _trailRenderer._meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private class SimpleOffsetMovementData : IBladeMovementData
    {
        private Vector3 _bottomPos;
        private IBladeMovementData? _followed;

        private Transform? _parent;

        private Vector3 _topPos;

        public float bladeSpeed => 0;

        public BladeMovementDataElement lastAddedData =>
            _followed == null ? default : Modify(_followed.lastAddedData);

        public BladeMovementDataElement prevAddedData =>
            _followed == null ? default : Modify(_followed.prevAddedData);

        internal void Init(IBladeMovementData followed, Transform parent)
        {
            _followed = followed;
            _parent = parent;
        }

        internal void InitProperties(Vector3 topPos, Vector3 bottomPos)
        {
            _topPos = topPos;
            _bottomPos = bottomPos;
        }

        private BladeMovementDataElement Modify(BladeMovementDataElement original)
        {
            if (_parent == null)
            {
                return original;
            }

            return new BladeMovementDataElement
            {
                time = original.time,
                topPos = original.bottomPos + _parent.TransformVector(_topPos),
                bottomPos = original.bottomPos + _parent.TransformVector(_bottomPos)
            };
        }
    }
}
