using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class PeriodicAsyncActor<T> : PeriodicAsyncActor<T, bool> // this hierarchy feels a little backward
	{

		public PeriodicAsyncActor(Action<T> action, int periodMs)
			: base((t, c, f, l) => { action.Invoke(t); return true; }, periodMs)
		{
		}

		public PeriodicAsyncActor(Action<T, CancellationToken> action, int periodMs)
			: base((t, c, f, l) => { action.Invoke(t, c); return true; }, periodMs)
		{
		}

		public PeriodicAsyncActor(SetAction<T> action, int periodMs)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; }, periodMs)
		{
		}
	}

	public class PeriodicAsyncActor<T, R> : IActor<T, R>, IDisposable
	{
		private readonly SetFunc<T, R> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly ConcurrentQueue<TaskCompletionSource<R>> _queue = new ConcurrentQueue<TaskCompletionSource<R>>();
		private readonly Timer _timer;
		private readonly Action _onOverrun;

		public PeriodicAsyncActor(Func<T, R> action, int periodMs)
			: this((t, c, f, l) => action.Invoke(t), periodMs)
		{
		}

		public PeriodicAsyncActor(Func<T, CancellationToken, R> action, int periodMs)
			: this((t, c, f, l) => action.Invoke(t, c), periodMs)
		{
		}

		public PeriodicAsyncActor(SetFunc<T, R> action, int periodMs, Action onOverrun = null)
		{
			_action = action;
			_onOverrun = onOverrun;
			_timer = new Timer(Callback, null, 1, periodMs);
		}

		private readonly object _overrunLock = new object();
		private void Callback(object state)
		{
			if (!Monitor.TryEnter(_overrunLock))
			{
				System.Diagnostics.Debug.WriteLine("PeriodicAsyncTaskScheduler: unable to complete all tasks in the alloted time period; skipping this run to allow them more time.");
				_onOverrun?.Invoke();
			}
			else
			{
				try
				{
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
							var result = _action.Invoke(value, token, isFirst, empty);
							var tret = result as Task;
							tret?.Wait(); // we can't move on until this one is done or we might get out of order
							isFirst = empty;
							source.SetResult(result);
						}
					}
				}
				finally
				{
					Monitor.Exit(_overrunLock);
				}
			}
		}

		public void Dispose()
		{
			_timer.Dispose();
		}

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