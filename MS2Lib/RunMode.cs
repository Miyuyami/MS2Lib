namespace MS2Lib
{
    public enum RunMode
    {
        /// <summary>
        /// The writing will be in serial by running one task at a time.
        /// </summary>
        Sync = 0,
        /// <summary>
        /// The writing will use multiple tasks on most occassions at the expense of memory.
        /// </summary>
        Async = 1,
        /// <summary>
        /// The writing will use multiple taks everywhere possible at the expense of more memory than <see cref="Async"/>.
        /// </summary>
        Async2 = 2,
    }
}
