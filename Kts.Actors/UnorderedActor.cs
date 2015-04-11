using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class UnorderedActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		public UnorderedActor(Action<T> action)
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

	public class UnorderedActor<T, R> : UnorderedActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public UnorderedActor(Func<T, R> action)
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

		public async Task Push(T value, IActor<R> next)
		{
			var result = await Push(value);
			if (next != null)
				await next.Push(result);
		}
		
		public async Task<R2> Push<R2>(T value, IActor<R, R2> next)
		{
			var result = await Push(value);
			return await next.Push(result);
		}
	}
}
