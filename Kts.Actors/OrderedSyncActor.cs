using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class OrderedSyncActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		public OrderedSyncActor(Action<T> action)
		{
			_action = action;
		}

		public Task Push(T value)
		{
			_action.Invoke(value);
			return Task.FromResult(true);
		}

		public Task Push(IEnumerable<T> values)
		{
			foreach (var value in values)
				_action.Invoke(value);
			return Task.FromResult(true);
		}
	}

	public class OrderedSyncActor<T, R> : OrderedSyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public OrderedSyncActor(Func<T, R> action)
			: base(t => action.Invoke(t))
		{
			_action = action;
		}

		new public Task<R> Push(T value)
		{
			return Task.FromResult(_action.Invoke(value));
		}

		new public Task<R[]> Push(IEnumerable<T> values)
		{
			var rs = new List<R>();
			foreach (var value in values)
				rs.Add(_action.Invoke(value));
			return Task.FromResult(rs.ToArray());
		}
	}
}
