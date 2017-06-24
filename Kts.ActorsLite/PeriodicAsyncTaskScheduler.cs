using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class PeriodicAsyncTaskScheduler: TaskScheduler, IDisposable
	{
		private readonly ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
		private readonly Timer _timer;
		private readonly Action _onOverrun;

		public PeriodicAsyncTaskScheduler(int periodMs, Action onOverrun = null)
		{
			// the threading timer uses the thread pool
			// its callback method is re-entrant in the case of an overrun
			// we can't have that here; we always want them to execute in order
			// we need to warn the user obout overruns somehow
			// biggest issue: Tasks are one-shots

			_onOverrun = onOverrun;
			_timer = new Timer(Callback, null, 1, periodMs);
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _tasks;
		}

		public override int MaximumConcurrencyLevel => 1;

		private readonly object _overrunLock = new object();
		private void Callback(object state)
		{
			if (!Monitor.TryEnter(_overrunLock))
			{
				System.Diagnostics.Debug.WriteLine("PeriodicAsyncTaskScheduler: unable to complete all tasks in the alloted time period.");
				if (_onOverrun != null)
					_onOverrun.Invoke();
			}
			else
			{
				try
				{
					while (_tasks.TryDequeue(out Task task))
						TryExecuteTask(task);
				}
				finally
				{
					Monitor.Exit(_overrunLock);
				}
			}
		}

		protected override void QueueTask(Task task)
		{
			_tasks.Enqueue(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override bool TryDequeue(Task task)
		{
			return false;
		}

		public void Dispose()
		{
			_timer.Dispose();
		}
	}
}
