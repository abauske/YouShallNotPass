using System;
using System.Collections.Generic;
using System.Linq;

public static class SelectExtension
{
    // I don't like this name :(
    public static IEnumerable<TResult> SelectWithPrevious<TSource, TResult>
    (this IEnumerable<TSource> source,
        Func<TSource, TSource, TResult> projection)
    {
        using (var iterator = source.GetEnumerator())
        {
            if (!iterator.MoveNext())
            {
                yield break;
            }
            TSource previous = iterator.Current;
            while (iterator.MoveNext())
            {
                yield return projection(previous, iterator.Current);
                previous = iterator.Current;
            }
        }
    }
    
    public static TSource MinBy<TSource>(this List<TSource> dict, Func<TSource, float> comp)
    {
        float minVal = Single.MaxValue;
        TSource min = default(TSource);
        foreach (var tuple in dict)
        {
            var val = comp(tuple);
            if (val < minVal)
            {
                minVal = val;
                min = tuple;
            }
        }
        return min;
    }
    
    public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
    {
        return source.Select((item, index) => (item, index));
    }
}