using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class OrderedAsyncActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly object _lock = new object();
		public OrderedAsyncActor(Action<T> action)
		{
			_action = action;
		}

		public async Task Push(T value)
		{
			Task task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev => _action.Invoke(value));
				_previous = task;
			}
			await task;
		}

		public async Task Push(IEnumerable<T> values)
		{
			var tasks = values.Select(v => Push(v));
			await Task.WhenAll(tasks);
		}
	}

	public class OrderedAsyncActor<T, R> : OrderedAsyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public OrderedAsyncActor(Func<T, R> action)
			: base(t => action.Invoke(t))
		{
			_action = action;
		}

		new public async Task<R> Push(T value)
		{
			Task<R> task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev => _action.Invoke(value));
				_previous = task;
			}
			return await task;
		}

		new public async Task<R[]> Push(IEnumerable<T> values)
		{
			var results = new List<R>();
			foreach (var value in values)
				results.Add(await Push(value));
			return results.ToArray();
		}
	}
}
