namespace CameraControl.Core.Classes;

public class Exiv2Data
{
	public string Tag { get; set; }

	public string Type { get; set; }

	public string Length { get; set; }

	public string Value { get; set; }

	public override string ToString()
	{
		return Tag + "|" + Type + "|" + Length + "|" + Value;
	}
}
