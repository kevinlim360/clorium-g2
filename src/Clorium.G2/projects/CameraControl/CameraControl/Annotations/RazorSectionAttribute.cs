using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class RazorSectionAttribute : Attribute
{
}
