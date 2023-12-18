using System;
using System.Collections.Generic;
using System.IO;

public class ConfFileParser
{
    public Dictionary<string, Dictionary<string, string>> Parse(string filePath)
    {
        var config = new Dictionary<string, Dictionary<string, string>>();
        string currentSection = null;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
            {
                // Skip empty lines and comments
                continue;
            }

            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                // New section
                currentSection = trimmedLine[1..^1];
                config[currentSection] = new Dictionary<string, string>();
            }
            else if (currentSection != null)
            {
                // Key-value pair within a section
                var parts = trimmedLine.Split('=', 2);
                if (parts.Length == 2)
                {
                    config[currentSection][parts[0].Trim()] = parts[1].Trim();
                }
            }
        }

        return config;
    }
}

