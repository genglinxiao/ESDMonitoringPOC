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
using Newtonsoft.Json;

class Program
{
    private const int DefaultPort = 502;
    private static List<Timer> timers = new List<Timer>();
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
        string configFilePath = "collector.conf";

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
		    return;
		case "--conf":
		    if (args.Length==2)
		    {
			    configFilePath = args[1];
		    }
		    else
		    {
			    Console.WriteLine("No configuration or wrong command line format.");
			    return;
		    }
		    break;
                default:
                    Console.WriteLine("Unknown option. Use '--help' for usage information.");
                    return;
            }
	}

	//Conf file processing
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

            var reader = new JsonSpecificationReader();
	    List<FieldSpecification> specifications = reader.ReadSpecifications(slave["spec_file"]);
	    var measurements = new List<(ushort, ushort, Gauge, FieldSpecification)>();
	    var flags = new List<(ushort, ushort, List<Counter>, FieldSpecification)>();

	    int interval=60; //default to 60 seconds if not specificed.
	    if(int.TryParse(slave["interval"], out int result))
	    {
		    interval = result;
	    }

		foreach (var field in specifications)
		{
			if (field.DataType!="flags")
			{
				var gauge=Metrics.CreateGauge(field.Name, field.Description,
                                                                new GaugeConfiguration
                                                                {
                                                                  LabelNames = new[] { "id", "model", "location" }
                                                                });
				measurements.Add((field.StartAddress,field.Length, gauge, field));
				
			}
			else
			{
				//Create one flag for each bit.
				//And count the time internals before back to normal.
				var promFlags = new List<Counter>();
				for (int i=0;i<field.Length*16;i++)
				{
					Counter c=Metrics.CreateCounter(field.Name, field.Name+i.ToString());
					promFlags.Add(c);
				}
				flags.Add((field.StartAddress, field.Length, promFlags, field));
			}
		}

	    var timer = new Timer(GetDeviceDataAndPublish,(slave["slaveAddr"], slave["unitId"], slave["id"], slave["model"], slave["location"], ChecksAttempted, ChecksSucceeded,  measurements), TimeSpan.Zero, TimeSpan.FromSeconds(interval));
	    timers.Add(timer);
	}
	
	//Wait for ctrl+c
        Console.CancelKeyPress += (sender, e) => CancelKeyPressHandler(sender, e);
        Console.WriteLine("Press 'Ctrl+C' to exit.");
        quitEvent.WaitOne();
    }

    private static void GetDeviceDataAndPublish(object state)
    {
	    var (conStr, unitIdString, deviceId, deviceModel, deviceLocation,  ChecksAttempted, ChecksSucceeded, measurements) = ((string, string, string, string, string, Counter, Counter, List<(ushort, ushort, Gauge, FieldSpecification)>))state;
	    byte unitId=1;
	    Byte.TryParse(unitIdString,out unitId);

	    ChecksAttempted.Inc();
	    try {
	    foreach (var (addr, len, gauge, field) in measurements)
	    {

		    Console.Write(field.Description);
		    Console.Write(" : ");
		    List<ushort> result = ReadMeasures(conStr, unitId, addr, len);
		    switch(field.DataType)
		    {
			    case "float":
				    gauge.WithLabels(deviceId, deviceModel, deviceLocation).Set(ConvertToFloat(result));
				    Console.WriteLine(ConvertToFloat(result));
				    break;
			    case "i32":
				    gauge.WithLabels(deviceId, deviceModel, deviceLocation).Set(ConvertToInt(result,0));
				    Console.WriteLine(ConvertToInt(result,0));
				    break;
			    case "i16":
				    short temp=unchecked((short)result[0]);
				    gauge.WithLabels(deviceId, deviceModel, deviceLocation).Set(temp);
				    Console.WriteLine(temp);
				    break;
			    case "u32":
				    uint inum = ((uint)result[1]<<16)|result[0];
				    gauge.WithLabels(deviceId, deviceModel, deviceLocation).Set(inum);
				    Console.WriteLine(inum);
				    break;
			    case "u16":
				    gauge.WithLabels(deviceId, deviceModel, deviceLocation).Set(result[0]);
				    Console.WriteLine(result[0]);
				    break;
			    case "chars":
				    Console.WriteLine(ConvertToCharArray(result));
				    break;
			    case "flags":
				    //Should never come here.
				    break;
		    }

	    }
	    ChecksSucceeded.Inc();
	    }
	    catch (Exception ex){
		    Console.WriteLine("Exception Occurred.");
	    }
    }

    private static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed, shutting down.");
	foreach (var timer in timers)
	{
		timer.Dispose();
	}
	timers.Clear();
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

            	ushort[] results;
		if (baseAddress>=40000)
		{
			results = master.ReadHoldingRegisters(unitId, baseAddress, length);
		}
		else
		{
			results = master.ReadInputRegisters(unitId, baseAddress, length);
		}	

		resultList.AddRange(results);
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
        Console.WriteLine("  --conf      <path to your configuration file.");
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



	public static void DumpObjectToConsole(object obj)
	{
	    string jsonString = JsonConvert.SerializeObject(obj, Formatting.Indented); // Pretty print the JSON
	    Console.WriteLine(jsonString);
	}


}

