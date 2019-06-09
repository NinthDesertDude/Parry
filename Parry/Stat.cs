using System;

namespace Parry
{
    /// <summary>
    /// Stores data with event hooks for when the value is going to
    /// change, has changed, or is about to be retrieved.
    /// </summary>
    public class Stat<T>
    {
        #region Properties
        /// <summary>
        /// Gets or sets the value and fires events.
        /// </summary>
        public T Data
        {
            get
            {
                OnGet?.Invoke(RawData);
                return RawData;
            }
            set
            {
                //Before set, any subscribers can cancel setting the value.
                if (OnBeforeSet?.Invoke(RawData) ?? false)
                {
                    return;
                }

                //Sets the value, then invokes subscribers after set.
                T oldData = RawData;
                RawData = value;

                OnAfterSet?.Invoke(oldData);
            }
        }

        /// <summary>
        /// Gets or sets the value without firing events.
        /// </summary>
        public T RawData { get; set; }
        #endregion

        #region Events
        /// <summary>
        /// This event fires when the value is retrieved, passing in the
        /// value retrieved to all handlers.
        /// </summary>
        public event Action<T> OnGet;

        /// <summary>
        /// This event fires just before the value is set, passing in the new
        /// value to be set and accepting a bool to cancel the setter if true
        /// to all handlers.
        /// </summary>
        public event Func<T, bool> OnBeforeSet;

        /// <summary>
        /// This event fires after the value is set, passing in the old value
        /// to all handlers.
        /// </summary>
        public event Action<T> OnAfterSet;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new stat with the given data.
        /// </summary>
        /// <param name="data">
        /// The data to store.
        /// </param>
        public Stat(T data)
        {
            this.RawData = data;
        }
        #endregion
    }
}