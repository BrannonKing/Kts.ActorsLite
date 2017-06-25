using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	/// <summary>
	/// Executes on the primary thread pool. If a current task is executing, any tasks queued to run after it will be replaced by the most recent request.
	/// </summary>
	public class MostRecentAsyncActor<T> : MostRecentAsyncActor<T, bool>, IActor<T>
	{
		public MostRecentAsyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public MostRecentAsyncActor(Action<T, CancellationToken> action)
			: base((t, c) => { action.Invoke(t, c); return true; })
		{
		}

		public MostRecentAsyncActor(SetAction<T> action)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; })
		{
		}
	}

	public class MostRecentAsyncActor<T, R> : IActor<T, R>
	{
		private readonly SetFunc<T, R> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly object _lock = new object();

		public MostRecentAsyncActor(Func<T, R> action)
			: this((t, c, f, l) => action.Invoke(t))
		{
		}

		public MostRecentAsyncActor(Func<T, CancellationToken, R> action)
			: this((t, c, f, l) => action.Invoke(t, c))
		{
		}

		public MostRecentAsyncActor(SetFunc<T, R> action)
		{
			_action = action;
		}
		
		public Task<R> Push(T value)
		{
			return Push(value, CancellationToken.None);
		}

		public Task<R[]> PushMany(IReadOnlyList<T> values)
		{
			return PushMany(values, CancellationToken.None);
		}

		private long _counter;
		public Task<R> Push(T value, CancellationToken token)
		{
			Task<R> task;
			lock (_lock)
			{
				var local = ++_counter;
				var isFirst = _previous.IsCompleted;
				task = _previous.ContinueWith(prev =>
				{
					var shouldRun = local == _counter;
					if (shouldRun && !token.IsCancellationRequested)
						return _action.Invoke(value, token, isFirst, true);
					return default(R);
				}, TaskContinuationOptions.PreferFairness); // don't pass the token in here so that all tasks succeed even if they don't do anything
				_previous = task;
			}
			return task;
		}

		public async Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			var r = await Push(values[values.Count - 1], token);
			var arr = new R[values.Count];
			arr[values.Count - 1] = r;
			return arr;
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
