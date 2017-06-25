using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
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

		public UnorderedAsyncActor(SetAction<T> action)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; })
		{
		}
	}

	public class UnorderedAsyncActor<T, R> : IActor<T, R>
	{
		private readonly SetFunc<T, R> _action;
		public UnorderedAsyncActor(Func<T, R> action)
			: this((t, c, f, l) => action.Invoke(t))
		{
		}

		public UnorderedAsyncActor(Func<T, CancellationToken, R> action)
			: this((t, c, f, l) => action.Invoke(t, c))
		{
		}

		public UnorderedAsyncActor(SetFunc<T, R> action)
		{
			_action = action;
		}
		
		public async Task<R> Push(T value)
		{
			return await Task.Run(() => _action.Invoke(value, CancellationToken.None, true, true));
		}

		public async Task<R[]> PushMany(IReadOnlyList<T> values)
		{
			return await Task.WhenAll(values.Select(v => Push(v)));
		}

		public async Task<R> Push(T value, CancellationToken token)
		{
			return await Task.Run(() => _action.Invoke(value, token, true, true), token); // not sure we want that last token passed to Task.Run or not
		}

		public async Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			return await Task.WhenAll(values.Select(v => Push(v, token)));
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
