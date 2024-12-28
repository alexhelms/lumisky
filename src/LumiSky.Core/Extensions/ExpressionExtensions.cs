using System.Reflection;

namespace System.Linq.Expressions;

public static class ExpressionExtensions
{
    public static PropertyInfo GetProperty<T>(this Expression<Func<T>> propertyExpression)
    {
        if (propertyExpression is null)
        {
            throw new ArgumentNullException(nameof(propertyExpression));
        }

        if (propertyExpression.Body is not MemberExpression body)
        {
            throw new ArgumentException("Invalid argument", nameof(propertyExpression));
        }

        if (body.Member is not PropertyInfo info)
        {
            throw new ArgumentException("Argument is not a property", nameof(propertyExpression));
        }

        return info;
    }

    public static string? GetPropertyName<T>(this Expression<Func<T>> propertyExpression)
    {
        return propertyExpression.GetProperty()?.Name;
    }

    public static string GetPropertyName<TSource, TProperty>(this Expression<Func<TSource, TProperty>> propertyLambda)
    {
        var type = typeof(TSource);

        if (propertyLambda.Body is not MemberExpression member)
            throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");

        if (member.Member is not PropertyInfo propInfo)
            throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");

        if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType!))
            throw new ArgumentException($"Expression '{propertyLambda}' refers to a property that is not from type {type}.");

        return propInfo.Name;
    }
}
