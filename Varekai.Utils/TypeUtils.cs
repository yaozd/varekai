using System;

namespace Varekai.Utils
{
    public static class TypeUtils
    {
        public static T CastFromTo<TS,T>(this TS source, string sourceName = "") where T : struct, IConvertible
        {
            var errorMessage = string.Format("The requested item {0} with value {1} cannot be converted to the type {2}"
                , sourceName
                , source
                , typeof(T).FullName);

            try
            {
                return (T)Convert.ChangeType(source, typeof(T));
            }
            catch (FormatException)
            {
                throw new ArgumentException(errorMessage);
            }
        }
    }
}

