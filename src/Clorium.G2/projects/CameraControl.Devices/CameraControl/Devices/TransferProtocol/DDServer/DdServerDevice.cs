using System.Text;

namespace CameraControl.Devices.TransferProtocol.DDServer;

public class DdServerDevice
{
	public int VendorId { get; set; }

	public int ProductId { get; set; }

	public string VendorName { get; set; }

	public string ProductName { get; set; }

	public DdServerDevice(byte[] data, int offset)
	{
		int num = offset;
		VendorId = data[num] | (data[num + 1] << 8);
		num += 2;
		ProductId = data[num] | (data[num + 1] << 8);
		num += 2;
		string[] array = Encoding.ASCII.GetString(data, num, data.Length - num).Split(new char[1]);
		if (array.Length > 1)
		{
			VendorName = array[0];
			ProductName = array[1];
		}
	}
}
