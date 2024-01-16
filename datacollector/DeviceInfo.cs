using Prometheus;
namespace DataCollector.Helper
{
	public class DeviceInfo
	{
	    public readonly string Id;
	    public string ConnectStr { get; set; }
	    public ushort BaseAddr { get; set; }
	    public ushort DataFrameLength {get; set;}
	    public Counter cAttempts{get; set;}
	    public Counter cSuccess{get; set;}
    
	    public DeviceInfo(string id, string connectStr, Counter ca, Counter cs, ushort baseAddr = 0, ushort frameLength = 1008)
	    {
	        Id = id;
		ConnectStr = connectStr;
	        BaseAddr = baseAddr;
	        DataFrameLength = frameLength;
		cAttempts = ca;
		cSuccess = cs;
	    }
	}
}
