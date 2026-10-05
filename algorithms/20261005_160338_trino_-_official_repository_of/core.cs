using System;
using System.Collections.Generic;
using System.Linq;

namespace TrinoSim
{
    // Simple representation of a table row as a dictionary of column name to value.
    public sealed class Row : Dictionary<string, object>
    {
        public Row() : base(StringComparer.OrdinalIgnoreCase) { }
        public Row(IDictionary<string, object> source) : base(source, StringComparer.OrdinalIgnoreCase) { }
    }

    // Interface for a data source connector.
    public interface IConnector
    {
        // Retrieves a table by name. Throws if not found.
        ITable GetTable(string name);
    }

    // Interface for a table.
    public interface ITable
    {
        string Name { get; }
        IEnumerable<Row> Scan();
    }

    // In‑memory implementation of ITable.
    public sealed class InMemoryTable : ITable
    {
        public string Name { get; }
        private readonly List<Row> _rows;

        public InMemoryTable(string name, IEnumerable<IDictionary<string, object>> rows)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _rows = rows.Select(r => new Row(r)).ToList();
        }

        public IEnumerable<Row> Scan() => _rows;
    }

    // In‑memory connector holding a set of tables.
    public sealed class InMemoryConnector : IConnector
    {
        private readonly Dictionary<string, ITable> _tables;

        public InMemoryConnector(IEnumerable<ITable> tables)
        {
            _tables = tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        }

        public ITable GetTable(string name)
        {
            if (!_tables.TryGetValue(name, out var table))
                throw new InvalidOperationException($"Table '{name}' not found.");
            return table;
        }
    }

    // Abstract plan node.
    public abstract class PlanNode
    {
        public abstract IEnumerable<Row> Execute();
    }

    // Scan node reads all rows from a table.
    public sealed class TableScanNode : PlanNode
    {
        private readonly ITable _table;

        public TableScanNode(ITable table) => _table = table ?? throw new ArgumentNullException(nameof(table));

        public override IEnumerable<Row> Execute() => _table.Scan();
    }

    // Filter node applies a predicate to its child node.
    public sealed class FilterNode : PlanNode
    {
        private readonly PlanNode _source;
        private readonly Func<Row, bool> _predicate;

        public FilterNode(PlanNode source, Func<Row, bool> predicate)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        public override IEnumerable<Row> Execute() => _source.Execute().Where(_predicate);
    }

    // Project node selects a subset of columns.
    public sealed class ProjectNode : PlanNode
    {
        private readonly PlanNode _source;
        private readonly string[] _columns;

        public ProjectNode(PlanNode source, IEnumerable<string> columns)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _columns = columns?.ToArray() ?? throw new ArgumentNullException(nameof(columns));
            if (_columns.Length == 0) throw new ArgumentException("At least one column must be projected.");
        }

        public override IEnumerable<Row> Execute()
        {
            foreach (var row in _source.Execute())
            {
                var projected = new Row();
                foreach (var col in _columns)
                {
                    if (row.TryGetValue(col, out var val))
                        projected[col] = val;
                    else
                        projected[col] = null;
                }
                yield return projected;
            }
        }
    }

    // Simple query description.
    public sealed class Query
    {
        public string TableName { get; }
        public Func<Row, bool>? Filter { get; }
        public IReadOnlyList<string> Columns { get; }

        public Query(string tableName, IEnumerable<string> columns, Func<Row, bool>? filter = null)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
            Columns = columns?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(columns));
            if (Columns.Count == 0) throw new ArgumentException("Select at least one column.");
            Filter = filter;
        }
    }

    // Engine that builds and executes a plan from a Query.
    public sealed class ExecutionEngine
    {
        private readonly IConnector _connector;

        public ExecutionEngine(IConnector connector) => _connector = connector ?? throw new ArgumentNullException(nameof(connector));

        // Build a linear plan: Scan -> optional Filter -> Project.
        private PlanNode BuildPlan(Query query)
        {
            var table = _connector.GetTable(query.TableName);
            PlanNode node = new TableScanNode(table);
            if (query.Filter != null)
                node = new FilterNode(node, query.Filter);
            node = new ProjectNode(node, query.Columns);
            return node;
        }

        // Execute the query and materialize results.
        public List<Row> Execute(Query query)
        {
            var plan = BuildPlan(query);
            return plan.Execute().ToList();
        }
    }
}
