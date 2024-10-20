using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Vivify.Managers;

namespace Vivify.Events;

public class RenderEnumCapturedSetting<TEnum> : EnumCapturedSetting<RenderSettings, TEnum>
    where TEnum : struct, Enum
{
    internal RenderEnumCapturedSetting(string property)
        : base(property)
    {
    }
}

public class RenderColorCapturedSetting : ColorCapturedSetting<RenderSettings>
{
    internal RenderColorCapturedSetting(string property)
        : base(property)
    {
    }
}

public class RenderFloatCapturedSetting : FloatCapturedSetting<RenderSettings>
{
    internal RenderFloatCapturedSetting(string property)
        : base(property)
    {
    }
}

public class RenderIntCapturedSetting : IntCapturedSetting<RenderSettings>
{
    internal RenderIntCapturedSetting(string property)
        : base(property)
    {
    }
}

public class RenderBoolCapturedSetting : BoolCapturedSetting<RenderSettings>
{
    internal RenderBoolCapturedSetting(string property)
        : base(property)
    {
    }
}

public class RenderMaterialCapturedSetting : CapturedSettings<RenderSettings, Material>
{
    internal RenderMaterialCapturedSetting(string property, AssetBundleManager assetBundleManager)
        : base(
            property,
            obj => assetBundleManager.TryGetAsset((string)obj, out Material? material) ? material : null)
    {
    }
}

public class RenderLightCapturedSetting : CapturedSettings<RenderSettings, Light>
{
    internal RenderLightCapturedSetting(string property, PrefabManager prefabManager)
        : base(
            property,
            obj => prefabManager.TryGetPrefab((string)obj, out InstantiatedPrefab? prefab)
                ? prefab.GameObject.GetComponents<Light>().FirstOrDefault(n => n.type == LightType.Directional)
                : null)
    {
    }
}

public class QualityEnumCapturedSetting<TEnum> : EnumCapturedSetting<QualitySettings, TEnum>
    where TEnum : struct, Enum
{
    internal QualityEnumCapturedSetting(string property)
        : base(property)
    {
    }
}

public class QualityIntCapturedSetting : IntCapturedSetting<QualitySettings>
{
    internal QualityIntCapturedSetting(string property)
        : base(property)
    {
    }
}

public class QualityFloatCapturedSetting : FloatCapturedSetting<QualitySettings>
{
    internal QualityFloatCapturedSetting(string property)
        : base(property)
    {
    }
}

public class QualityBoolCapturedSetting : BoolCapturedSetting<QualitySettings>
{
    internal QualityBoolCapturedSetting(string property)
        : base(property)
    {
    }
}

public class EnumCapturedSetting<TClass, TEnum> : CapturedSettings<TClass, TEnum>
    where TClass : class
    where TEnum : struct, Enum
{
    internal EnumCapturedSetting(string property)
        : base(property, Convert)
    {
    }

    private static TEnum Convert(object obj)
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), (int)(float)obj);
    }
}

public class IntCapturedSetting<TClass> : CapturedSettings<TClass, int>
    where TClass : class
{
    internal IntCapturedSetting(string property)
        : base(property, Convert.ToInt32)
    {
    }
}

public class FloatCapturedSetting<TClass> : CapturedSettings<TClass, float>
    where TClass : class
{
    internal FloatCapturedSetting(string property)
        : base(property, Convert.ToSingle)
    {
    }
}

public class BoolCapturedSetting<TClass> : CapturedSettings<TClass, bool>
    where TClass : class
{
    internal BoolCapturedSetting(string property)
        : base(property, Convert)
    {
    }

    private static bool Convert(object obj)
    {
        return System.Convert.ToBoolean(obj);
    }
}

public class ColorCapturedSetting<TClass> : CapturedSettings<TClass, Color>
    where TClass : class
{
    internal ColorCapturedSetting(string property)
        : base(property, Convert)
    {
    }

    private static Color Convert(object obj)
    {
        return (Vector4)obj;
    }
}

public class CapturedSettings<TClass, TProperty>
    where TClass : class
{
    private readonly Func<object, TProperty?> _convert;
    private readonly Func<TProperty> _get;
    private readonly Action<TProperty?> _set;

    private TProperty? _captured;

    internal CapturedSettings(string property, Func<object, TProperty?> convert)
    {
        PropertyInfo propertyInfo = AccessTools.Property(typeof(TClass), property);
        _get = (Func<TProperty>)Delegate.CreateDelegate(typeof(Func<TProperty>), propertyInfo.GetMethod);
        _set = (Action<TProperty?>)Delegate.CreateDelegate(typeof(Action<TProperty>), propertyInfo.SetMethod);
        _convert = convert;
    }

    public void Capture()
    {
        _captured = _get();
    }

    public void Reset()
    {
        _set(_captured);
    }

    public void Set(object value)
    {
        _set(_convert(value));
    }
}
