using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class AspMvcActionSelectorAttribute : Attribute
{
}
