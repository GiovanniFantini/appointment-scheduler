using System.Linq.Expressions;
using AppointmentScheduler.Data;
using Microsoft.EntityFrameworkCore;

namespace AppointmentScheduler.API.Tests.Helpers;

internal sealed class ApplicationDbContextMockBuilder
{
    private readonly List<Action<Mock<IApplicationDbContext>>> _setups = new();

    public ApplicationDbContextMockBuilder WithSet<TEntity>(
        Expression<Func<IApplicationDbContext, DbSet<TEntity>>> propertyExpression,
        List<TEntity>? source = null,
        Func<TEntity, object?[]?>? keySelector = null)
        where TEntity : class
    {
        var items = source ?? [];
        _setups.Add(context =>
            context.SetupGet(propertyExpression)
                .Returns(MockDbSetFactory.Create(items, keySelector).Object));

        return this;
    }

    public ApplicationDbContextMockBuilder WithEmptySet<TEntity>(
        Expression<Func<IApplicationDbContext, DbSet<TEntity>>> propertyExpression,
        Func<TEntity, object?[]?>? keySelector = null)
        where TEntity : class
        => WithSet(propertyExpression, [], keySelector);

    public Mock<IApplicationDbContext> Build()
        => Build(out _);

    public Mock<IApplicationDbContext> Build(out SaveChangesTracker tracker)
    {
        var localTracker = new SaveChangesTracker();
        tracker = localTracker;

        var context = new Mock<IApplicationDbContext>();
        foreach (var setup in _setups)
        {
            setup(context);
        }

        context.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => localTracker.Count++)
            .ReturnsAsync(1);

        return context;
    }
}