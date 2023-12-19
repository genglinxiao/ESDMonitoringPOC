using System;
using System.Net;
using System.Net.Sockets;
using Modbus;
using Modbus.Device;

class Program
{
    private const int DefaultPort = 502;
    static void Main(string[] args)
    {
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
                default:
                    Console.WriteLine("Unknown option. Use '--help' for usage information.");
                    return;
            }
	}
        const string configFilePath = "collector.conf";

        try
        {
            var parser = new ConfFileParser();
            var config = parser.Parse(configFilePath);

            if (!IsValidConfiguration(config))
            {
                Console.WriteLine("Invalid configuration. Please check the config file.");
                Environment.Exit(1); // Exit with a non-zero status to indicate an error
            }

            // Proceed with the rest of the program

	    string slaveIpsStr=config["General"]["slaveIps"];
	    string[] slaveIps=slaveIpsStr.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
		    .Select(item => item.Trim())
		    .ToArray();

	    ModbusTcpMasterReadInputs(slaveIps[0]);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading configuration: {ex.Message}");
            Environment.Exit(1); // Exit with a non-zero status to indicate an error
        }

	

    }

    static bool IsValidConfiguration(Dictionary<string, Dictionary<string, string>> config)
    {
        // Implement validation logic here
        return config.ContainsKey("Input") && config.ContainsKey("Process") && config.ContainsKey("Process")&& config.ContainsKey("General");
        // Potentially more checks...
    }
    /// <summary>
    ///     Simple Modbus TCP master read inputs example.
    /// </summary>
    public static void ModbusTcpMasterReadInputs(string connectionString)
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


        using (TcpClient client = new TcpClient(ipAddressStr, port))
        {
            ModbusIpMaster master = ModbusIpMaster.CreateIp(client);

            // read five input values
            ushort startAddress = 100;
            ushort numInputs = 5;
            bool[] inputs = master.ReadInputs(startAddress, numInputs);

            for (int i = 0; i < numInputs; i++)
            {
                Console.WriteLine($"Input {(startAddress + i)}={(inputs[i] ? 1 : 0)}");
            }
        }
    }

    static void PrintVersion()
    {
        Console.WriteLine("DataCollector version 1.0.0");
        // Replace with actual version retrieval logic if necessary
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

