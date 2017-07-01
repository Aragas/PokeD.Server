using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace PokeD.Server.Data
{
    /// <summary>
    /// Class ReaderWriterLockList. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// https://github.com/petersondrew/ThreadSafeList/blob/master/List/ReaderWriterLockList.cs
    public sealed class ReaderWriterLockList<T> : IList<T>
    {
        /// <summary>
        /// The internal backing list
        /// </summary>
        readonly IList<T> list;

        /// <summary>
        /// The <see cref="ReaderWriterLockSlim"/> instance
        /// </summary>
        readonly ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        public int Count
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return list.Count;
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        public bool IsReadOnly => true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderWriterLockList{T}"/> class.
        /// </summary>
        public ReaderWriterLockList() { list = new List<T>(); }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReaderWriterLockList{T}"/> class.
        /// </summary>
        /// <param name="collection">The collection.</param>
        public ReaderWriterLockList(IEnumerable<T> collection) { list = new List<T>(collection); }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            locker.EnterReadLock();
            try
            {
                foreach (var item in list)
                    yield return item;
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        public void Add(T item)
        {
            locker.EnterWriteLock();
            try
            {
                list.Add(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        public void Clear()
        {
            locker.EnterWriteLock();
            try
            {
                list.Clear();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false.</returns>
        public bool Contains(T item)
        {
            locker.EnterReadLock();
            try
            {
                return list.Contains(item);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            locker.EnterReadLock();
            try
            {
                list.CopyTo(array, arrayIndex);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public bool Remove(T item)
        {
            locker.EnterWriteLock();
            try
            {
                return list.Remove(item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
        public int IndexOf(T item)
        {
            locker.EnterReadLock();
            try
            {
                return list.IndexOf(item);
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        public void Insert(int index, T item)
        {
            locker.EnterWriteLock();
            try
            {
                list.Insert(index, item);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        public void RemoveAt(int index)
        {
            locker.EnterWriteLock();
            try
            {
                list.RemoveAt(index);
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>T.</returns>
        public T this[int index]
        {
            get
            {
                locker.EnterReadLock();
                try
                {
                    return list[index];
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
            set
            {
                locker.EnterWriteLock();
                try
                {
                    list[index] = value;
                }
                finally
                {
                    locker.ExitWriteLock();
                }
            }
        }
    }
}
