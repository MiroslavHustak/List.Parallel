[<RequireQualifiedAccess>]
module List.Parallel

open System
open System.Threading
open FSharp.Control

//Examples
let private myIdeaOfASmallList = 100
let private myIdeaOfALargelList = 200

let private maxDegreeOfParallelismThrottled = 12
let private maxDegreeOfParallelismMedium = 12
let private maxDegreeOfParallelism = 12

let private maxDegreeOfParallelismExample : int -> int = 

    let (|Small|Medium|Large|) length = 
        match length with
        | length 
            when length < myIdeaOfASmallList 
            -> Small
        | length
            when length >= myIdeaOfASmallList && length <= myIdeaOfALargelList 
            -> Medium
        | _ -> Large
    
    function
        | Small  -> maxDegreeOfParallelismThrottled
        | Medium -> maxDegreeOfParallelismMedium
        | Large  -> maxDegreeOfParallelism

// Legend:
//
// Parallelism mechanism:
//   PT    -> Task Parallel Library (TPL)-based. Underlying mechanism: TPL's Parallel.For/ForEach (data-parallel partitioner)
//   AW    -> Async Workflow-based. Underlying mechanism: F# Async workflows composed via Async.Parallel
//
// Workload / behavior modifiers:
//   CPU   -> CPU-bound operations
//   IO    -> I/O-bound operations
//   Token -> variant with built-in cancellation support (via CancellationToken)
//   Async -> asynchronous variant

// Any combination is missing? Let me know.

// ******************** ITER ***********************

let iter_CPU_PT (action : 'a -> unit) (list : 'a list) : Result<unit, string> =

    match list with
    | [] -> Ok ()
    | _  ->
        try
            list
            |> List.toArray
            |> Array.Parallel.iter action  
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter_CPU_AW (action : 'a -> unit) (list : 'a list) : Result<unit, string> =

    match list with
    | [] -> Ok ()
    | _  ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))   

            list
            |> Array.ofList
            |> Array.map (fun x -> async { return action x })  
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> Async.Ignore<unit array> 
            |> Async.RunSynchronously
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter_CPU_AW_Token (token : CancellationToken) (action : 'a -> unit) (list : 'a list) : Result<unit, string> =
        
    match list with
    | [] -> Ok ()
    | _  ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))
        
            list
            |> Array.ofList
            |> Array.map
                (fun x 
                    ->
                    async
                        {
                            token.ThrowIfCancellationRequested ()
                            return action x
                        }
                )
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> Async.Ignore<unit array> 
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter_CPU_AW_Async (action : 'a -> unit) (list : 'a list) : Async<Result<unit, string>> =

    match list with
    | [] -> 
        async { return Ok () }
    | _  ->
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))  

                    let! _ = 
                        list
                        |> Array.ofList
                        |> Array.map (fun x -> async { return action x })  
                        |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                        |> Async.Ignore<unit array> 

                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            }        

let iter_CPU_AW_Token_Async (token : CancellationToken) (action : 'a -> unit) (list : 'a list) : Async<Result<unit, string>> =

    match list with
    | [] ->
        async { return Ok () }
    | _  ->
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

                    let tasks =
                        list
                        |> Array.ofList
                        |> Array.map
                            (fun x 
                                ->
                                async
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        do! Async.SwitchToThreadPool()
                                        return action x
                                    }
                            )

                    let! _ = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            }        

let iter_IO_AW maxDegreeOfParallelism (action : 'a -> unit) (list : 'a list) : Result<unit, string> =
 
    match list with
    | [] -> Ok ()
    | _  ->
        try
            let maxDegreeOfParallelismAdapted = List.length >> maxDegreeOfParallelism <| list
            
            list
            |> Array.ofList
            |> Array.map (fun item -> async { return action item })  
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> Async.Ignore<unit array> 
            |> Async.RunSynchronously 
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter_IO_AW_Token maxDegreeOfParallelism (token : CancellationToken) (action : 'a -> unit) (list : 'a list) : Result<unit, string> =

    match list with
    | [] -> Ok ()
    | _  ->
        try
            let maxDegreeOfParallelismAdapted =
                List.length >> maxDegreeOfParallelism <| list

            list
            |> Array.ofList
            |> Array.map
                (fun item 
                    ->
                    async
                        {
                            token.ThrowIfCancellationRequested ()
                            return action item
                        }
                )
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> Async.Ignore<unit array> 
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter_IO_AW_Async maxDegreeOfParallelism (action : 'a -> unit) (list : 'a list) : Async<Result<unit, string>> =
 
    match list with
    | [] 
        -> 
        async { return Ok () }
    | _ 
        ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = List.length >> maxDegreeOfParallelism <| list
            
                    let! _ =
                        list
                        |> Array.ofList
                        |> Array.map (fun item -> async { return action item })  
                        |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                        |> Async.Ignore<unit array> 

                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            } 

