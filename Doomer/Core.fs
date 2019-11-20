module Core

open System.Diagnostics

type Measure() =
    let samples = 50
    let mutable measurements = Array.create samples 0.0
    let mutable index = 0
    let mutable stopwatch = new Stopwatch()

    member this.Start() = stopwatch.Reset() ; stopwatch.Start()
    member this.Reset() = stopwatch.Reset()
    member this.StartNoReset() = stopwatch.Start()
    member this.StopNoRecord() = stopwatch.Stop()
    member this.Record() = stopwatch.Stop()
    member this.Stop() =
        stopwatch.Stop()
        index <- (index + 1) % samples
        measurements.[index] <- stopwatch.Elapsed.TotalMilliseconds

    member this.GetAverage() =
        Array.averageBy double measurements

    member this.SortAndPrint() =
        Array.sort measurements
        |> printfn "%A"
