using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
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
	}

	public class MostRecentAsyncActor<T, R> : IActor<T, R>
	{
		private readonly Func<T, CancellationToken, R> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly object _lock = new object();

		public MostRecentAsyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public MostRecentAsyncActor(Func<T, CancellationToken, R> action)
		{
			_action = action;
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
			Task<R> task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev =>
				{
					bool shouldRun = false;
					lock (_lock)
						shouldRun = prev == _previous;
					if (shouldRun && !token.IsCancellationRequested)
						return _action.Invoke(value, token);
					return default(R);
				}, token);
				_previous = task;
			}
			return task;
		}

		public async Task<R[]> Push(IReadOnlyList<T> values, CancellationToken token)
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
