using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class PeriodicAsyncActor<T> : PeriodicAsyncActor<T, bool> // this hierarchy feels a little backward
	{

		public PeriodicAsyncActor(Action<T> action, int periodMs)
			: this((t, c) => action.Invoke(t), periodMs)
		{
		}
		
		public PeriodicAsyncActor(Action<T, CancellationToken> action, int periodMs)
			: base((t, c) => { action.Invoke(t, c); return true; }, periodMs)
		{
		}
	}

	public class PeriodicAsyncActor<T, R> : IActor<T, R>, IDisposable
	{
		private readonly Func<T, CancellationToken, R> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly ConcurrentQueue<TaskCompletionSource<R>> _queue = new ConcurrentQueue<TaskCompletionSource<R>>();
		private readonly Timer _timer;

		public PeriodicAsyncActor(Func<T, R> action, int periodMs)
			: this((t, c) => action.Invoke(t), periodMs)
		{
		}

		public PeriodicAsyncActor(Func<T, CancellationToken, R> action, int periodMs)
		{
			_action = action;

			_timer = new Timer(Callback, null, 1, periodMs);
		}

		private void Callback(object state)
		{
			TaskCompletionSource<R> source;
			while (_queue.TryDequeue(out source))
			{
				var tuple = (Tuple<T, CancellationToken>)source.Task.AsyncState;
				var value = tuple.Item1;
				var token = tuple.Item2;
				if (token != null && token.IsCancellationRequested)
				{
					source.SetCanceled();
				}
				else
				{
					var result = _action.Invoke(value, token);
					source.SetResult(result);
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

		public Task<R[]> Push(IReadOnlyList<T> values)
		{
			return Push(values, CancellationToken.None);
		}

		public Task<R> Push(T value, CancellationToken token)
		{
			var source = new TaskCompletionSource<R>(Tuple.Create(value, token));
			_queue.Enqueue(source);
			return source.Task;
		}

		public Task<R[]> Push(IReadOnlyList<T> values, CancellationToken token)
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

		Task IActor<T>.Push(IReadOnlyList<T> values)
		{
			return Push(values);
		}

		Task IActor<T>.Push(T value, CancellationToken token)
		{
			return Push(value, token);
		}

		Task IActor<T>.Push(IReadOnlyList<T> values, CancellationToken token)
		{
			return Push(values, token);
		}
	}
}