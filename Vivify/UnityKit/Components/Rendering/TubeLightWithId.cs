using IPA.Utilities;
using UnityEngine;

namespace Vivify.UnityKit.Components.Rendering
{
    public class TubeLightWithId : BaseComponentsMonoBehaviour
    {
        private static readonly FieldAccessor<LightWithIdMonoBehaviour, int>.Accessor _IDAccessor =
            FieldAccessor<LightWithIdMonoBehaviour, int>.GetAccessor("_ID");

        private static readonly FieldAccessor<TubeBloomPrePassLightWithId, TubeBloomPrePassLight>.Accessor _tubeBloomPrePassLightAccessor =
            FieldAccessor<TubeBloomPrePassLightWithId, TubeBloomPrePassLight>.GetAccessor("_tubeBloomPrePassLight");

        private static readonly FieldAccessor<TubeBloomPrePassLightWithId, bool>.Accessor _setOnlyOnceAccessor =
            FieldAccessor<TubeBloomPrePassLightWithId, bool>.GetAccessor("_setOnlyOnce");

        private static readonly FieldAccessor<TubeBloomPrePassLightWithId, bool>.Accessor _setColorOnlyAccessor =
            FieldAccessor<TubeBloomPrePassLightWithId, bool>.GetAccessor("_setColorOnly");

        [SerializeField]
        private int _ID = -1;

        [Space]
#pragma warning disable CS8618
        [SerializeField]
        private TubeLight _tubeLight;
#pragma warning restore CS8618

        [SerializeField]
        private bool _setOnlyOnce;

        [SerializeField]
        private bool _setColorOnly;

        protected override void InitializeBaseComponents()
        {
            TubeBloomPrePassLight tubeBloomPrePassLight = _tubeLight.GetBaseComponent<TubeBloomPrePassLight>();
            TubeBloomPrePassLightWithId tubeBloomPrePassLightWithId = AddBaseComponent(GetInstantiator().InstantiateComponent<TubeBloomPrePassLightWithId>(gameObject));
            LightWithIdMonoBehaviour lightWithIdMonoBehaviour = tubeBloomPrePassLightWithId;
            _IDAccessor(ref lightWithIdMonoBehaviour) = _ID;
            _tubeBloomPrePassLightAccessor(ref tubeBloomPrePassLightWithId) = tubeBloomPrePassLight;
            _setOnlyOnceAccessor(ref tubeBloomPrePassLightWithId) = _setOnlyOnce;
            _setColorOnlyAccessor(ref tubeBloomPrePassLightWithId) = _setColorOnly;
        }
    }
}
