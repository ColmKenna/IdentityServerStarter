namespace PrimativeExtensionMethods;

public static class DictionaryExtensions
{
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
        TValue defaultValue)
    {
        if (dictionary.ContainsKey(key))
        {
            return dictionary[key];
        }

        return defaultValue;
    }
}

public static class ListExtensions
{
    public static bool CompareTwoLists<T>(this List<T> list1, List<T> list2)
    {
        return list1.CompareTwoLists(list2, (l1, l2) => l1.Equals(l2));
    }

    public static bool CompareTwoLists<T>(this List<T> list1, List<T> list2, Func<T, T, bool> comparer)
    {
        if (list1.Count != list2.Count)
        {
            return false;
        }

        for (int i = 0; i < list1.Count; i++)
        {
            if (!comparer(list1[i], list2[i]))
            {
                return false;
            }
        }

        return true;
    }


    public static void RemoveWhere<T>(this IList<T> list, Func<T, bool> predicate)
    {
        var itemsToRemove = list.Where(predicate).ToList();

        foreach (var itemToRemove in itemsToRemove)
        {
            list.Remove(itemToRemove);
        }
    }

    public static void AddWhereNotPresent<T, U>(this IList<T> list,
        IEnumerable<T> newList, Func<T, U> keySelector
    )
    {
        var itemsToAddList = newList.ToList();
        var itemsToAddFiltered = itemsToAddList.Where(x => !list.Select(keySelector).Contains(keySelector(x))).ToList();

        foreach (var itemToAdd in itemsToAddFiltered)
        {
            list.Add(itemToAdd);
        }
    }

    public static IList<T> AddIfNotExists<TKey, T>(this IList<T> list, T newItem, Func<T, TKey> keySelector)
    {
        if (!list.Any(item => EqualityComparer<TKey>.Default.Equals(keySelector(item), keySelector(newItem))))
        {
            list.Add(newItem);
        }

        return list;
    }
    
    public static IList<T> AddIfNotExists<T>(this IList<T> list, T newItem)
    {
        if (!list.Contains(newItem))
        {
            list.Add(newItem);
        }

        return list;
    }
    

    public static void UpdateWhere<T>(this IList<T> list,
        IEnumerable<T> newList, Func<T, T, bool> predicate, Action<T, T> updateAction
    )
    {
        foreach (var item in list)
        {
            if (newList.Any(x => predicate(item, x)))
            {
                updateAction(item, newList.First(x => predicate(item, x)));
            }
        }
    }

    public static bool IsContainedIn<T>(this T item, IEnumerable<T> list)
    {
        return list.Contains(item);
    }
}