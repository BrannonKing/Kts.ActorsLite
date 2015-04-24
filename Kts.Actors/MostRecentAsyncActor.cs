using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class MostRecentAsyncActor<T> : IActor<T>
	{
		private readonly Action<T> _action;
		protected Task _previous = Task.FromResult(true);
		protected readonly object _lock = new object();
		public MostRecentAsyncActor(Action<T> action)
		{
			_action = action;
		}

		public async Task Push(T value)
		{
			Task task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev =>
				{
					bool shouldRun = false;
					lock (_lock)
						shouldRun = _previous == prev;
					if (shouldRun)
						_action.Invoke(value);
				});
				_previous = task;
			}
			await task;
		}

		public async Task Push(IEnumerable<T> values)
		{
			if (values.Any())
				await Push(values.Last());
		}
	}

	public class MostRecentAsyncActor<T, R> : MostRecentAsyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, R> _action;
		public MostRecentAsyncActor(Func<T, R> action)
			: base(t => action.Invoke(t))
		{
			_action = action;
		}

		new public async Task<R> Push(T value)
		{
			Task<R> task;
			lock (_lock)
			{
				task = _previous.ContinueWith(prev =>
				{
					bool shouldRun = false;
					lock (_lock)
						shouldRun = _previous == prev;
					if (shouldRun)
						return _action.Invoke(value);
					return default(R);
				});
				_previous = task;
			}
			return await task;
		}

		new public async Task<R[]> Push(IEnumerable<T> values)
		{
			if (values.Any())
				return new[] { await Push(values.Last()) };
			return new R[0];
		}
	}
}
