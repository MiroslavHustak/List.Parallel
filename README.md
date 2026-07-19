See library.fs - the code will tell you all you need to use this library.

Legend:

Parallelism mechanism:

PT    -> Task Parallel Library (TPL)-based. Underlying mechanism: TPL's Parallel.For/ForEach (data-parallel partitioner)

AW    -> Async Workflow-based. Underlying mechanism: F# Async workflows composed via Async.Parallel


Workload / behavior modifiers:

CPU   -> CPU-bound operations

IO    -> I/O-bound operations

Token -> variant with built-in cancellation support (via CancellationToken)

Async -> asynchronous variant
