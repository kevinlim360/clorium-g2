using System;

namespace CameraControl.Annotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class ValueProviderAttribute : Attribute
{
	[NotNull]
	public string Name { get; private set; }

	public ValueProviderAttribute(string name)
	{
		Name = name;
	}
}
