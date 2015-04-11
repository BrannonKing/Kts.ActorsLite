using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class SynchronousActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		public SynchronousActor(Action<T> action)
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

	public class SynchronousActor<T, R> : SynchronousActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public SynchronousActor(Func<T, R> action)
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

		public Task Push(T value, IActor<R> next)
		{
			var result = _action.Invoke(value);
			if (next != null)
				return next.Push(result);
			return Task.FromResult(result);
		}
		
		public Task<R2> Push<R2>(T value, IActor<R, R2> next)
		{
			return next.Push(_action.Invoke(value));
		}
	}
}
