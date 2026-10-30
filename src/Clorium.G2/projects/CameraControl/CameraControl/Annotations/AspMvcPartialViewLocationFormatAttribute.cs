using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class AspMvcPartialViewLocationFormatAttribute : Attribute
{
	public string Format { get; private set; }

	public AspMvcPartialViewLocationFormatAttribute(string format)
	{
		Format = format;
	}
}
