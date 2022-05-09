module PiCamAgent

open System.Diagnostics
open System
open Azure.Storage.Blobs
open MMALSharp.Handlers
open MMALSharp

let private sw = Stopwatch()
let private connectionStringEnvironmentVariableName = "AZURE_STORAGE_CONNECTION_STRING"
let private containerName = "pi-cam-pictures"

let private connectionString = 
    if String.IsNullOrEmpty <| Environment.GetEnvironmentVariable connectionStringEnvironmentVariableName then
        failwith $"Missing environment variable: {connectionStringEnvironmentVariableName}"
    else 
        Environment.GetEnvironmentVariable connectionStringEnvironmentVariableName

let private blobServiceClient = BlobServiceClient connectionString
let private blobContainerClientResponse = blobServiceClient.CreateBlobContainer containerName
let private blobContainerClient = blobContainerClientResponse.Value

let upload (imgBytes:ResizeArray<byte>) counter = 
    let msg = sprintf $"Captured image #%i{counter} in %i{sw.ElapsedMilliseconds} ms" 
    printfn "%s" msg

    let n = DateTime.UtcNow
    let filename = $"{n.Year}-{n.Month}-{n.Day} {n.Hour}:{n.Minute}.jpg"
    let blobClient = blobContainerClient.GetBlobClient filename
    let binaryData = BinaryData imgBytes
    let result = blobClient.Upload binaryData
    let rawResponse = result.GetRawResponse()
    if rawResponse.IsError then
        ()
    else
        ()

let private captureAndTransmitPicture (counter: int) =
    sw.Restart()
    use imgCaptureHandler = new InMemoryCaptureHandler()    

    imgCaptureHandler
    |> MMALCamera.Instance.TakeRawPicture
    |> Async.AwaitTask
    |> Async.RunSynchronously

    upload imgCaptureHandler.WorkingData counter


let piCamAgent = MailboxProcessor.Start(fun inbox->
    let rec messageLoop() = async {
        let! counter = inbox.Receive()
        captureAndTransmitPicture counter
        return! messageLoop()
        }

    // start the loop
    messageLoop()
    )