using System;
using System.Reflection;

namespace Varekai.Utils
{
    public static class AnonymousObjectsUtils
    {
        public static TPT ValueOf<TPT>(this object @object, string propertyName)
        {
            @object.EnsureIsNotNull();

            var propInfo = @object
                .GetType()
                .GetProperty(propertyName);

            return (TPT)propInfo.GetValue(@object);
        }
    }
}

