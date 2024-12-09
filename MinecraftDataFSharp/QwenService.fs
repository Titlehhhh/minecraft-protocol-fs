module MinecraftDataFSharp.QwenService

open System.IO
open System.Net.Http
open System.Text
open System.Text.Json
open System.Threading.Tasks
type AsyncBuilder with
    member inline _.Bind (x : 'x System.Threading.Tasks.Task, x2yA : 'x -> 'y Async) =
       async.Bind(Async.AwaitTask x, x2yA)

let private baseUrl = "https://qwen-qwen2-5.hf.space/call/model_chat"

let Predict (prompt: string, systemMessage: string) : Async<string option>=    
    async {
        let request = {| data = [| box prompt; box [||]; box systemMessage; box "72B" |] |}
        let jsonContent = JsonSerializer.Serialize(request)
        use client = new HttpClient()
        use! response =
            client.PostAsync(baseUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json")) |> Async.AwaitTask            

        response.EnsureSuccessStatusCode() |> ignore
        let! responseBody = response.Content.ReadAsStringAsync() |> Async.AwaitTask 
        use json = JsonDocument.Parse(responseBody)
        let eventId = json.RootElement.GetProperty("event_id").GetString()
        let sseUrl = $"{baseUrl}/{eventId}"
        use! responseStream = client.GetStreamAsync(sseUrl) |> Async.AwaitTask 
        use reader = new StreamReader(responseStream)
        
        let rec readLoop () = async {
            if reader.EndOfStream then return None else
            let (|StartWith|_|) expected actual =
                if (actual : string).StartsWith(expected : string)
                then Some (actual.Substring expected.Length)
                else None
            match! reader.ReadLineAsync() with
            | StartWith "event: complete" _ ->
                // Важно: следующая строка не будет прочитана, если не схвачен `event: complete`
                match! reader.ReadLineAsync() with
                | StartWith "data: " jsonData  ->
                    use doc = JsonDocument.Parse jsonData
                    let textElement = doc.RootElement[1].[0].[1].GetProperty("text").GetString()                            
                    return Some textElement
                | _ ->
                    return failwith "Failed to parse response" 
            | _ ->
                return! readLoop ()
        } 
        return! readLoop ()
    } 
