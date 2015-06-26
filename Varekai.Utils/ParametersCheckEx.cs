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

        public static T ConvertToType<TS,T>(this TS parameter, string paramName = "") where T : struct, IConvertible
        {
            var errorMessage = string.Format("The parameter {0} with value {1} cannot be converted to the type {2}"
				, paramName
				, parameter
				, typeof(T).FullName);

            try
            {
                return (T)Convert.ChangeType(parameter, typeof(T));
            }
            catch (FormatException)
            {
                throw new ArgumentException(errorMessage);
            }
        }
    }
}

