namespace CalculationThreadPool.IntegrationTests

open System
open System.Threading

module CustomPoolCalc =
    
    let calcPool = Libraries.CalculationThreadPool.ThreadPool<int32, int32> 50
    
    let private actor() =
        
        let mailbox = MailboxProcessor<CalculationTaskMessage>.Start(fun inbox ->
        
            let rec loop() = async {
                match! inbox.Receive() with
                | CalculationTaskMessage.DoWork (start, value, ch) ->
                    let inWait = (DateTime.UtcNow - start).Ticks / 10000L
                    let! res = calcPool.Execute(value, PrimeCalculations.nextPrime, 10000)
                    let total = (DateTime.UtcNow - start).Ticks / 10000L 
                    match res with
                    | Ok v -> ch.Reply(v, inWait, total)
                    | _ -> ch.Reply(0, inWait, total)
                | Die ->
                    return ()
                
                return! loop()
            }
            
            loop()
        )
        
        {|
           Calc = fun v -> mailbox.PostAndAsyncReply(fun ch -> DoWork(DateTime.UtcNow, v, ch))
           Die = fun () -> mailbox.Post(Die)
        |}
        
        
   
    
    let run actorsCount callsCount = async {
        
        let actors = [| for i in 1..actorsCount -> actor() |]
        let ra = Random(DateTime.Now.Ticks |> int32)
        
        let mutable totalExecutionTime = 0L
        let mutable totalWaitingTime = 0L
        
        let step() = async {
            let actor = actors[ra.Next(actorsCount)]
            let! res, wait, total = actor.Calc(10000)
            Interlocked.Add(&totalWaitingTime, wait) |> ignore
            Interlocked.Add(&totalExecutionTime, total) |> ignore
            return res
        }

        let! _ =
            seq { for _ in 1..callsCount -> step() }
            |> fun (s) -> Async.Parallel(s, 500)
            
        calcPool.Stop()            
                    
        return (double totalWaitingTime/ double callsCount), ( double totalExecutionTime / double callsCount) 
    }

