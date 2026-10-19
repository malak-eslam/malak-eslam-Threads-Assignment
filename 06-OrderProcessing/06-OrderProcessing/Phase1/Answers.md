# Phase 1 — Answers

-- R1. Why are sync and async times almost the same when orders are awaited one after another?

   Both versions process orders sequentially so each order must finish 
   before the next begins async/await does not automatically make execution faster
   but it allows the thread to do other work while waiting.

   ---------------------------
-- R2. Is the thread ID before an await always the same as after it? Why?

   No,The thread ID may stay the same or change after await
   In my logs it was Thread 11 before and after the first await
   However execution may resume on a different thread depending on 
   the synchronization context and execution environment.

    ----------------------------

-- R3. What does the thread do while Task.Delay() is waiting? And while Thread.Sleep() is waiting?

   Task.Delay() : The thread is free to do other work while waiting.
   Thread.Sleep() : The current thread is blocked until the sleep period ends.

   ----------------------------

-- R4. Why should a repository method return Task<T> instead of T?

  Returning Task<T> allows the caller to use await without blocking the current thread 
  while an asynchronous operation is in progress. 
  The task represents the operation and its eventual result.

   ----------------------------
s
-- R5. What is the difference between File.ReadAllText and File.ReadAllTextAsync? Does the async version occupy a thread while the disk is reading?

  File.ReadAllText(): Reads the file synchronously and blocks the current thread until the operation finishes.

  File.ReadAllTextAsync() : reads the file asynchronously
  allowing the current thread to do other work while waiting for the I/O operation to complete.
  
  It does not need to keep a thread blocked while the disk is reading

    ----------------------------
