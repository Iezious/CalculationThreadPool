namespace Libraries.CalculationThreadPool

open System.Collections.Generic
open System.Threading

type internal ThreadPoolMailboxMessage<'treq, 'tres> =
    {
        Request: 'treq
        Calculator: 'treq -> 'tres
        ResultAwait:  AsyncReplyChannel<Result<'tres, exn>> 
    }

type ThreadPool<'treq, 'tres>(threadsCount: int) =
    
    let waitForMessage = new AutoResetEvent(false)
    let queue = Queue<ThreadPoolMailboxMessage<'treq, 'tres>>()
    
    let stopper = new CancellationTokenSource()
    
    let mailBox = MailboxProcessor<ThreadPoolMailboxMessage<'treq, 'tres>>.Start(fun inbox -> 
        
        let rec loop() = async {
            let! msg = inbox.Receive()
            lock queue (fun _ -> queue.Enqueue(msg))
            waitForMessage.Set() |> ignore
            
            return! loop()
        }
        
        loop()
    , stopper.Token)
    
    let threadRun() =
        
        while not stopper.IsCancellationRequested do
            let haveWork, work = lock queue (fun _ -> queue.TryDequeue())
            if haveWork then
                try
                    let res = work.Calculator(work.Request)
                    work.ResultAwait.Reply(Ok res)
                with e -> work.ResultAwait.Reply(Error e)
            else
//                Thread.Sleep(1)
                waitForMessage.WaitOne(100) |> ignore
            
    let _pool = seq { for i in  0..threadsCount-1 -> Thread(threadRun, Name = $"CalculationThread_{i}") }
               |> Seq.toArray
               |> Array.iter (fun th -> th.UnsafeStart())

    member _.Execute(request, calculator, timeout) =
        mailBox.PostAndAsyncReply(fun ch -> { Request = request; Calculator = calculator; ResultAwait = ch  })
        
    member _.Stop() =
        stopper.Cancel()
        
