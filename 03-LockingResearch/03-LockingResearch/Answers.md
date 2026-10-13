1. What is System.Threading.Monitor used for?

Monitor is a class in System.Threading used to synchronize access to shared resources between threads
It allows only one thread at a time to enter a critical section protected by the same lock object.
It provides methods such as Enter, Exit, TryEnter, Wait, and Pulse.

-------------------
2. What does the C# lock statement compile to?

When lock is used with an object the compiler generates code that uses Monitor
Enter and Monitor.Exit inside a try-finally structure.

-------------------
3. List at least two differences between lock and calling Monitor APIs directly.

lock is easier to write and use.
Monitor provides more control through methods like TryEnter, Wait, and Pulse.
With Monitor.Enter and Monitor.Exit, we need try finally to ensure the lock is released

-------------------
4. When might a developer use Monitor.TryEnter, Monitor.Pulse, or Monitor.Wait instead of a plain lock?

TryEnter: To try to acquire a lock without waiting indefinitely.
Wait: To release the lock and wait for a notification.
Pulse: To notify a waiting thread that it can try to continue.

-------------------
5. Write the same critical section twice: once with lock, once with Monitor.Enter / Monitor.Exit.
1. 
Using lock
lock (_lock)
{
    balance += 100;
}
Using Monitor.Enter and Monitor.Exit
Monitor.Enter(_lock);

try
{
    balance += 100;
}
finally
{
    Monitor.Exit(_lock);
}

-------------------
## Sources

1. [Microsoft Learn — Monitor Class](https://learn.microsoft.com/en-us/dotnet/api/system.threading.monitor)
2. [Microsoft Learn — The lock statement](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/statements/lock)

-------------------
