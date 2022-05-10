module PiCamAgent

open System.Diagnostics
open System
open Azure.Storage.Blobs
open MMALSharp.Handlers
open MMALSharp
open System.IO
open MMALSharp.Common

let private connectionStringEnvironmentVariableName = "AZURE_STORAGE_CONNECTION_STRING"
let private containerName = "pi-cam-pictures"

let private connectionString = 
    if String.IsNullOrEmpty <| Environment.GetEnvironmentVariable connectionStringEnvironmentVariableName then
        failwith $"Missing environment variable: {connectionStringEnvironmentVariableName}"
    else 
        Environment.GetEnvironmentVariable connectionStringEnvironmentVariableName

let private blobServiceClient = BlobServiceClient connectionString
let private blobContainerClient = blobServiceClient.GetBlobContainerClient containerName

let upload (memoryStream: MemoryStream) counter = 
    let sw = Stopwatch.StartNew()

    let n = DateTime.UtcNow
    let fileName = $"{n.Year}-{n.Month}-{n.Day} {n.Hour}:{n.Minute}:{n.Second}.jpg"
    let blobClient = blobContainerClient.GetBlobClient fileName
    let binaryData = BinaryData(memoryStream.ToArray())
    let result = blobClient.Upload binaryData
    let rawResponse = result.GetRawResponse()
    if rawResponse.IsError then
        printfn $"Image Upload %s{fileName} failed in %i{sw.ElapsedMilliseconds} ms."
    else
        printfn $"Uploaded image %s{fileName} to Blob Storage in %i{sw.ElapsedMilliseconds} ms."

let private captureAndTransmitPicture (counter: int) =
    let sw = Stopwatch.StartNew()
    use imgCaptureHandler = new MemoryStreamCaptureHandler()    

    MMALCamera.Instance.TakePicture(imgCaptureHandler, MMALEncoding.JPEG, MMALEncoding.I420)
    |> Async.AwaitTask
    |> Async.RunSynchronously
    
    printfn $"Captured image #%i{counter} in %i{sw.ElapsedMilliseconds} ms"
    
    upload imgCaptureHandler.CurrentStream counter


let piCamAgent = MailboxProcessor.Start(fun inbox->
    let rec messageLoop() = async {
        let! counter = inbox.Receive()
        captureAndTransmitPicture counter
        return! messageLoop()
        }

    // start the loop
    messageLoop()
    )