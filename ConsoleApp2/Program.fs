open System
open System.Net.Http
open System.Text

let makeRequest () =
    let url = "https://qwen-qwen2-5-coder-artifacts.hf.space/gradio_api/call/generation_code"
    let data = "{ \"data\": [\"Hi\"] }"
    
    use client = new HttpClient()
    use content = new StringContent(data, Encoding.UTF8, "application/json")
    
    // Синхронный POST запрос
    let response = client.PostAsync(url, content).Result
    
    // Проверка успешности запроса
    if response.IsSuccessStatusCode then
        let responseBody = response.Content.ReadAsStringAsync().Result
        // Извлечение EVENT_ID из ответа
        let eventId = responseBody.Split('"').[3]
        printfn "Event ID: %s" eventId
        
        // Синхронный запрос с использованием EVENT_ID
        for i in 1..10 do            
            let urlWithEventId = sprintf "https://qwen-qwen2-5-coder-artifacts.hf.space/gradio_api/call/generation_code/%s" eventId
            let finalResponse = client.GetStringAsync(urlWithEventId).Result
            printfn "Final response: %s" finalResponse
    else
        printfn "Request failed with status code: %d" (int response.StatusCode)

// Вызов функции
makeRequest ()