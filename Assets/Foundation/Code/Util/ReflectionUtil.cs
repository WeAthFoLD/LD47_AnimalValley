
using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.Utilities;

public static class ReflectionUtil {

    /// <summary>
    ///
    /// </summary>
    /// <param name="baseType">Can be interface of base class</param>
    /// <returns></returns>
    public static Type[] FindAllSubTypes(Type baseType) {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var result = new List<Type>();
        assemblies.ForEach(assembly => {
            var types = assembly.GetTypes();
            result.AddRange(types
                .Where(type => type.IsClass && type != baseType && baseType.IsAssignableFrom(type))
                .ToArray());
        });
        return result.ToArray();
    }
}
