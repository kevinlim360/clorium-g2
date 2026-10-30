using System.Collections.Generic;

namespace CameraControl.Devices.Classes;

public class Capability<T>
{
	public virtual T Current { get; set; }

	public virtual List<T> Candidates { get; set; }
}
