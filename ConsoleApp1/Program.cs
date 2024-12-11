using System;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Text;
using System.Text.Json;
using Protodef;
using Protodef.Converters;
using Protodef.Enumerable;

class Program
{
    static async Task Main(string[] args)
    {
        string json = """
                      [{
                        "name": "message",
                        "type": "string"
                      }]
                      """;
        var options = new JsonSerializerOptions();
        options.Converters.Add(new DataTypeConverter());
        ProtodefContainer type = new ProtodefContainer(
            JsonSerializer.Deserialize<List<ProtodefContainerField>>(json, options));

        Console.WriteLine();
    }
}