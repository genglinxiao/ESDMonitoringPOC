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

        try
        {
	    config = parser.Parse(configFilePath);

            if (!IsValidConfiguration(config))
            {
                Console.WriteLine("Invalid configuration. Please check the config file.");
                Environment.Exit(1); // Exit with a non-zero status to indicate an error
            }

	    ////////// to be removed ////////
	    //string slaveIpsStr=config["General"]["slaveIps"];
	    //slaves=slaveIpsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
		//    .Select(item => item.Trim())
		  //  .ToArray();
	    ///di = new DeviceInfo(config["General"]["id"],slaves[0]);
	    ////////// to be removed ////////

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            Environment.Exit(1); // Exit with a non-zero status to indicate an error
        }

	//Start prometheus
	using var server = new MetricServer(port: 1235);
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

	    int interval=60;
	    if(int.TryParse(slave["interval"], out int result))
	    {
		    interval = result;
	    }

	    timer = new Timer(GetDeviceDataAndPublish, di, TimeSpan.Zero, TimeSpan.FromSeconds(interval));

	}
	
	//Wait for ctrl+c
        Console.CancelKeyPress += (sender, e) => CancelKeyPressHandler(sender, e);
        Console.WriteLine("Press 'Ctrl+C' to exit.");
        quitEvent.WaitOne();
    }

    private static void GetDeviceDataAndPublish(object state)
    {
	    if (state is DeviceInfo di)
	    {
 	    	Console.WriteLine("Querying data at: " + DateTime.Now);
		di.cAttempts.Inc();
	
		List<ushort> dataFrame = ReadHoldingRegisters(di.ConnectStr, di.BaseAddr, di.DataFrameLength);

		di.cSuccess.Inc();
        	Console.WriteLine(DateTime.Now + "Data extracted successfully.");
	    }else
	    {
		    throw new ArgumentException("Expecting a DeviceInfo class here.");
	    }
    }

    private static void CancelKeyPressHandler(object sender, ConsoleCancelEventArgs e)
    {
        Console.WriteLine("Ctrl+C pressed, shutting down.");
        timer?.Dispose(); // Stop the timer and clean up
	quitEvent.Set();

        e.Cancel = false;
    }


    static bool IsValidConfiguration(Dictionary<string, Dictionary<string, string>> config)
    {
        // Implement validation logic here
        return config.ContainsKey("General");
        // Potentially more checks...
    }

    public static List<ushort> ReadHoldingRegisters(string connectionString, ushort baseAddress, ushort length)
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

	    for (int i=0;i<length/125 +1;i++)
	    {
            	ushort startAddress =(ushort)( baseAddress + i*125);
            	ushort numInputs = 125;
            	ushort[] inputs = master.ReadHoldingRegisters(startAddress, numInputs);
		//bool[] inputs = master.ReadInputs(startAddress, numInputs);

		resultList.AddRange(inputs);
            	for (int j = 0; j < numInputs; j++)
            	{
                	//Console.WriteLine($"Input {(startAddress + j)}={(inputs[j])}");
                	Console.Write(".");
            	}
	    }
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
        Console.WriteLine("  --modpoll   Use modpoll as external ModBus handler");
        // Add other options and their descriptions
    }
}

