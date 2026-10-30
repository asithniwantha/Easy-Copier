using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Easy_Copier.Infrastructure
{
    /// <summary>
    /// An <see cref="ObservableCollection{T}"/> that supports bulk updates while suppressing
    /// individual collection change notifications, raising a single <see cref="NotifyCollectionChangedAction.Reset"/> notification when complete.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        /// <summary>
        /// Replaces the current contents with the provided items, raising a single <see cref="NotifyCollectionChangedAction.Reset"/> notification.
        /// </summary>
        /// <param name="newItems">The sequence of new items to populate the collection with.</param>
        public void ReplaceRange(IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(newItems);

            _suppressNotification = true;
            try
            {
                Items.Clear();
                foreach (T item in newItems)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Adds a sequence of items, raising a single <see cref="NotifyCollectionChangedAction.Reset"/> notification.
        /// </summary>
        /// <param name="newItems">The sequence of items to add.</param>
        public void AddRange(IEnumerable<T> newItems)
        {
            ArgumentNullException.ThrowIfNull(newItems);

            _suppressNotification = true;
            try
            {
                foreach (T item in newItems)
                {
                    Items.Add(item);
                }
            }
            finally
            {
                _suppressNotification = false;
                OnPropertyChanged(new PropertyChangedEventArgs("Count"));
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <inheritdoc />
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnCollectionChanged(e);
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (!_suppressNotification)
            {
                base.OnPropertyChanged(e);
            }
        }
    }
}

