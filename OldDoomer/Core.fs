module Core

open System.Collections.Generic
open System.Diagnostics

type Measure() =
    let samples = 50
    let mutable measurements = Array.create samples 0.0
    let mutable index = 0
    let mutable stopwatch = Stopwatch()

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
        
type FrameCounter() =
    let mutable totalFrames = 0s
    let mutable totalSeconds = 0.0f
    let mutable currentFramesPerSecond = 0.0f

    let maximumSamples = 100

    let sampleBuffer : Queue<single> = Queue<single>()

    member val AverageFramesPerSecond = 0f with get, set
    
    member this.Update (deltaTime: single) =
        currentFramesPerSecond <- 1.0f / deltaTime;

        sampleBuffer.Enqueue(currentFramesPerSecond)

        if sampleBuffer.Count > maximumSamples then
            sampleBuffer.Dequeue() |> ignore
            this.AverageFramesPerSecond <- Seq.average sampleBuffer
        else
            this.AverageFramesPerSecond <- currentFramesPerSecond;

        totalFrames <- totalFrames + 1s
        totalSeconds <- totalSeconds + deltaTime
