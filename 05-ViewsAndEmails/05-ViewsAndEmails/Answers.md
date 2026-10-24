# Assignment 05 — Answers

Student name: malak eslam
Group / ID: 2

## Failures you observed

 The actual views were less than 50000
 Some emails were missing
 An exception could happen when the threads accessed the queue at the same time

 ------------------------
## What you changed

  Used lock to fix the views count
  Used lock  when adding and removing email jobs
  Used another lock  when adding emails to the list
  Moved Thread.Sleep(1) outside the lock
  Removed the  try catch because it was no longer needed

   ------------------------
## Collection safety vs business-rule safety

  Collection safety protects data when multiple threads use it
  Business rules describe what the program should do
  A safe collection does not mean the whole program is correct

   ------------------------
   result before
    Views expected=50000, actual=25400          
    Email jobs placed=20, unique sent=18, total sends=18

   result after
    Views expected=50000, actual=50000
    Email jobs placed=20, unique sent=20, total sends=20

    ------------------------
