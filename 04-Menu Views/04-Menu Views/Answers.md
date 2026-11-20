# Assignment 04 — Answers

Student name: malak eslam 
Group / ID: 2 Tuesday and Thursday

## Why Expected and Actual disagree

- Views++ is not atomic which means it is not done as one complete operation
- Two threads can update the value at the same time causing some updates to be lost.

## What you changed

- I used lock to make sure only one thread updates Views at a time.

## When would you protect the update differently

- I would use Interlocked.Increment to safely increase one number
-  and lock to protect multiple operations.

--------------------------
 result before using lock:
  Expected: 400000
  Actual:   180400    --> increasing

--------------------------
 result after using lock:
  Expected: 400000
  Actual:   400000
  
  --------------------------
