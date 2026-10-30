using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// Provides extension methods for working with <see cref="ObservableCollection{T}"/>.
    /// </summary>
    public static class ObservableCollectionExtensions
    {
        /// <summary>
        /// Replaces the contents of an <see cref="ObservableCollection{T}"/> with a new collection of items.
        /// When the target collection is a <see cref="BulkObservableCollection{T}"/>, batch suppression is used
        /// so only a single Reset notification is raised to prevent UI rendering overhead.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The target collection to update.</param>
        /// <param name="newItems">The sequence of new items to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="newItems"/> is <c>null</c>.</exception>
        public static void UpdateFrom<T>(this ObservableCollection<T> collection, IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(newItems);

            if (collection is BulkObservableCollection<T> bulkCollection)
            {
                bulkCollection.ReplaceRange(newItems);
                return;
            }

            collection.Clear();
            foreach (T item in newItems)
            {
                collection.Add(item);
            }
        }
    }
}
