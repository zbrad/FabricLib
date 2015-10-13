using System;
using System.Threading.Tasks;

namespace ZBrad.FabLibs.Utilities
{
    /// <summary>
    /// task utilities
    /// </summary>
    public static class TaskUtility
    {
        static Task<bool> isCompletedTrue = Task.FromResult<bool>(true);
        static Task<bool> isCompletedFalse = Task.FromResult<bool>(false);
        
        /// <summary>
        /// returns a constant task completion with true result
        /// </summary>
        /// <value>task with true result</value>
        public static Task<bool> IsCompletedTrue { get { return isCompletedTrue; } }

        /// <summary>
        /// returns a constant task completion with false result
        /// </summary>
        /// <value>task with false result</value>
        public static Task<bool> IsCompletedFalse { get { return isCompletedFalse; } }

        /// <summary>
        /// execute a function synchronously
        /// </summary>
        /// <typeparam name="TResult">type of task result</typeparam>
        /// <param name="func">function reference</param>
        /// <returns>completed task with a typed result</returns>
        public static Task<TResult> ExecuteSynchronously<TResult>(Func<TResult> func)
        {
            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();

            try
            {
                tcs.SetResult(func());
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }

            return tcs.Task;
        }

        /// <summary>
        /// execute an action synchronously
        /// </summary>
        /// <param name="action">action reference</param>
        /// <returns>completed task</returns>
        public static Task ExecuteSynchronously(Action action)
        {
            return TaskUtility.ExecuteSynchronously<object>(() =>
            {
                action();
                return null;
            });
        }
    }
}
