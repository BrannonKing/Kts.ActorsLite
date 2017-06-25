using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
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
			: base((t, c, f, l) => { action.Invoke(t, c); return true; })
		{
		}

		public OrderedAsyncActor(SetAction<T> action)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; })
		{
		}
	}

	public class OrderedAsyncActor<T, R> : IActor<T, R>
	{
		private readonly SetFunc<T, R> _action;
		private Task _previous = Task.FromResult(true);
		private readonly object _lock = new object();
		public OrderedAsyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedAsyncActor(Func<T, CancellationToken, R> action)
			: this((t, c, f, l) => action.Invoke(t, c))
		{
		}

		public OrderedAsyncActor(SetFunc<T, R> action)
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

		public Task<R> Push(T value, CancellationToken token)
		{
			Task<R> task = null;
			lock (_lock)
			{
				var isFirst = _previous.IsCompleted;
				Func<bool> isLast = () => { lock (_lock) return _previous == task; }; // hoping for by-ref closure on task here

				task = _previous.ContinueWith(prev =>
				{
					var ret = _action.Invoke(value, token, isFirst, isLast.Invoke());
					return ret;

				}, TaskContinuationOptions.PreferFairness);
				_previous = task;
			}
			return task;
		}

		public async Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			var results = new List<Task<R>>(values.Count);
			foreach (var value in values)
			{
				if (token.IsCancellationRequested)
					break;
				results.Add(Push(value, token));
			}
			await Task.WhenAll(results);
			return results.Select(r => r.Result).ToArray();
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
