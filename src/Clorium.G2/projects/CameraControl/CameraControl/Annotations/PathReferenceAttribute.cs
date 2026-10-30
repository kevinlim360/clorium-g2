using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class PathReferenceAttribute : Attribute
{
	public string BasePath { get; private set; }

	public PathReferenceAttribute()
	{
	}

	public PathReferenceAttribute([PathReference] string basePath)
	{
		BasePath = basePath;
	}
}
