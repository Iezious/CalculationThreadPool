namespace CalculationThreadPool.IntegrationTests
open System

type CalculationTaskMessage =
    | DoWork of  Start : DateTime * Value: int * ch : AsyncReplyChannel<int32 * int64 * int64>
    | Die