let iter_IO_AW_Token_Async maxDegreeOfParallelism (token : CancellationToken) (action : 'a -> unit) (list : 'a list) : Async<Result<unit, string>> =

    match list with
    | [] ->
        async { return Ok () }
    | _  ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted =
                        List.length >> maxDegreeOfParallelism <| list

                    let tasks =
                        list
                        |> Array.ofList
                        |> Array.map
                            (fun item
                                ->
                                async
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        do! Async.SwitchToThreadPool()
                                        return action item
                                    }
                            )

                    let! _ = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            }

// ******************** ITER2 ***********************

let iter2_CPU_PT<'a,'b> (mapping : 'a -> 'b -> unit) (xs1 : 'a list) (xs2 : 'b list) : Result<unit, string> =

    match xs1, xs2 with
    | [], _ | _, [] 
        -> 
        Ok ()
    | _ when List.length xs1 <> List.length xs2
        -> 
        Ok ()
    | _ ->
        try
            List.zip xs1 xs2
            |> List.toArray
            |> Array.Parallel.iter (fun (x, y) -> mapping x y)
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter2_CPU_AW<'a,'b> (mapping : 'a -> 'b -> unit) (xs1 : 'a list) (xs2 : 'b list) : Result<unit, string> =

    match xs1, xs2 with
    | [], _ | _, [] 
        -> 
        Ok ()
    | _ when List.length xs1 <> List.length xs2
        -> 
        Ok ()
    | _ ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map (fun (x, y) -> async { return mapping x y })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> Async.Ignore<unit array> 
            |> Async.RunSynchronously
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter2_IO_AW<'a,'b> maxDegreeOfParallelism (mapping : 'a -> 'b -> unit) (xs1 : 'a list) (xs2 : 'b list) : Result<unit, string> =      
    
    let xs1Length = List.length xs1
    let xs2Length = List.length xs2
    
    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        Ok ()
    | false
        ->
        try
            let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length
        
            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map (fun (x, y) -> async { return mapping x y })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> Async.Ignore<unit array> 
            |> Async.RunSynchronously
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter2_IO_AW_Token<'a,'b,'c> maxDegreeOfParallelism (mapping : 'a -> 'b -> Async<'c>) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Result<unit, string> =

    let xs1Length = List.length xs1
    let xs2Length = List.length xs2

    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        Ok ()
    | false
        ->
        try
            let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length        
                    
            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map
                (fun (x, y) 
                    ->
                    async 
                        {
                            token.ThrowIfCancellationRequested ()
                            return! mapping x y
                        }
                )        
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted) |> Async.Ignore<'c array>
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            Ok ()
        with
        | ex -> Error (string ex.Message)

let iter2_IO_AW_Async<'a,'b> maxDegreeOfParallelism (mapping : 'a -> 'b -> unit) (xs1 : 'a list) (xs2 : 'b list) : Async<Result<unit, string>> =      
    
    let xs1Length = List.length xs1
    let xs2Length = List.length xs2
    
    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        async { return Ok () }
    | false
        ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length
        
                    let! _ =
                        List.zip xs1 xs2
                        |> Array.ofList
                        |> Array.map (fun (x, y) -> async { return mapping x y })
                        |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                        |> Async.Ignore<unit array> 

                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            } 

let iter2_IO_AW_Token_Async<'a,'b,'c> maxDegreeOfParallelism (mapping : 'a -> 'b -> Async<'c>) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Async<Result<unit, string>> =

    let xs1Length = List.length xs1
    let xs2Length = List.length xs2

    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        async { return Ok () }
    | false
        ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length        
           
                    let tasks =  
                        List.zip xs1 xs2
                        |> Array.ofList
                        |> Array.map
                            (fun (x, y) 
                                ->
                                async 
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        return! mapping x y
                                    }
                            )        
                    let! _ = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                    return Ok ()
                with
                | ex -> return Error (string ex.Message)
            }     

// ******************** MAP ***********************

