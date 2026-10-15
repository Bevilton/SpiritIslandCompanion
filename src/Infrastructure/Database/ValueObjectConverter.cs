using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Infrastructure.Database;

/// <summary>
/// Maps a single-value value object to the column holding its value, and back. A row coming
/// out of the database has already passed the value object's own <c>Create</c> once, so it
/// is rebuilt through the constructor directly rather than validated again — the constructor
/// stays private in the domain, and is reached here by reflection, once per closed type.
/// <para>
/// A converter rather than an owned type because a value object is a value. Two seats may
/// hold the very same <c>UserId</c> instance and a converted column does not care; an owned
/// entity is tracked by identity, and reads sharing as the value moving from one owner to the
/// other. Every defensive copy the domain used to make existed only to work around that.
/// </para>
/// </summary>
internal sealed class ValueObjectConverter<TValueObject, TValue>()
    : ValueConverter<TValueObject, TValue>(ToProvider(), FromProvider())
    where TValueObject : notnull
{
    private static Expression<Func<TValueObject, TValue>> ToProvider()
    {
        var valueObject = Expression.Parameter(typeof(TValueObject), "valueObject");
        return Expression.Lambda<Func<TValueObject, TValue>>(Expression.Property(valueObject, "Value"), valueObject);
    }

    private static Expression<Func<TValue, TValueObject>> FromProvider()
    {
        var constructor = typeof(TValueObject).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [typeof(TValue)])
            ?? throw new InvalidOperationException(
                $"{typeof(TValueObject).Name} has no constructor taking a {typeof(TValue).Name}.");

        var value = Expression.Parameter(typeof(TValue), "value");
        return Expression.Lambda<Func<TValue, TValueObject>>(Expression.New(constructor, value), value);
    }
}
