using UnityEngine;
using Zenject;

namespace Vivify.ObjectPrefab;

internal class NoteMaterialProperties : ITickable
{
    private static readonly int _arrowCutoutPropertyID = Shader.PropertyToID("_ArrowCutout");

    private static readonly int _arrowCutoutTexOffsetPropertyID = Shader.PropertyToID("_ArrowCutoutTexOffset");

    private readonly MaterialPropertyBlockController _materialPropertyBlockController;
    private readonly CutoutEffect _arrowCutoutEffect;

    private float _previousCutout;

    internal NoteMaterialProperties(
        MaterialPropertyBlockController materialPropertyBlockController,
        CutoutEffect arrowCutoutEffect)
    {
        _materialPropertyBlockController = materialPropertyBlockController;
        _arrowCutoutEffect = arrowCutoutEffect;
    }

    public void Tick()
    {
        if (!Mathf.Approximately(_arrowCutoutEffect._cutout, _previousCutout))
        {
            Vector3 cutoutOffset = _arrowCutoutEffect.useRandomCutoutOffset
                ? _arrowCutoutEffect._randomNoiseTexOffset
                : _arrowCutoutEffect._cutoutOffset;
            MaterialPropertyBlock materialPropertyBlock = _materialPropertyBlockController.materialPropertyBlock;
            materialPropertyBlock.SetVector(_arrowCutoutTexOffsetPropertyID, cutoutOffset);
            materialPropertyBlock.SetFloat(_arrowCutoutPropertyID, _arrowCutoutEffect._cutout);
            _materialPropertyBlockController.ApplyChanges();
        }

        _previousCutout = _arrowCutoutEffect._cutout;
    }
}
