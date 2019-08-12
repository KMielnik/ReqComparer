using ReqComparer;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualComparer
{
    public class ListWithNotifications<T> : List<T>, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public async Task AddRangeNotifyFinishAsync(IEnumerable<T> items)
        {
            await Task.Run(() => AddRange(items));
            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
