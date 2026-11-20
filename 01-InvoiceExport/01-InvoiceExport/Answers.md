# Assignment 01 — Answers

Student name:  malak eslam
Group / ID: 2 (Monday & Thursday)

## Problems with the current design

TThe program uses blocking operations.
Orders are processed one by one making the export slower.
The thread waits during file and database operations.

## What you changed

1. Replaced Thread.Sleep with await Task.Delay to simulate non blocking delays
2. Created an ExportOneAsync method to handle the export process for one order.
Started the export tasks before awaiting them individually
allowing i/o operations from different orders to overlap

Result before the Changes :

invoice-1001.txt
invoice-1002.txt
invoice-1003.txt
invoice-1004.txt
invoice-1005.txt

Export finished in 6.0 s   -The export takes a long time because each order is processed separately one after another

Result after the Changes :

invoice-1004.txt
invoice-1003.txt
invoice-1005.txt
invoice-1001.txt
invoice-1002.txt

Export finished in 1.9 s   The export takes less time than before and orders are processed concurrently.

