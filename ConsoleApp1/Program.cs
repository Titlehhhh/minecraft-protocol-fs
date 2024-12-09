using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

class Program
{
    static string RandomHash()
    {
        //Generate random string, exmaple jv0o09ku7g
        Random random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 10)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    static async Task Main(string[] args)
    {
        string promtPah = "C:\\Users\\Title\\RiderProjects\\MinecraftDataFSharp\\MinecraftDataFSharp\\BasePromt.txt";
        string promt = File.ReadAllText(promtPah);

        string systemMessage = "You C# code generator for Minecraft protocol library in C# based on provided JSON. ";

        string url = "https://qwen-qwen2-5.hf.space/call/model_chat";
        var request = new
        {
            data = new object[]
            {
                promt,
                Array.Empty<object>(),
                systemMessage,
                "72B"
            }
        };
        string jsonContent = JsonSerializer.Serialize(request);
        string eventId;
        using var client = new HttpClient();

        var response =
            await client.PostAsync(url, new StringContent(jsonContent, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(responseBody);
        eventId = json.RootElement.GetProperty("event_id").GetString();

        string sseUrl = "https://qwen-qwen2-5.hf.space/call/model_chat/" + eventId;
        await using (var responseStream = await client.GetStreamAsync(sseUrl))
        {
            using var reader = new StreamReader(responseStream);
            while (!reader.EndOfStream)
            {
                string line = await reader.ReadLineAsync();
                if (line.StartsWith("event: complete"))
                {
                    line = await reader.ReadLineAsync();
                    int dataIndex = line.IndexOf("data: ");
                    if (dataIndex != -1)
                    {
                        string jsonData = line.Substring(dataIndex + 6);
                        using (JsonDocument doc = JsonDocument.Parse(jsonData))
                        {
                            var textElement = doc.RootElement[1][0][1].GetProperty("text").GetString();
                            Console.WriteLine(textElement);
                        }
                    }
                }
            }
        }
    }
}