let map_CPU_PT (action : 'a -> 'b) (list : 'a list) : Result<'b list, string> =

    match list with
    | []
        -> 
        Ok []
    | _ ->
        try
            list
            |> List.toArray
            |> Array.Parallel.map action  
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map_CPU_AW (action : 'a -> 'b) (list : 'a list) : Result<'b list, string> =

    match list with
    | [] -> Ok []
    | _  ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

            list
            |> Array.ofList
            |> Array.map (fun x -> async { return action x })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> Async.RunSynchronously
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map_CPU_AW_Async (action : 'a -> Async<'b>) (list : 'a list) : Async<Result<'b list, string>> =

    match list with
    | [] ->
        async { return Ok [] }
    | _  ->
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

                    let tasks =
                        list
                        |> Array.ofList
                        |> Array.map (fun x -> async { return! action x })

                    let! result = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                    return result |> Array.toList |> Ok
                with
                | ex -> return Error (string ex.Message)
            }

let map_CPU_AW_Token (token : CancellationToken) (action : 'a -> 'b) (list : 'a list) : Result<'b list, string> =

    match list with
    | [] -> Ok []
    | _  ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

            list
            |> Array.ofList
            |> Array.map
                (fun x 
                    ->
                    async 
                        {
                            token.ThrowIfCancellationRequested ()
                            do! Async.SwitchToThreadPool()
                            return action x
                        }
                )  
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map_CPU_AW_Token_Async (token : CancellationToken) (action : 'a -> Async<'b>) (list : 'a list) : Async<Result<'b list, string>> =

    match list with
    | [] ->  
        async { return Ok [] }
    | _  ->
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))      

                    let tasks = 
                        list
                        |> Array.ofList   
                        |> Array.map
                            (fun item 
                                ->
                                async 
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        do! Async.SwitchToThreadPool()
                                        return! action item
                                    }
                            )   

                    let! result =  Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                    return result |> List.ofArray |> Ok
                with
                | ex -> return Error (string ex.Message)
            } 

let map_IO_AW maxDegreeOfParallelism (action : 'a -> 'b) (list : 'a list) : Result<'b list, string> =

    match list with
    | [] -> Ok []
    | _  ->
        try
            let maxDegreeOfParallelismAdapted = List.length >> maxDegreeOfParallelism <| list  
         
            list
            |> Array.ofList
            |> Array.map (fun item -> async { return action item })  
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> Async.RunSynchronously  
            |> List.ofArray
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map_IO_AW_Async maxDegreeOfParallelism (action : 'a -> Async<'b>) (list : 'a list) : Async<Result<'b list, string>> =
         
    match list with
    | [] -> 
        async { return Ok [] }
    | _  ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = List.length >> maxDegreeOfParallelism <| list  
                  
                    let tasks = 
                        list
                        |> Array.ofList       
                        |> Array.map (fun item -> async { return! action item })  
            
                    let! result = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                    return result |> List.ofArray |> Ok
                with
                | ex -> return Error (string ex.Message)
            }
            
let map_IO_AW_Token maxDegreeOfParallelism (token : CancellationToken) (action : 'a -> 'b) (list : 'a list) : Result<'b list, string> =

    match list with
    | [] -> Ok []
    | _  ->
        try
            let maxDegreeOfParallelismAdapted =
                List.length >> maxDegreeOfParallelism <| list

            list
            |> Array.ofList
            |> Array.map
                (fun item 
                    ->
                    async
                        {
                            token.ThrowIfCancellationRequested ()
                            return action item
                        }
                )
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)
            
let map_IO_AW_Token_Async maxDegreeOfParallelism (token : CancellationToken) (action : 'a -> Async<'b>) (list : 'a list) : Async<Result<'b list, string>> =
     
    match list with
    | [] -> 
        async { return Ok [] }
    | _  ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = List.length >> maxDegreeOfParallelism <| list  
              
                    let tasks = 
                        list
                        |> Array.ofList   
                        |> Array.map
                            (fun item 
                                ->
                                async 
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        return! action item
                                    }
                            )        
        
                    let! result = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                    return result |> List.ofArray |> Ok
                with
                | ex -> return Error (string ex.Message)
            }   

// ******************** MAP2 ***********************

