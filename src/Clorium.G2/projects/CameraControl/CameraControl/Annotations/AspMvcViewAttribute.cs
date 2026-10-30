using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class AspMvcViewAttribute : Attribute
{
}
