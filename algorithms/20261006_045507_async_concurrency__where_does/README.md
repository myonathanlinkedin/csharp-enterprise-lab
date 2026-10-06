# Async Concurrency: Where does the scheduler live?

A clean, dependency-free **C#** implementation of **Async Concurrency: Where does the scheduler live?**, focused on predictable latency, strict memory layout, and deterministic execution.

---

## 🏛️ Architecture & Design Decisions

This module organizes `Async Concurrency: Where does the scheduler live?` into an isolated, self-contained unit:
* **Domain Focus**: `Algorithmic Engineering`
* **Primary Primitives**: `Standard Memory Primitives`
* **Memory Strategy**: Buffer boundaries are strictly verified to prevent out-of-bounds access and memory leak hazards.
* **Correctness Model**: State transitions adhere to strict ordering guarantees with explicit synchronization fences where necessary.

### Asymptotic Complexity

| Metric | Bound | Characteristics |
| :--- | :---: | :--- |
| **Best Case Time** | `$O(1)$` | Optimized fast-path execution |
| **Average / Worst Time** | `$O(N)$` | Deterministic upper bound for generalized workloads |
| **Space Complexity** | `$O(N)$` | Strict bounds without unconstrained heap growth |

---

## 🧪 Verification Suite

The accompanying `main.cs` driver executes self-contained verification tests:
1. **Nominal Flow**: Validates baseline correctness under typical real-world inputs.
2. **Boundary Conditions**: Exercises extreme edge cases (empty inputs, singletons, capacity limits).
3. **Invariant Preservation**: Validates internal state consistency throughout mutation lifecycles.

### Running Locally

```bash
csharp main.cs
```

---

*Curated as part of the Polyglot Systems Lab • Maintained by [@myonathanlinkedin](https://github.com/myonathanlinkedin)*