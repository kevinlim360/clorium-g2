using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class RegexPatternAttribute : Attribute
{
}
