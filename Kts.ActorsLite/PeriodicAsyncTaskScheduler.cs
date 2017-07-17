using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	/// <summary>
	/// Runs all the queued tasks when the timer elapses (which happens repeatedly).
	/// </summary>
	public class PeriodicAsyncTaskScheduler : TaskScheduler, IDisposable
	{
		private readonly ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();
		private readonly CancellationTokenSource _exitSource = new CancellationTokenSource();
		private readonly Task _periodicTask; // just here to keep it in scope

		public PeriodicAsyncTaskScheduler(TimeSpan period, Action<TimeSpan> onOverrun = null)
		{
			if (period < TimeSpan.FromMilliseconds(0.5)) // it actually seems to be 1ms on most Win10 systems
				throw new ArgumentException("The period is too small to be achievable. Use a SpinWait instead.");

			// the threading timer uses the thread pool
			// its callback method is re-entrant in the case of an overrun
			// we can't have that here; we always want them to execute in order
			// we also need to warn the user obout overruns somehow

			_periodicTask = new Task(() => // using Task because Thread is not available in the present .NET Standard
			{
				var sw = new Stopwatch();
				while (!_exitSource.IsCancellationRequested)
				{
					// TODO: name this thread once we're using .NETStandard 2.0 (by checking to see if it is a threadpool thread or not)
					sw.Restart();
					while (_queue.TryDequeue(out Task task))
					{
						TryExecuteTask(task);
					}
					sw.Stop();
					if (sw.Elapsed > period)
					{
						Debug.WriteLine("PeriodicAsyncTaskScheduler: unable to complete all tasks in the alloted time period of {0}. Took {1}", period, sw.Elapsed);
						onOverrun?.Invoke(sw.Elapsed);
						continue;
					}
					try
					{
						using (var slim = new ManualResetEventSlim(false))
							slim.Wait(period - sw.Elapsed, _exitSource.Token);
					}
					catch (OperationCanceledException) { }
				}
			}, TaskCreationOptions.LongRunning);
			_periodicTask.Start();
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _queue;
		}

		public override int MaximumConcurrencyLevel => 1;

		protected override void QueueTask(Task task)
		{
			_queue.Enqueue(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override bool TryDequeue(Task task)
		{
			// need this to be atomic:
			//if (_queue.TryPeek(out var head) && head == task)
			//{
			//	return _queue.TryDequeue(out head);
			//}
			return false;
		}

		public void Dispose()
		{
			_exitSource.Cancel();
		}

		public int ScheduledTasksCount => _queue.Count;
	}
}
