using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Vivify.Managers;

public interface ICapturedSetting
{
    public void Capture();

    public void Reset();

    public void Set(object value);
}

public class EnumCapturedSetting<TClass, TEnum> : CapturedSetting<TClass, TEnum>
    where TClass : class
    where TEnum : struct, Enum
{
    internal EnumCapturedSetting(string property)
        : base(property, Convert)
    {
    }

    private static TEnum Convert(object obj)
    {
        return (TEnum)Enum.ToObject(typeof(TEnum), obj);
    }
}

public class IntCapturedSetting<TClass> : CapturedSetting<TClass, int>
    where TClass : class
{
    internal IntCapturedSetting(string property)
        : base(property, Convert.ToInt32)
    {
    }
}

public class FloatCapturedSetting<TClass> : CapturedSetting<TClass, float>
    where TClass : class
{
    internal FloatCapturedSetting(string property)
        : base(property, Convert.ToSingle)
    {
    }
}

public class BoolCapturedSetting<TClass> : CapturedSetting<TClass, bool>
    where TClass : class
{
    internal BoolCapturedSetting(string property)
        : base(property, Convert)
    {
    }

    private static bool Convert(object obj)
    {
        return (bool)obj;
    }
}

public class ColorCapturedSetting<TClass> : CapturedSetting<TClass, Color>
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

public class CapturedSetting<TClass, TProperty> : ICapturedSetting
    where TClass : class
    where TProperty : struct
{
    private readonly Func<object, TProperty> _convert;
    private readonly Func<TProperty> _get;
    private readonly Action<TProperty> _set;

    private TProperty _captured;

    internal CapturedSetting(string property, Func<object, TProperty> convert)
    {
        PropertyInfo propertyInfo = AccessTools.Property(typeof(TClass), property);
        _get = (Func<TProperty>)Delegate.CreateDelegate(typeof(Func<TProperty>), propertyInfo.GetMethod);
        _set = (Action<TProperty>)Delegate.CreateDelegate(typeof(Action<TProperty>), propertyInfo.SetMethod);
        _convert = convert;
    }

    public void Capture()
    {
        _get();
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
