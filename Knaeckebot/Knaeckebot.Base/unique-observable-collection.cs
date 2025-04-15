using Knaeckebot.Services;
using System.Collections.ObjectModel;

namespace Knaeckebot
{
    /// <summary>
    /// Extended ObservableCollection that doesn't allow duplicates
    /// </summary>
    public class UniqueObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Adds an element only if it's not already in the collection
        /// </summary>
        public new void Add(T item)
        {
            if (!this.Contains(item))
            {
                base.Add(item);
                LogManager.Log($"UniqueObservableCollection: Element added, new size: {this.Count}", LogLevel.Debug);
            }
            else
            {
                LogManager.Log($"UniqueObservableCollection: Element already exists, not added", LogLevel.Debug);
            }
        }
    }
}