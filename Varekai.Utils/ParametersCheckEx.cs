using System;

namespace Varekai.Utils
{
    public static class ParametersCheckEx
    {
        public static void EnsureIsNotNull<T>(this T parameter, string paramName = "")
        {
            if (parameter == null)
                throw new ArgumentNullException(paramName);
        }

        public static void EnsureHasValue(this string parameter, string paramName = "")
        {
            if (string.IsNullOrWhiteSpace(parameter))
                throw new ArgumentException("The argument must have a value", paramName);
        }
    }
}

