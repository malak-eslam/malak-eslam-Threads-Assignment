Assignment 02 — Answers
Student name: malak eslam
Group / ID: 2 

-- What is wrong
     Multiple threads can book tables at the same time
     They may read the available tables before another thread updates the number
     causing incorrect booking totals.

-- How you fixed it
    I used lock to allow only one thread at a time to check table availability and update the booking totals
    This prevents race conditions and keeps the final numbers correct.


-- result before fixing the issue:

Customer-03 booked a table. Left=2
Customer-01 booked a table. Left=1
Customer-02 booked a table. Left=2
Customer-04 booked a table. Left=1
Customer-05 booked a table. Left=2
Customer-06 booked a table. Left=1
Customer-09 booked a table. Left=0
Customer-07 booked a table. Left=1
Customer-11 booked a table. Left=-4
Customer-10 booked a table. Left=-4
Customer-08 booked a table. Left=-4
Customer-12 booked a table. Left=-4

Booked=6, TablesLeft=-4


-- result after fixing the issue:

Customer-01 booked a table. Left=4
Customer-02 booked a table. Left=3
Customer-03 booked a table. Left=2
Customer-04 booked a table. Left=1
Customer-05 booked a table. Left=0

Booked=5, TablesLeft=0