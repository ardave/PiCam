open System
open System.Threading

for counter = 1 to Int32.MaxValue do
    PiCamAgent.piCamAgent.Post counter
    10. |> TimeSpan.FromSeconds |> Thread.Sleep