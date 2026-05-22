using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace AppointmentScheduler.API.Tests.Helpers;

internal static class MockDbSetFactory
{
    public static Mock<DbSet<TEntity>> Create<TEntity>(
        List<TEntity> source,
        Func<TEntity, object?[]?>? keySelector = null)
        where TEntity : class
    {
        var mockSet = new Mock<DbSet<TEntity>>();

        mockSet.As<IAsyncEnumerable<TEntity>>()
            .Setup(x => x.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) => new TestAsyncEnumerator<TEntity>(source.GetEnumerator()));

        mockSet.As<IEnumerable<TEntity>>()
            .Setup(x => x.GetEnumerator())
            .Returns(() => source.GetEnumerator());

        mockSet.As<IQueryable<TEntity>>()
            .Setup(x => x.Provider)
            .Returns(() => new TestAsyncQueryProvider<TEntity>(source.AsQueryable().Provider));

        mockSet.As<IQueryable<TEntity>>()
            .Setup(x => x.Expression)
            .Returns(() => source.AsQueryable().Expression);

        mockSet.As<IQueryable<TEntity>>()
            .Setup(x => x.ElementType)
            .Returns(() => source.AsQueryable().ElementType);

        mockSet.As<IQueryable<TEntity>>()
            .Setup(x => x.GetEnumerator())
            .Returns(() => source.AsQueryable().GetEnumerator());

        mockSet.Setup(x => x.Add(It.IsAny<TEntity>()))
            .Returns<TEntity>(entity =>
            {
                source.Add(entity);
                return null!;
            });

        mockSet.Setup(x => x.AddRange(It.IsAny<IEnumerable<TEntity>>()))
            .Callback<IEnumerable<TEntity>>(entities => source.AddRange(entities));

        mockSet.Setup(x => x.Remove(It.IsAny<TEntity>()))
            .Returns<TEntity>(entity =>
            {
                source.Remove(entity);
                return null!;
            });

        if (keySelector != null)
        {
            mockSet.Setup(x => x.FindAsync(It.IsAny<object?[]?>()))
                .Returns((object?[]? keyValues) =>
                {
                    var entity = source.FirstOrDefault(item => KeysMatch(keySelector(item), keyValues));
                    return new ValueTask<TEntity?>(entity);
                });
        }

        return mockSet;
    }

    private static bool KeysMatch(IReadOnlyList<object?>? expectedKeys, IReadOnlyList<object?>? actualKeys)
    {
        if (expectedKeys == null || actualKeys == null || expectedKeys.Count != actualKeys.Count)
            return false;

        for (var index = 0; index < expectedKeys.Count; index++)
        {
            if (!Equals(expectedKeys[index], actualKeys[index]))
                return false;
        }

        return true;
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var cleanedExpression = StripEfOperators(expression);
            var elementType = cleanedExpression.Type.GetGenericArguments().FirstOrDefault() ?? typeof(TEntity);
            var asyncEnumerableType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(asyncEnumerableType, cleanedExpression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(StripEfOperators(expression));

        public object? Execute(Expression expression)
            => _inner.Execute(StripEfOperators(expression));

        public TResult Execute<TResult>(Expression expression)
            => _inner.Execute<TResult>(StripEfOperators(expression));

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var cleanedExpression = StripEfOperators(expression);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeof(TResult).GenericTypeArguments[0];
                var executionResult = typeof(IQueryProvider)
                    .GetMethods()
                    .Single(method => method.Name == nameof(IQueryProvider.Execute) && method.IsGenericMethod)
                    .MakeGenericMethod(resultType)
                    .Invoke(_inner, new object[] { cleanedExpression });

                return (TResult)typeof(Task)
                    .GetMethods()
                    .Single(method => method.Name == nameof(Task.FromResult) && method.IsGenericMethod)
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new[] { executionResult })!;
            }

            return Execute<TResult>(cleanedExpression);
        }

        private static Expression StripEfOperators(Expression expression)
            => new EfOperatorStripper().Visit(expression)!;
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
            => new(_inner.MoveNext());
    }

    private sealed class EfOperatorStripper : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions)
                && node.Arguments.Count > 0
                && node.Method.Name is nameof(EntityFrameworkQueryableExtensions.Include)
                    or nameof(EntityFrameworkQueryableExtensions.ThenInclude)
                    or nameof(EntityFrameworkQueryableExtensions.AsNoTracking)
                    or "AsSplitQuery")
            {
                return Visit(node.Arguments[0]);
            }

            return base.VisitMethodCall(node);
        }
    }
}