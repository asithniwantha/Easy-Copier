using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.Infrastructure
{
    public static class ObservableCollectionExtensions
    {
        public static void UpdateFrom<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(newItems);

            collection.Clear();
            foreach (T item in newItems)
            {
                collection.Add(item);
            }
        }
    }
}
