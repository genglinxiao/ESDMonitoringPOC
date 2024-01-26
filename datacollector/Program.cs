using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;

using Modbus;
using Modbus.Device;
using Prometheus;

using DataCollector.Conf;
using DataCollector.Helper;

class Program
{
    private const int DefaultPort = 502;
    private static Timer? timer;
    private static ManualResetEvent quitEvent = new ManualResetEvent(false);

    static void Main(string[] args)
    {
    	string[] slaves=new string[]{""};
	
	using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Information); // Set the minimum log level here
        });

        ILogger logger = loggerFactory.CreateLogger<Program>();

	//Command line processing
	if (args.Length > 0)
        {
            switch (args[0].ToLower())
            {
                case "--version":
                    PrintVersion();
                    return;
                case "--help":
                    PrintHelp();
                    return;
                // Add other cases for different options
		case "--modpoll":
		    throw new NotImplementedException();
                default:
                    Console.WriteLine("Unknown option. Use '--help' for usage information.");
                    return;
            }
	}

	//Conf file processing
        const string configFilePath = "collector.conf";
        var parser = new ConfFileParser();
        Dictionary<string, Dictionary<string, string>> config = null;
	int port = 0;

        try
        {
	    config = parser.Parse(configFilePath);

            if (!IsValidConfiguration(config))
            {
                Console.WriteLine("Invalid configuration. Please check the config file.");
                Environment.Exit(1); // Exit with a non-zero status to indicate an error
            }
	    port = int.Parse(config["General"]["port"]);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            Environment.Exit(1); // Exit with a non-zero status to indicate an error
        }

	
	using var server = new MetricServer(port: port);
	Metrics.SuppressDefaultMetrics();
	try
	{
	    server.Start();
	}
	catch (HttpListenerException ex)
	{
	    Console.WriteLine($"Failed to start metric server: {ex.Message}");
	    return;
	}

	//remove the general section
	if (config!=null)
		config.Remove("General");

	//For the rest part of config, check each slave device
	foreach (var slaveConfig in config)
	{
	    string slaveKey = slaveConfig.Key;
	    Dictionary<string,string> slave = slaveConfig.Value;

	    Counter ChecksAttempted = Metrics.CreateCounter("checks_attempted_for_"+slaveKey, "Numbers of checks attempted on the device: "+slaveKey);
	    Counter ChecksSucceeded = Metrics.CreateCounter("checks_succeeded_on_"+slaveKey, "Numbers of checks succeeded on the device: "+slaveKey);

	    DeviceInfo di = new DeviceInfo(slave["id"], slave["slaveAddr"], ChecksAttempted, ChecksSucceeded);

            var reader = new JsonSpecificationReader();
	    List<FieldSpecification> specifications = reader.ReadSpecifications(slave["spec_file"]);
	    var measurements = new List<(ushort, ushort, Gauge, FieldSpecification)>();
	    //Dictionary<ushort, Gauge> measurements =new Dictionary<ushort, Gauge>();

	    int interval=60;
	    if(int.TryParse(slave["interval"], out int result))
	    {
		    interval = result;
	    }

		foreach (var field in specifications)
		{
                   measurements.Add((field.StartAddress,field.Length, Metrics.CreateGauge(field.Name, field.Description), field));
		}

	    timer = new Timer(GetDeviceDataAndPublish,(slave["slaveAddr"],ChecksAttempted, ChecksSucceeded,  measurements), TimeSpan.Zero, TimeSpan.FromSeconds(interval));

	}
	
	//Wait for ctrl+c
        Console.CancelKeyPress += (sender, e) => CancelKeyPressHandler(sender, e);
        Console.WriteLine("Press 'Ctrl+C' to exit.");
        quitEvent.WaitOne();
    }

    private static void GetDeviceDataAndPublish(object state)
    {
	    var (conStr, ChecksAttempted, ChecksSucceeded, measurements) = ((string,Counter, Counter, List<(ushort, ushort, Gauge, FieldSpecification)>))state;
	    ChecksAttempted.Inc();
	    try {
	    foreach (var (addr, len, gauge, field) in measurements)
	    {

		    Console.WriteLine(field.Description);
		    List<ushort> result = ReadMeasures(conStr, 1, addr, len);
		    switch(field.DataType)
		    {
			    case "float":
				    gauge.Set(ConvertToFloat(result));
				    Console.WriteLine(ConvertToFloat(result));
				    break;
			    case "i32":
				    gauge.Set(ConvertToInt(result,0));
				    break;
			    case "i16":
				    break;
			    case "u32":
				    break;
			    case "u16":
				    gauge.Set(result[0]);
				    break;
			    case "chars":
				    Console.WriteLine(ConvertToCharArray(result));
				    break;
			    case "flags":
				    break;
		    }

		    //gauge.Set(
	    }
	    ChecksSucceeded.Inc();
	    }
	    catch (Exception ex){
		    Console.WriteLine("Exception Occurred.");
	    }
	    /*
	    if (state is DeviceInfo di)
	    {
 	    	Console.WriteLine("Querying data at: " + DateTime.Now);
		di.cAttempts.Inc();
	
		List<ushort> dataFrame = ReadInputRegisters(di.ConnectStr, di.BaseAddr, len);

		di.cSuccess.Inc();
        	Console.WriteLine(DateTime.Now + "Data extracted successfully.");
	    }else
	    {
		    throw new ArgumentException("Expecting a DeviceInfo class here.");
	    }*/
    }

    private static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed, shutting down.");
        timer?.Dispose(); // Stop the timer and clean up
	quitEvent.Set();

        e.Cancel = false;
    }


    private static bool IsValidConfiguration(Dictionary<string, Dictionary<string, string>> config)
    {
        // Implement validation logic here
        return config.ContainsKey("General");
        // Potentially more checks...
    }

    public static List<ushort> ReadMeasures(string connectionString, byte unitId, ushort baseAddress, ushort length)
    {

	if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));
        }

        string ipAddressStr;
        int port;

        int colonIndex = connectionString.IndexOf(':');
        if (colonIndex >= 0)
        {
            ipAddressStr = connectionString.Substring(0, colonIndex);
            if (!int.TryParse(connectionString.Substring(colonIndex + 1), out port))
            {
                throw new FormatException("Invalid port number.");
            }
        }
        else
        {
            ipAddressStr = connectionString;
            port = DefaultPort;
        }

        if (!IPAddress.TryParse(ipAddressStr, out IPAddress ipAddress))
        {
            throw new FormatException("Invalid IP address.");
        }

	var resultList = new List<ushort>();

        using (TcpClient client = new TcpClient(ipAddressStr, port))
        {
            ModbusIpMaster master = ModbusIpMaster.CreateIp(client);

            	ushort[] inputs = master.ReadInputRegisters(baseAddress, length);

		resultList.AddRange(inputs);
        }
	return resultList;
    }

    static void PrintVersion()
    {
        Console.WriteLine("DataCollector version 1.0.0");
    }

    static void PrintHelp()
    {
        Console.WriteLine("DataCollector Usage:");
        Console.WriteLine("  --version   Show the version information.");
        Console.WriteLine("  --help      Show this help message.");
        // Add other options and their descriptions
    }
    private static short ConvertToShort(ushort value)
    {
    	// If the MSB is set, treat it as a negative number
    	if ((value & 0x8000) != 0) // Check if MSB is set
    	{
        	return unchecked((short)value); // Convert using unchecked to preserve the sign
    	}
    	else
    	{
        	return (short)value;
    	}
    }
    private static int ConvertToInt(List<ushort> data, int startIndex)
    {
	    uint upper = (uint)data[startIndex];
	    uint lower = (uint)data[startIndex + 1];
	    
	    uint combined = (upper << 16) | lower;

	    // If the MSB is set, treat it as a negative number
	    if ((upper & 0x8000) != 0) // Check if MSB of upper ushort is set
	    {
	        return unchecked((int)combined); // Convert using unchecked to preserve the sign
	    }
	    else
	    {
	        return (int)combined;
	    }
    }

    private static float ConvertToFloat(List<ushort> data)
    {
	    if (data.Count != 2)
	    {
		    foreach (ushort dp in data)
		    {
			    Console.WriteLine("You have {0} data points",data.Count);
			    Console.Write("0x{0:X4} ", dp);
		    }
		    Console.WriteLine();
	        throw new ArgumentException("List must contain exactly 2 elements for float conversion.");
	    }
	    // Combine the two ushort values into a single uint
	    uint combined = ((uint)data[0] << 16) | data[1];

	    // Reinterpret the uint as a float
	    return BitConverter.ToSingle(BitConverter.GetBytes(combined), 0);
    }
    static char[] ConvertToCharArray(List<ushort> data)
    {
	    char[] chars = new char[data.Count * 2];
	    for (int i = 0; i < data.Count; i++)
	    {
		    //chars[i * 2] = (char)(data[i] & 0xFF);          // Lower byte
		    //chars[i * 2 + 1] = (char)((data[i] >> 8) & 0xFF); // Upper byte
		    chars[i * 2 + 1] = (char)(data[i] & 0xFF);          // Lower byte
		    chars[i * 2] = (char)((data[i] >> 8) & 0xFF); // Upper byte
	    }
	    return chars;
    }

}

