namespace Libraries.CalculationThreadPool

open System
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks


type RoundRobingThreadPool<'treq, 'tres>(threadsCount: int) =

    let stopper = new CancellationTokenSource()
    let random  = Random()
    
    let innerWorker(i) =
        
        let waitForMessage = new AutoResetEvent(false)
        let queue = Queue<ThreadPoolMailboxMessage<'treq, 'tres>>()
        
        let threadRun() =
            
            while not stopper.IsCancellationRequested do
                let haveWork, work = lock queue (fun _ -> queue.TryDequeue())
                if haveWork then
                    try
                        let res = work.Calculator(work.Request)
                        work.ResultAwait.SetResult(Ok res)
                    with e ->
                        work.ResultAwait.SetResult(Error e)
                else
                    waitForMessage.WaitOne(10) |> ignore
                    
        let thread =  Thread(threadRun, Name = $"CalculationThread_{i}")
        thread.UnsafeStart()
        
        fun work ->
            lock queue (fun () -> queue.Enqueue(work))
            waitForMessage.Set() |> ignore
        
    let pool = seq { for i in  0..threadsCount-1 -> innerWorker(i)}
               |> Seq.toArray

    member _.Execute(request, calculator, timeout) =
        let await = TaskCompletionSource<Result<'tres, exn>>()
        
        {Request = request; Calculator = calculator; ResultAwait = await }
        |> pool[random.Next(pool.Length)]
        
        await.Task

    member _.Stop() =
        stopper.Cancel()
        
