using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class PeriodicAsyncActor<T> : PeriodicAsyncActor<T, bool> // this hierarchy feels a little backward
	{

		public PeriodicAsyncActor(Action<T> action, TimeSpan period)
			: base((t, c, f, l) => { action.Invoke(t); return true; }, period)
		{
		}

		public PeriodicAsyncActor(Action<T, CancellationToken> action, TimeSpan period)
			: base((t, c, f, l) => { action.Invoke(t, c); return true; }, period)
		{
		}

		public PeriodicAsyncActor(SetAction<T> action, TimeSpan period)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; }, period)
		{
		}
	}

	public class PeriodicAsyncActor<T, R> : IActor<T, R>, IDisposable
	{
		protected readonly ConcurrentQueue<TaskCompletionSource<R>> _queue = new ConcurrentQueue<TaskCompletionSource<R>>();
		private readonly CancellationTokenSource _exitSource = new CancellationTokenSource();
		private readonly Task _periodicTask; // just here to keep it in scope

		public PeriodicAsyncActor(Func<T, R> action, TimeSpan period)
			: this((t, c, f, l) => action.Invoke(t), period)
		{
		}

		public PeriodicAsyncActor(Func<T, CancellationToken, R> action, TimeSpan period)
			: this((t, c, f, l) => action.Invoke(t, c), period)
		{
		}

		public PeriodicAsyncActor(SetFunc<T, R> action, TimeSpan period, Action<TimeSpan> onOverrun = null)
		{
			_periodicTask = new Task(() => // using Task because Thread is not available in the present .NET Standard
			{
				var sw = new Stopwatch();
				while (!_exitSource.IsCancellationRequested)
				{
					// TODO: name this thread once we're using .NETStandard 2.0 (by checking to see if it is a threadpool thread or not)
					sw.Restart();
					var isFirst = true;
					while (_queue.TryDequeue(out var source))
					{
						var tuple = (Tuple<T, CancellationToken>)source.Task.AsyncState;
						var value = tuple.Item1;
						var token = tuple.Item2;
						if (token.IsCancellationRequested)
						{
							source.SetCanceled();
						}
						else
						{
							var empty = _queue.IsEmpty;
							var result = action.Invoke(value, token, isFirst, empty);
							var tret = result as Task;
							tret?.Wait(); // we can't move on until this one is done or we might get out of order
							isFirst = empty;
							source.SetResult(result);
						}
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
						Task.Delay(period - sw.Elapsed).Wait(_exitSource.Token);
					}
					catch (TaskCanceledException) { }
				}
			}, TaskCreationOptions.LongRunning);
			_periodicTask.Start();
		}

		public void Dispose()
		{
			_exitSource.Cancel();
		}

		public int ScheduledTasksCount => _queue.Count;

		public Task<R> Push(T value)
		{
			return Push(value, CancellationToken.None);
		}

		public Task<R[]> PushMany(IReadOnlyList<T> values)
		{
			return PushMany(values, CancellationToken.None);
		}

		public Task<R> Push(T value, CancellationToken token)
		{
			var source = new TaskCompletionSource<R>(Tuple.Create(value, token));
			_queue.Enqueue(source);
			return source.Task;
		}

		public Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			var tasks = new List<Task<R>>();
			foreach (var value in values)
				tasks.Add(Push(value, token));
			return Task.WhenAll(tasks);
		}

		Task IActor<T>.Push(T value)
		{
			return Push(value);
		}

		Task IActor<T>.PushMany(IReadOnlyList<T> values)
		{
			return PushMany(values);
		}

		Task IActor<T>.Push(T value, CancellationToken token)
		{
			return Push(value, token);
		}

		Task IActor<T>.PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			return PushMany(values, token);
		}
	}
}