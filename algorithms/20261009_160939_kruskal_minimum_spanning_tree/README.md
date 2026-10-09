# Kruskal Minimum Spanning Tree with Disjoint-Set Union

A clean, dependency-free **C#** reference implementation of **Kruskal Minimum Spanning Tree with Disjoint-Set Union**, focused on core algorithmic mechanics, clear memory layout, and test verification.

---

## 🏛️ Architecture & Design Decisions

This module organizes `Kruskal Minimum Spanning Tree with Disjoint-Set Union` into an isolated, self-contained unit:
* **Domain Focus**: `Balanced Hierarchical Indexing`
* **Primary Primitives**: `Node Pointers & Self-Balancing Trees`
* **Memory Strategy**: Zero external heap dependencies; designed as a pure in-memory algorithmic component.
* **Correctness Model**: Execution behavior is validated against nominal workflows and boundary edge cases.

### Asymptotic Complexity

| Metric | Bound | Characteristics |
| :--- | :---: | :--- |
| **Best Case Time** | `O(1)` | Optimized fast-path execution |
| **Average / Worst Time** | `O(log N)` | Deterministic upper bound for generalized workloads |
| **Space Complexity** | `O(N)` | Strict bounds without unconstrained heap growth |

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

<sub>Standard C# reference implementation • Maintained by [@myonathanlinkedin](https://github.com/myonathanlinkedin)</sub>