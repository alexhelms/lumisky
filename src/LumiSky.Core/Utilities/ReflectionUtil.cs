namespace LumiSky.Core.Utilities;

public static class ReflectionUtil
{
    private readonly static Dictionary<(Type, Type), bool> _assignableGenericTypeCache = new();

    public static bool IsAssignableToGenericType(Type givenType, Type genericType)
    {
        if (_assignableGenericTypeCache.TryGetValue((givenType, genericType), out var isAssignable))
        {
            return isAssignable;
        }

        var interfaceTypes = givenType.GetInterfaces();

        foreach (var it in interfaceTypes)
        {
            if (it.IsGenericType && it.GetGenericTypeDefinition() == genericType)
            {
                StoreInCache(true);
                return true;
            }
        }

        if (givenType.IsGenericType && givenType.GetGenericTypeDefinition() == genericType)
        {
            StoreInCache(true);
            return true;
        }

        Type baseType = givenType.BaseType!;
        if (baseType == null)
        {
            StoreInCache(false);
            return false;
        }

        return IsAssignableToGenericType(baseType, genericType);

        void StoreInCache(bool isAssignable) => _assignableGenericTypeCache[(givenType, genericType)] = isAssignable;
    }
}
