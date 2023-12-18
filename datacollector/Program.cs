using System;

class Program
{
    static void Main(string[] args)
    {
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
        return config.ContainsKey("Input") && config.ContainsKey("Process") && config.ContainsKey("Process");
        // Potentially more checks...
    }
}

