using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Xml;
using System.Xml.Linq;

namespace DirectumTestTask
{
	internal class Program
	{
		static JsonNode? GetValueFromJson(string jsonString, string source, string type)
		{
			using (JsonDocument jsonDoc = JsonDocument.Parse(jsonString))
			{
				var property = jsonDoc.RootElement.GetProperty(source);
				switch (type.ToLower())
				{
					case "integer":
						if (int.TryParse(property.GetString(), out var num))
							return num;
						throw new InvalidOperationException("Cant convert to 'int' because number is not integer");
					case "string":
						return property.GetString();
					case "datetime":
						if (DateTime.TryParse(property.GetString(), out var date))
							return date;
						if (property.GetString() == null)
							return null;
						throw new InvalidOperationException("Cant convert to 'DateTime'");
					case "boolean":
						return property.GetBoolean();
				}
				throw new InvalidOperationException($"Cant convert to '{type}'");
			}
		}

		static JsonObject? BuildJsonFromXmlConfig(XElement xmlObject, string jsonString)
		{
			var jsonObject = new JsonObject();
			var properties = xmlObject.Element("properties");
			if (properties != null)
			{
				foreach (XElement property in properties.Elements("property"))
				{
					var propertyName = property.Attribute("name");
					var source = property.Attribute("source");
					var type = property.Attribute("type");
					if (propertyName == null || source == null || type == null)
						throw new XmlException("Missing attributes in 'property' element");

					var jsonValue = GetValueFromJson(jsonString, source.Value, type.Value);
					jsonObject.Add(propertyName.Value, jsonValue);
				}
			}
			else
			{
				throw new XmlException("There is no 'properties' in XML file");
			}

			return jsonObject;
		}
		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				args = new string[] { "jsonData.json", "xmlData.xml"};
				Console.WriteLine("Wrong number of argumetns\nExample of usage: DirectumTestTask 'yourJsonFileName'.json 'yourXmlFileName'.xml");
				return;
			}

			string jsonFilePath = args[0];
			string xmlFilePath = args[1];

			if (!File.Exists(jsonFilePath))
			{
				Console.WriteLine($"Json file '{jsonFilePath}' not found");
				return;
			}

			if (!File.Exists(xmlFilePath))
			{
				Console.WriteLine($"Xml file '{xmlFilePath}' not found");
				return;
			}

			try
			{
				string jsonString = File.ReadAllText(jsonFilePath);

				var xmlConfig = XDocument.Load(xmlFilePath);
				var xmlObject = xmlConfig.Element("object");
				if (xmlObject == null)
					throw new XmlException("No 'object' found");
				var fileName = xmlObject?.Attribute("name")?.Value;
				if (fileName == null || fileName == "")
					fileName = "default"; //здесь можно и ошибку выкинуть, но я решил сделать "базовое" имя для файла

				var jsonObject = BuildJsonFromXmlConfig(xmlObject, jsonString);

				var outputJsonFileName = $"{fileName}.json";

				var jsonOptions = new JsonSerializerOptions
				{
					Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
					WriteIndented = true
				};
				File.WriteAllText(outputJsonFileName, JsonSerializer.Serialize(jsonObject, jsonOptions));

				FileInfo fileInfo = new FileInfo(outputJsonFileName);
				string fullPath = fileInfo.FullName;

				Console.WriteLine($"Completed. Data saved to {fullPath}");
			}
			catch (XmlException ex)
			{
				Console.WriteLine($"Bad XML configuration: {ex.Message}");
			}
			catch (JsonException ex)
			{
				Console.WriteLine($"Bad JSON file: {ex.Message}");
			}
			catch (KeyNotFoundException ex)
			{
				Console.WriteLine($"JSON file dont have 'source' from XML: {ex.Message}");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"{ex.GetType()}: {ex.Message}");
			}
		}
	}
}