let map2_CPU_PT<'a,'b,'c> (mapping : 'a -> 'b -> 'c) (xs1 : 'a list) (xs2 : 'b list) : Result<'c list, string> =

    let xs1Length = List.length xs1
    let xs2Length = List.length xs2

    match xs1, xs2 with
    | [], _ | _, [] 
        ->
        Ok []
    | _ when xs1Length <> xs2Length
        ->
        Ok []
    | _ ->
        try
            List.zip xs1 xs2
            |> List.toArray
            |> Array.Parallel.map (fun (x, y) -> mapping x y)
            |> Array.toList        
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map2_CPU2_AW<'a,'b,'c> (mapping : 'a -> 'b -> 'c) (xs1 : 'a list) (xs2 : 'b list) : Result<'c list, string> =

    match xs1, xs2 with
    | [], _ | _, [] 
        ->
        Ok []
    | _ when List.length xs1 <> List.length xs2
        ->
        Ok []
    | _ ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))  

            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map (fun (x, y) -> async { return mapping x y })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> Async.RunSynchronously
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map2_CPU2_AW_Token<'a, 'b, 'c> (mapping : 'a -> 'b -> 'c) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Result<'c list, string> =

    match xs1, xs2 with
    | [], _ | _, [] 
        ->
        Ok []
    | _ when List.length xs1 <> List.length xs2
        ->
        Ok []
    | _ ->
        try
            let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))  

            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map
                (fun (x, y) 
                    ->
                    async 
                        {
                            token.ThrowIfCancellationRequested ()
                            return mapping x y
                        }
                )        
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map2_CPU2_AW_Async<'a,'b,'c> (mapping : 'a -> 'b -> 'c) (xs1 : 'a list) (xs2 : 'b list) : Async<Result<'c list, string>> =

    match xs1, xs2 with
    | [], _ | _, [] 
        ->
        async { return Ok [] }
    | _ when List.length xs1 <> List.length xs2
        ->
        async { return Ok [] }
    | _ ->
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))  

                    let tasks = 
                        List.zip xs1 xs2
                        |> Array.ofList
                        |> Array.map (fun (x, y) -> async { return mapping x y })

                    let! results = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                    return results |> Array.toList |> Ok
                with
                | ex -> return Error (string ex.Message)
            }    

let map2_CPU2_AW_Token_Async<'a,'b,'c> (mapping : 'a -> 'b -> Async<'c>) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Async<Result<'c list, string>> =

    match xs1, xs2 with
    | [], _ | _, [] 
        ->
        async { return Ok [] }

    | _ when List.length xs1 <> List.length xs2
        ->
        async { return Ok [] }

    | _ ->        
        async
            {
                try
                    let maxDegree = System.Math.Max(1, (Environment.ProcessorCount - 1))

                    let tasks = 
                        List.zip xs1 xs2
                        |> Array.ofList
                        |> Array.map
                            (fun (x, y) 
                                ->
                                async 
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        return! mapping x y
                                    }
                            )  
                    let! results = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegree)
                    return results |> Array.toList |> Ok
                with
                | ex -> return Error (string ex.Message)
            }    

let map2_IO_AW<'a,'b,'c> maxDegreeOfParallelism (mapping : 'a -> 'b -> 'c) (xs1 : 'a list) (xs2 : 'b list) : Result<'c list, string> =
    
    let xs1Length = List.length xs1
    let xs2Length = List.length xs2
    
    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        Ok []
    | false
        ->
        try
            let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length
        
            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map (fun (x, y) -> async { return mapping x y })
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
            |> Async.RunSynchronously
            |> Array.toList
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map2_IO_AW_Token<'a,'b,'c> maxDegreeOfParallelism (mapping : 'a -> 'b -> 'c) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Result<'c list, string> =
    
    let xs1Length = List.length xs1
    let xs2Length = List.length xs2

    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        Ok []
    | false
        ->
        try
            let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length        
        
            List.zip xs1 xs2
            |> Array.ofList
            |> Array.map
                (fun (x, y) 
                    ->
                    async 
                        {
                            token.ThrowIfCancellationRequested ()
                            return mapping x y
                        }
                )        
            |> fun tasks -> Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)           
            |> fun a -> Async.RunSynchronously(a, cancellationToken = token)
            |> List.ofArray     
            |> Ok
        with
        | ex -> Error (string ex.Message)

let map2_IO_AW_Token_Async<'a,'b,'c> maxDegreeOfParallelism (mapping : 'a -> 'b -> Async<'c>) (token : CancellationToken) (xs1 : 'a list) (xs2 : 'b list) : Async<Result<'c list, string>> =

    let xs1Length = List.length xs1
    let xs2Length = List.length xs2

    match (xs1Length = 0 || xs2.IsEmpty) || xs1Length <> xs2Length with
    | true 
        ->
        async { return Ok [] }
    | false
        ->
        async
            {
                try
                    let maxDegreeOfParallelismAdapted = maxDegreeOfParallelism xs1Length        
           
                    let tasks =  
                        List.zip xs1 xs2
                        |> Array.ofList
                        |> Array.map
                            (fun (x, y) 
                                ->
                                async 
                                    {
                                        token.ThrowIfCancellationRequested ()
                                        do! Async.SwitchToThreadPool()
                                        return! mapping x y
                                    }
                            )        
                    let! results = Async.Parallel(tasks, maxDegreeOfParallelism = maxDegreeOfParallelismAdapted)
                    return results |> Array.toList |> Ok
                with
                | ex -> return Error (string ex.Message)
            }  