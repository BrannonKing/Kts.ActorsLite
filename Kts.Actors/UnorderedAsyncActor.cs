using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class UnorderedAsyncActor<T> : IActor<T>
	{
		private readonly Action<T, CancellationToken> _action;
		public UnorderedAsyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public UnorderedAsyncActor(Action<T, CancellationToken> action)
		{
			_action = action;
		}

		public async Task Push(T value)
		{
			await Task.Run(() => _action.Invoke(value, CancellationToken.None));
		}

		public async Task Push(IEnumerable<T> values)
		{
			await Task.WhenAll(values.Select(v => Push(v)));
		}


		public async Task Push(T value, CancellationToken token)
		{
			await Task.Run(() => _action.Invoke(value, token), token);
		}

		public async Task Push(IEnumerable<T> values, CancellationToken token)
		{
			await Task.WhenAll(values.Select(v => Push(v, token)));
		}
	}

	public class UnorderedAsyncActor<T, R> : UnorderedAsyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, CancellationToken, R> _action;
		public UnorderedAsyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public UnorderedAsyncActor(Func<T, CancellationToken, R> action)
			: base((t, c) => action.Invoke(t, c))
		{
			_action = action;
		}
		
		new public async Task<R> Push(T value)
		{
			return await Task.Run(() => _action.Invoke(value, CancellationToken.None));
		}

		new public async Task<R[]> Push(IEnumerable<T> values)
		{
			return await Task.WhenAll(values.Select(v => Push(v)));
		}

		new public async Task<R> Push(T value, CancellationToken token)
		{
			return await Task.Run(() => _action.Invoke(value, token), token);
		}

		new public async Task<R[]> Push(IEnumerable<T> values, CancellationToken token)
		{
			return await Task.WhenAll(values.Select(v => Push(v, token)));
		}
	}
}
