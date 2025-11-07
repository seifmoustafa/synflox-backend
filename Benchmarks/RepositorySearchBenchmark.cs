using BenchmarkDotNet.Attributes;
using System.Linq.Expressions;

public class RepositorySearchBenchmark
{
    private IQueryable<TestEntity> _data = null!;
    private readonly string _term = "50";

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(0, 10000).Select(i => new TestEntity
        {
            Name = $"Name{i}",
            Email = $"email{i}@test.com",
            Age = i,
            CreatedAt = DateTime.UtcNow.AddDays(i),
            Extra1 = i.ToString(),
            Extra2 = i.ToString(),
            Extra3 = i.ToString(),
            Extra4 = i.ToString(),
            Extra5 = i.ToString(),
            Extra6 = i.ToString(),
            Extra7 = i.ToString(),
            Extra8 = i.ToString(),
            Extra9 = i.ToString(),
            Extra10 = i.ToString(),
            Extra11 = i.ToString(),
            Extra12 = i.ToString(),
            Extra13 = i.ToString(),
            Extra14 = i.ToString(),
            Extra15 = i.ToString(),
            Extra16 = i.ToString(),
            Extra17 = i.ToString(),
            Extra18 = i.ToString(),
            Extra19 = i.ToString(),
            Extra20 = i.ToString()
        }).AsQueryable();
    }

    [Benchmark]
    public int Reflection()
    {
        var expr = GenerateSearchExpressionReflection<TestEntity>(_term);
        return _data.Where(expr).Count();
    }

    [Benchmark]
    public int Explicit()
    {
        var expr = GenerateSearchExpressionExplicit<TestEntity>(_term,
            e => e.Name,
            e => e.Email,
            e => e.Age,
            e => e.CreatedAt,
            e => e.Extra1,
            e => e.Extra2,
            e => e.Extra3,
            e => e.Extra4,
            e => e.Extra5,
            e => e.Extra6,
            e => e.Extra7,
            e => e.Extra8,
            e => e.Extra9,
            e => e.Extra10,
            e => e.Extra11,
            e => e.Extra12,
            e => e.Extra13,
            e => e.Extra14,
            e => e.Extra15,
            e => e.Extra16,
            e => e.Extra17,
            e => e.Extra18,
            e => e.Extra19,
            e => e.Extra20);
        return _data.Where(expr).Count();
    }

    // Reflection-based implementation
    private static Expression<Func<T, bool>> GenerateSearchExpressionReflection<T>(string PageSearch)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var properties = typeof(T).GetProperties()
            .Where(p => p.CanRead);

        Expression? searchExpression = null;
        var searchValue = Expression.Constant(PageSearch);
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

        foreach (var property in properties)
        {
            var propertyExpression = Expression.Property(parameter, property);

            Expression stringExpr = propertyExpression;
            if (property.PropertyType != typeof(string))
            {
                stringExpr = Expression.Call(propertyExpression, property.PropertyType.GetMethod(nameof(object.ToString), Type.EmptyTypes)!);
            }

            Expression? propertyCondition = null;

            if (containsMethod != null)
                propertyCondition = Expression.Call(stringExpr, containsMethod, searchValue);

            if (propertyCondition != null)
            {
                searchExpression = searchExpression == null
                    ? propertyCondition
                    : Expression.OrElse(searchExpression, propertyCondition);
            }
        }

        return searchExpression != null
            ? Expression.Lambda<Func<T, bool>>(searchExpression, parameter)
            : entity => true;
    }

    // Explicit field list implementation
    private static Expression<Func<T, bool>> GenerateSearchExpressionExplicit<T>(string PageSearch, params Expression<Func<T, object>>[] searchColumns)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? searchExpression = null;

        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) });

        foreach (var column in searchColumns)
        {
            Expression body = column.Body;
            if (body.NodeType == ExpressionType.Convert && body is UnaryExpression unary)
                body = unary.Operand;

            var replaced = new ReplaceParameterVisitor(column.Parameters[0], parameter).Visit(body)!;

            if (replaced.Type != typeof(string))
            {
                replaced = Expression.Call(replaced, replaced.Type.GetMethod(nameof(object.ToString), Type.EmptyTypes)!);
            }

            Expression? propertyCondition = null;
            if (containsMethod != null)
                propertyCondition = Expression.Call(replaced, containsMethod, Expression.Constant(PageSearch));

            if (propertyCondition != null)
            {
                searchExpression = searchExpression == null
                    ? propertyCondition
                    : Expression.OrElse(searchExpression, propertyCondition);
            }
        }

        return searchExpression != null
            ? Expression.Lambda<Func<T, bool>>(searchExpression, parameter)
            : entity => true;
    }

    private sealed class ReplaceParameterVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _oldParameter;
        private readonly Expression _newExpression;

        public ReplaceParameterVisitor(ParameterExpression oldParameter, Expression newExpression)
        {
            _oldParameter = oldParameter;
            _newExpression = newExpression;
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _oldParameter ? _newExpression : base.VisitParameter(node);
    }

    private class TestEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Extra1 { get; set; } = string.Empty;
        public string Extra2 { get; set; } = string.Empty;
        public string Extra3 { get; set; } = string.Empty;
        public string Extra4 { get; set; } = string.Empty;
        public string Extra5 { get; set; } = string.Empty;
        public string Extra6 { get; set; } = string.Empty;
        public string Extra7 { get; set; } = string.Empty;
        public string Extra8 { get; set; } = string.Empty;
        public string Extra9 { get; set; } = string.Empty;
        public string Extra10 { get; set; } = string.Empty;
        public string Extra11 { get; set; } = string.Empty;
        public string Extra12 { get; set; } = string.Empty;
        public string Extra13 { get; set; } = string.Empty;
        public string Extra14 { get; set; } = string.Empty;
        public string Extra15 { get; set; } = string.Empty;
        public string Extra16 { get; set; } = string.Empty;
        public string Extra17 { get; set; } = string.Empty;
        public string Extra18 { get; set; } = string.Empty;
        public string Extra19 { get; set; } = string.Empty;
        public string Extra20 { get; set; } = string.Empty;
    }
}

