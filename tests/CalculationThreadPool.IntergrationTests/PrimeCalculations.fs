namespace CalculationThreadPool.IntegrationTests

module PrimeCalculations =
    
    let isPrime n =
        let sqrt' = (float >> sqrt >> int) n // square root of integer
        [ 2 .. sqrt' ] // all numbers from 2 to sqrt'
        |> List.forall (fun x -> n % x <> 0) // no divisors

    let nextPrime n =
         let mutable c = n
         while not <| isPrime c do c <- c+1
         c
