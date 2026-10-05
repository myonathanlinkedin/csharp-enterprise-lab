# Grid Theory and Polynomiality in Dynamic Lot-Sizing

High-performance **Grid Theory and Polynomiality in Dynamic Lot-Sizing** primitive implemented in idiomatic **C#**. Built from scratch using standard library constructs with zero external dependencies.

### Core Highlights
* **Language & Standard**: Modern `C#` standard library conventions.
* **Architecture Pattern**: Designed for `Algorithmic Engineering` using `Standard Memory Primitives`.
* **Runtime Overhead**: Memory allocations are kept minimal to avoid allocator contention and preserve CPU cache locality.
* **Concurrency & Safety**: State transitions adhere to strict ordering guarantees with explicit synchronization fences where necessary.

---

### Complexity Analysis

| Dimension | Bound |
| :--- | :--- |
| **Time (Best Case)** | `$O(1)$` |
| **Time (Worst Case)** | `$O(N \log N)$` |
| **Auxiliary Space** | `$O(N)$` |

---

### Test Suite Execution

Self-contained verification drivers are embedded directly in `main.cs` to validate happy paths, boundary inputs, and invariant preservation.

```bash
csharp main.cs
```

---

*Source code released under the MIT License • [@myonathanlinkedin](https://github.com/myonathanlinkedin)*