using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	/// <summary>
	/// Executes on the primary thread pool. If a task is already executing, the incoming request is queued on the back of that.
	/// </summary>
	public class OrderedAsyncActor<T> : OrderedAsyncActor<T, bool>
	{
		public OrderedAsyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedAsyncActor(Action<T, CancellationToken> action)
			: base((t, c) => { action.Invoke(t, c); return true; })
		{
		}
	}

	public class OrderedAsyncActor<T, R> : IActor<T, R>
	{
		private readonly Func<T, CancellationToken, R> _action;
		private Task _previous = Task.FromResult(true);
		private readonly object _lock = new object();
		public OrderedAsyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedAsyncActor(Func<T, CancellationToken, R> action)
		{
			_action = action;
		}

		public Task<R> Push(T value)
		{
			return Push(value, CancellationToken.None);
		}

		public Task<R[]> Push(IEnumerable<T> values)
		{
			return Push(values, CancellationToken.None);
		}

		public Task<R> Push(T value, CancellationToken token)
		{
			Task<R> task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev => _action.Invoke(value, token), token);
				_previous = task;
			}
			return task;
		}

		public async Task<R[]> Push(IEnumerable<T> values, CancellationToken token)
		{
			var results = new List<R>();
			foreach (var value in values)
			{
				if (token.IsCancellationRequested)
					break;
				results.Add(await Push(value, token));
			}
			return results.ToArray();
		}

		Task IActor<T>.Push(T value)
		{
			return Push(value);
		}

		Task IActor<T>.Push(IEnumerable<T> values)
		{
			return Push(values);
		}

		Task IActor<T>.Push(T value, CancellationToken token)
		{
			return Push(value, token);
		}

		Task IActor<T>.Push(IEnumerable<T> values, CancellationToken token)
		{
			return Push(values, token);
		}
	}
}
