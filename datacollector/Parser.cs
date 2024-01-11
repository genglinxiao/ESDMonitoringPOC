using Newtonsoft.Json;
namespace DataCollector.Helper{
	public class FieldSpecification
	{
		public string Name { get; set; }
		public int StartAddress { get; set; }
		public int Length { get; set; }
		public string DataType { get; set; }
		public string Unit { get; set; }
		public string Description { get; set; }
	}

	public class DataParser
	{
	    public Dictionary<string, object> ParseData(ushort[] data, List<FieldSpecification> specifications)
	    {
	        var result = new Dictionary<string, object>();

	        foreach (var spec in specifications)
        	{
	            if (spec.StartAddress + spec.Length > data.Length)
        	    {
	                // Handle error: specification exceeds data array bounds
	                continue;
	            }

	            object value = ExtractAndInterpretData(data, spec);
	            result.Add(spec.Name, value);
	        }

        	return result;
	    }

            public object ExtractAndInterpretData(ushort[] data, FieldSpecification spec)
	    {
		    switch (spec.DataType.ToLower())
		    {
		        case "float":
		            return ExtractFloat(data, spec.StartAddress);
		        case "uint":
		            return ExtractUnsignedInt(data, spec.StartAddress);
		        case "flags":
		            return ExtractFlags(data, spec.StartAddress, spec.Length);
		        default:
		            throw new NotImplementedException($"Data type {spec.DataType} is not implemented.");
		    }
		}

		private float ExtractFloat(ushort[] data, int startAddress)
		{
		    if (startAddress + 1 >= data.Length)
		        throw new ArgumentOutOfRangeException("Not enough data to extract float.");

		    byte[] bytes = new byte[4];
		    BitConverter.GetBytes(data[startAddress]).CopyTo(bytes, 0);
		    BitConverter.GetBytes(data[startAddress + 1]).CopyTo(bytes, 2);

		    return BitConverter.ToSingle(bytes, 0);
		}

		private uint ExtractUnsignedInt(ushort[] data, int startAddress)
		{
			///Possible Endianness issue!!!!!!!!!!!!!!
			///Possible Endianness issue!!!!!!!!!!!!!!
			///Possible Endianness issue!!!!!!!!!!!!!!
			///Possible Endianness issue!!!!!!!!!!!!!!
		    if (startAddress >= data.Length)
		        throw new ArgumentOutOfRangeException("Not enough data to extract unsigned int.");

		    return data[startAddress];
		}

		private bool[] ExtractFlags(ushort[] data, int startAddress, int length)
		{
		    if (startAddress >= data.Length)
		        throw new ArgumentOutOfRangeException("Not enough data to extract flags.");

		    var flags = new bool[length];
		    ushort flagData = data[startAddress];

		    for (int i = 0; i < length; i++)
		    {
		        flags[i] = (flagData & (1 << i)) != 0;
		    }

		    return flags;
		}
	}

	public class JsonSpecificationReader
	{
	    public List<FieldSpecification> ReadSpecifications(string filePath)
	    {
	        try
	        {
	            string json = File.ReadAllText(filePath);
	            return JsonConvert.DeserializeObject<List<FieldSpecification>>(json);
	        }
	        catch (Exception ex)
	        {
	            // Handle exceptions (e.g., file not found, invalid JSON)
	            Console.WriteLine("Error reading specification file: " + ex.Message);
	            return new List<FieldSpecification>();
	        }
	    }
	}

}
