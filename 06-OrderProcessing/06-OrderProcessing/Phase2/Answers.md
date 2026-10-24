Q1. What is the difference between starting tasks and awaiting them one by one?**

Starting tasks before awaiting them allows independent operations to run concurrently
reducing execution time. Awaiting each operation before starting the next makes them run sequentially and takes longer.

Example:
Concurrent: 1002 ms. 
Sequential: 2013 ms.

Q2. Does every Task create a new thread? Does Task.Delay() occupy a thread while waiting?

Answer: No. A Task does not necessarily create a new thread. 
           Task.Delay() does not block a thread while waiting.

Q3. What happens if we use .Result or .Wait() instead of await?

Answer: .Result and .Wait() block the current thread, while await allows it to do other work while waiting.

Q4. What happens if an exception occurs inside a Task that is never awaited?

Answer: The exception is stored in the Task and may go unnoticed if the Task's failure is never observed.

Q5. When should we use Task.Run() and when should we avoid it?

Answer: Use Task.Run() for CPU-bound work. Avoid wrapping naturally asynchronous I/O operations in Task.Run().

Q6. What is the difference between Task and Task<T>?

Answer: Task represents an asynchronous operation without a result, while Task<T> represents an operation that returns a value of type T.

Q7. Why can't we use await inside a lock block?

Answer: C# does not allow await inside lock. For asynchronous synchronization, we can use SemaphoreSlim with WaitAsync().

Q8. Why did the stock reservation cause a race condition, and how was it fixed?

Answer: Both operations read the same stock before either updated it. The issue was fixed by protecting the stock check and update so only one operation can modify the stock at a time.