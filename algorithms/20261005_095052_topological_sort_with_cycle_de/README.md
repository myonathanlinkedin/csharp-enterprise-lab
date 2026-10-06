# Topological Sort with Cycle Detection in Directed Graphs (C#)

> An in-memory reference implementation of **Topological Sort with Cycle Detection in Directed Graphs** in **C#**, adhering to standard library idioms, clean data structures, and assertion test suites.

## Overview & Mechanics

The implementation focuses on the core mathematical properties of **Topological Sort with Cycle Detection in Directed Graphs**:
* **Data Organization**: Built upon `Adjacency List & Priority Heap` to ensure predictable traversal and storage overhead.
* **Safety Invariants**: Memory allocations are kept minimal to maintain clear data locality and predictable memory bounds.
* **Execution Guarantees**: Encapsulates state within isolated data structures, keeping logic self-contained.

## Complexity Profile

* **Time Complexity**:
  * Fast Path (Best): `O(V + E)`
  * Generalized (Avg / Worst): `O((V + E) log V)`
* **Space Footprint**: `O(V + E)` resident heap / stack overhead.

## Verification & Test Scenarios

The test suite in `main.cs` validates:
* Standard operational paths against expected outcomes.
* Extreme values and edge inputs to ensure robust failure handling.
* State stability across sequential and repeated operations.

```bash
# Execute local verification runner
csharp main.cs
```

---

*Source code released under the MIT License • [@myonathanlinkedin](https://github.com/myonathanlinkedin)*
