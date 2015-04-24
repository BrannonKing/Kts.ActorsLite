using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class UnorderedAsyncActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		public UnorderedAsyncActor(Action<T> action)
		{
			_action = action;
		}

		public async Task Push(T value)
		{
			await Task.Run(() => _action.Invoke(value));
		}

		public async Task Push(IEnumerable<T> values)
		{
			await Task.WhenAll(values.Select(v => Push(v)));
		}
	}

	public class UnorderedAsyncActor<T, R> : UnorderedAsyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public UnorderedAsyncActor(Func<T, R> action)
			: base(t => action.Invoke(t))
		{
			_action = action;
		}

		new public async Task<R> Push(T value)
		{
			return await Task.Run(() => _action.Invoke(value));
		}

		new public async Task<R[]> Push(IEnumerable<T> values)
		{
			return await Task.WhenAll(values.Select(v => Push(v)));
		}
	}
}
