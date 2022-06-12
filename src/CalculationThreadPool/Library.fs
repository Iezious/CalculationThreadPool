namespace Libraries.CalculationThreadPool

open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

type internal ThreadPoolMailboxMessage<'treq, 'tres> =
    {
        Request: 'treq
        Calculator: 'treq -> 'tres
        ResultAwait:  TaskCompletionSource<Result<'tres, exn>> 
//        ResultAwait:  AsyncReplyChannel<Result<'tres, exn>> 
    }

type ThreadPool<'treq, 'tres>(threadsCount: int) =
    
    let waitForMessage = new AutoResetEvent(false)
    let queue = Queue<ThreadPoolMailboxMessage<'treq, 'tres>>()
    
    let stopper = new CancellationTokenSource()
    
//    let mailBox = MailboxProcessor<ThreadPoolMailboxMessage<'treq, 'tres>>.Start(fun inbox -> 
//        
//        let rec loop() = async {
//            let! msg = inbox.Receive()
//            lock queue (fun _ -> queue.Enqueue(msg))
//            waitForMessage.Set() |> ignore
//            
//            return! loop()
//        }
//        
//        loop()
//    , stopper.Token)
    
    let threadRun() =
        
        while not stopper.IsCancellationRequested do
            let haveWork, work = lock queue (fun _ -> queue.TryDequeue())
            if haveWork then
                try
                    let res = work.Calculator(work.Request)
//                    work.ResultAwait.Reply(Ok res)
                    work.ResultAwait.SetResult(Ok res)
                with e ->
//                    work.ResultAwait.Reply(Error e)
                    work.ResultAwait.SetResult(Error e)
            else
                waitForMessage.WaitOne(10) |> ignore
            
    let _pool = seq { for i in  0..threadsCount-1 -> Thread(threadRun, Name = $"CalculationThread_{i}") }
               |> Seq.toArray
               |> Array.iter (fun th -> th.UnsafeStart())

    member _.Execute(request, calculator, timeout) =
        let await = TaskCompletionSource<Result<'tres, exn>>()
        let work = {Request = request; Calculator = calculator; ResultAwait = await }
        lock queue (fun () -> queue.Enqueue(work))
        waitForMessage.Set() |> ignore
        await.Task

//    member _.Execute(request, calculator, timeout) =
//        mailBox.PostAndAsyncReply(fun ch -> { Request = request; Calculator = calculator; ResultAwait = ch })
        
    member _.Stop() =
        stopper.Cancel()
        
