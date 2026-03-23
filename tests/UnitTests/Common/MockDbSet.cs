using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;

namespace UnitTests.Common;

/// <summary>
/// Creates a Moq DbSet that supports EF Core async LINQ operations
/// (ToListAsync, FirstOrDefaultAsync, etc.) without an in-memory database.
/// EF-specific methods like AsNoTracking() are stripped before expression evaluation.
/// </summary>
public static class MockDbSet
{
    public static Mock<DbSet<T>> Create<T>(IList<T> data) where T : class
    {
        var source = new AsyncEnumerable<T>(data.AsQueryable());
        var mock = new Mock<DbSet<T>>();

        mock.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(source.GetAsyncEnumerator());

        mock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(source.Provider);
        mock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(source.Expression);
        mock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(source.ElementType);
        mock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => source.GetEnumerator());

        return mock;
    }
}

/// <summary>
/// An IQueryable wrapper that also implements IAsyncEnumerable and propagates
/// the async provider through all LINQ transformations.
/// </summary>
// IOrderedQueryable<T> is needed because OrderBy/ThenBy cast the result to it.
// It adds no members beyond IQueryable<T> so implementing it is trivial.
file sealed class AsyncEnumerable<T>(IQueryable<T> inner) : IOrderedQueryable<T>, IAsyncEnumerable<T>
{
    public Type ElementType => inner.ElementType;
    public Expression Expression => inner.Expression;
    public IQueryProvider Provider => new AsyncQueryProvider(inner.Provider);

    public IEnumerator<T> GetEnumerator() => inner.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken _ = default) =>
        new AsyncEnumerator<T>(inner.GetEnumerator());
}

file sealed class AsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;
    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
    public ValueTask DisposeAsync() { inner.Dispose(); return ValueTask.CompletedTask; }
}

/// <summary>
/// Query provider that wraps all created sub-queries in AsyncEnumerable,
/// ensuring the async capability is maintained through LINQ chains.
/// </summary>
file sealed class AsyncQueryProvider(IQueryProvider inner) : IAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) =>
        inner.CreateQuery(EfExpressionStripper.Strip(expression));

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        var stripped = EfExpressionStripper.Strip(expression);
        return new AsyncEnumerable<TElement>(new EnumerableQuery<TElement>(stripped));
    }

    public object? Execute(Expression expression) =>
        inner.Execute(EfExpressionStripper.Strip(expression));

    public TResult Execute<TResult>(Expression expression) =>
        inner.Execute<TResult>(EfExpressionStripper.Strip(expression));

    public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken _ = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var result = typeof(IQueryProvider)
            .GetMethod(nameof(Execute), 1, [typeof(Expression)])!
            .MakeGenericMethod(resultType)
            .Invoke(this, [EfExpressionStripper.Strip(expression)]);

        return (TResult)typeof(Task)
            .GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, [result])!;
    }
}

/// <summary>
/// Strips EF Core-specific extension methods from expression trees so they
/// can be evaluated by standard in-memory LINQ.
/// </summary>
file sealed class EfExpressionStripper : ExpressionVisitor
{
    private static readonly HashSet<string> EfMethods =
    [
        "AsNoTracking", "AsTracking", "AsNoTrackingWithIdentityResolution",
        "Include", "ThenInclude", "TagWith", "AsSplitQuery", "AsSingleQuery"
    ];

    public static Expression Strip(Expression expression) =>
        new EfExpressionStripper().Visit(expression);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Arguments.Count >= 1 &&
            EfMethods.Contains(node.Method.Name) &&
            node.Method.DeclaringType?.Namespace?.StartsWith("Microsoft.EntityFrameworkCore") == true)
        {
            return Visit(node.Arguments[0]);
        }

        return base.VisitMethodCall(node);
    }
}
