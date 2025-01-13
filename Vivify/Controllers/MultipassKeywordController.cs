#if !V1_29_1
using UnityEngine;
using UnityEngine.XR.OpenXR;

namespace Vivify.Controllers;

[RequireComponent(typeof(Camera))]
internal class MultipassKeywordController : MonoBehaviour
{
    private const string MULTIPASS_KEYWORD = "MULTIPASS_ENABLED";
    private const string MULTIPASS_EYE_KEY = "_StereoActiveEye";

    private static readonly int _multipassEye = Shader.PropertyToID(MULTIPASS_EYE_KEY);

    private Camera _camera = null!;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnPreRender()
    {
        bool enable = _camera.stereoEnabled &&
                      _camera.stereoTargetEye != StereoTargetEyeMask.None &&
                      OpenXRSettings.Instance.renderMode == OpenXRSettings.RenderMode.MultiPass;
        if (enable)
        {
            Shader.EnableKeyword(MULTIPASS_KEYWORD);
        }
        else
        {
            Shader.DisableKeyword(MULTIPASS_KEYWORD);
        }

        Shader.SetGlobalInt(_multipassEye, (int)_camera.stereoActiveEye);
    }
}
#endif
