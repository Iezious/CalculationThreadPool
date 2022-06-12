namespace Libraries.CalculationThreadPool

open System.Threading.Tasks

type private ThreadPoolMailboxMessage<'treq, 'tres> =
    {
        Request: 'treq
        Calculator: 'treq -> 'tres
        ResultAwait:  TaskCompletionSource<Result<'tres, exn>> 
    }