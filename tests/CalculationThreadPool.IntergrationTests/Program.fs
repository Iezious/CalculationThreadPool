open System
open System.Diagnostics
open CalculationThreadPool.IntegrationTests

async {
    Console.WriteLine "Start"
    let sw = Stopwatch()
    sw.Start()
//    let! aw, ae = NetPoolCalc.run 10000 100000 1_000_000
    let! aw, ae = CustomPoolCalc.run 10000 100000 1_000_000
    sw.Stop()
    Console.WriteLine $"avg wait: {aw}, awg exec: {ae}, total run {sw.ElapsedMilliseconds}"
} |> Async.RunSynchronously