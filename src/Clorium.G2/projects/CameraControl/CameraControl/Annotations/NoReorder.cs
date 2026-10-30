using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.All)]
public sealed class NoReorder : Attribute
{
}
