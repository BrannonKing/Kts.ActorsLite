using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	/// <summary>
	/// Executes on the primary thread pool as soon as possible.
	/// </summary>
	public class UnorderedAsyncActor<T> : UnorderedAsyncActor<T, bool>
	{
		public UnorderedAsyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public UnorderedAsyncActor(Action<T, CancellationToken> action)
			: base((t, c) => { action.Invoke(t, c); return true; })
		{
		}
	}

	public class UnorderedAsyncActor<T, R> : IActor<T, R>
	{
		private readonly Func<T, CancellationToken, R> _action;
		public UnorderedAsyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public UnorderedAsyncActor(Func<T, CancellationToken, R> action)
		{
			_action = action;
		}
		
		public async Task<R> Push(T value)
		{
			return await Task.Run(() => _action.Invoke(value, CancellationToken.None));
		}

		public async Task<R[]> Push(IReadOnlyList<T> values)
		{
			return await Task.WhenAll(values.Select(v => Push(v)));
		}

		public async Task<R> Push(T value, CancellationToken token)
		{
			return await Task.Run(() => _action.Invoke(value, token), token);
		}

		public async Task<R[]> Push(IReadOnlyList<T> values, CancellationToken token)
		{
			return await Task.WhenAll(values.Select(v => Push(v, token)));
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
