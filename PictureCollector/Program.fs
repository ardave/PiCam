open System
open System.Diagnostics
open System.Net.Http
open System.Threading
open MMALSharp
open MMALSharp.Common
open MMALSharp.Handlers


let args = Environment.GetCommandLineArgs()
let sw = Stopwatch()
let client = new HttpClient()

let notify counter = 
    let msg = sprintf $"Captured image #%i{counter} in %i{sw.ElapsedMilliseconds} ms" 
    printfn "%s" msg
    use httpContent = new StringContent(msg)
    let response = client.PostAsync("https://azure", httpContent).Result
    if response.IsSuccessStatusCode then
        printfn "Successfully notified API."
    else
        printfn $"API notification failed with status code %A{response.StatusCode} and content:\n%s{response.Content.ReadAsStringAsync().Result}" 

for counter = 1 to Int32.MaxValue do
    sw.Restart()
    let cam = MMALCamera.Instance
    use imgCaptureHandler = new InMemoryCaptureHandler()

    imgCaptureHandler
    |> cam.TakeRawPicture
    |> Async.AwaitTask
    |> Async.RunSynchronously

    notify counter

    1. |> TimeSpan.FromSeconds |> Thread.Sleep