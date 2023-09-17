using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Lumafly.Util
{
    internal static class EventArgsCache
    {
        internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new (NotifyCollectionChangedAction.Reset);
    }

    public class SortableObservableCollection<T> : ObservableCollection<T>
    {
        public SortableObservableCollection(IEnumerable<T> iter) : base(iter) {}
        
        public void SortBy(Func<T, T, int> comparer)
        {
            // This shouldn't ever change due to binary serialization constraints.
            if (Items is not List<T> items)
                throw new InvalidOperationException("The backing field type is not List<T> on Collection<T>.");

            items.Sort((x, y) => comparer(x, y));
            
            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        }
    }
}
