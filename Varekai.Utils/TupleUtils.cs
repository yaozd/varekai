using System;

namespace Varekai.Utils
{
    public static class TupleUtils
    {
        public static T1 First<T1, T2>(this Tuple<T1,T2> tuple)
        {
            return tuple.Item1;
        }

        public static T2 Second<T1, T2>(this Tuple<T1,T2> tuple)
        {
            return tuple.Item2;
        }

        public static T1 Head<T1, T2>(this Tuple<T1,T2> tuple)
        {
            return tuple.Item1;
        }

        public static T1 Head<T1, T2, T3>(this Tuple<T1,T2, T3> tuple)
        {
            return tuple.Item1;
        }

        public static Tuple<T2, T3> Tail<T1, T2, T3>(this Tuple<T1,T2, T3> tuple)
        {
            return Tuple.Create(tuple.Item2, tuple.Item3);
        }

        public static T1 Head<T1, T2, T3, T4>(this Tuple<T1,T2, T3, T4> tuple)
        {
            return tuple.Item1;
        }

        public static Tuple<T2, T3, T4> Tail<T1, T2, T3, T4>(this Tuple<T1,T2, T3, T4> tuple)
        {
            return Tuple.Create(tuple.Item2, tuple.Item3, tuple.Item4);
        }
    }
}

