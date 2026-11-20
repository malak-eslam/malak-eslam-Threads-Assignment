Phase 4 — Answers
Q1. For each of Tasks 10.1–10.4, which tool did you use, and why can't "start, then await one by one" meet the requirement?
Task 10.1 — Task.WhenEach: It reports tasks as they complete, allowing orders to be displayed in completion order instead of start order.
Task 10.2 — Task.WhenAll: It allows the four dashboard loads to run concurrently and waits for all of them to finish.
Task 10.3 — Task.WhenAny: It allows the service to select the first successful shipping quote and continue waiting if a carrier fails.
Task 10.4 A — Parallel.ForEachAsync: It distributes CPU-bound fraud-scoring work across available processor cores while respecting the concurrency limit.
Task 10.4 B — Parallel.ForEachAsync: It processes email sends concurrently while limiting the maximum degree of parallelism to four.

Awaiting tasks one by one would delay reporting completed orders, increase the dashboard's total waiting time, delay selecting the fastest shipping quote, and prevent the fraud-scoring and email tasks from using the required concurrency effectively.

Q2. In 10.2, when several loads fail, how do you get every exception? What does a plain await give you?

After Task.WhenAll throws, I inspect each faulted task and collect its exceptions using task.Exception.Flatten().InnerExceptions. A plain await propagates one exception to the caller, while the combined task's Exception can contain multiple failures.

Q3. In 10.3, what happens to the losing carriers if you never cancel them?

The losing carriers continue running even though their quotes are no longer needed. This wastes resources and allows unnecessary work to continue. Cancelling them with a CancellationToken allows them to stop when they observe the cancellation request.

Q4. Why does 10.4 A get faster with more CPU cores, while 10.4 B does not depend on the number of cores?

Fraud scoring is CPU-bound, so multiple CPU cores can calculate scores in parallel. Email sending is I/O-bound, so most of the time is spent waiting for asynchronous operations rather than using the CPU. Therefore, email performance depends more on I/O latency and concurrency limits than on the number of CPU cores.

Q5. What is the difference between parallel work (10.4 A) and concurrent asynchronous work (10.1)?

Parallel work executes multiple computations simultaneously on different CPU cores. Concurrent asynchronous work allows multiple operations to make progress during overlapping periods without requiring a dedicated thread for each operation. Task.WhenEach reports tasks as they finish; it does not itself execute them in parallel.

