module MinecraftDataFSharp.LLMService

open System.IO
open System.Net.Http
open System.Net.ServerSentEvents
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FSharp.Control

type AsyncBuilder with
    member inline _.Bind(x: 'x System.Threading.Tasks.Task, x2yA: 'x -> 'y Async) = async.Bind(Async.AwaitTask x, x2yA)

let private baseUrl = "https://qwen-qwen2-5.hf.space/call/model_chat"

let Predict (prompt: string, systemMessage: string) =
    async {
        let request = {| data = [| box prompt; box [||]; box systemMessage; box "Coder-7B" |] |}
        let jsonContent = JsonSerializer.Serialize(request)
        use client = new HttpClient()

        use! response =
            client.PostAsync(baseUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"))
            |> Async.AwaitTask

        response.EnsureSuccessStatusCode() |> ignore
        let! responseBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        use json = JsonDocument.Parse(responseBody)
        let eventId = json.RootElement.GetProperty("event_id").GetString()
        let sseUrl = $"{baseUrl}/{eventId}"
        use! responseStream = client.GetStreamAsync(sseUrl) |> Async.AwaitTask

        let parser = SseParser.Create(responseStream)
        let! eventOption = 
            parser.EnumerateAsync()
            |> TaskSeq.tryFind(fun x -> x.EventType = "complete")
        let getText (json: string) =
            use document = JsonDocument.Parse(json)
            document.RootElement[1].[0].[1].GetProperty("text").GetString()

        return eventOption |> Option.map (fun x -> getText x.Data)
    }
