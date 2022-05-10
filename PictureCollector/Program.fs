open System
open System.Threading

PiCamAgent.piCamAgent.Error.Add(fun ex -> printfn $"Unhandled exception in mailboxprocessor:\n%A{ex}")

for counter = 1 to Int32.MaxValue do
    PiCamAgent.piCamAgent.Post counter
    1. |> TimeSpan.FromMinutes |> Thread.Sleep