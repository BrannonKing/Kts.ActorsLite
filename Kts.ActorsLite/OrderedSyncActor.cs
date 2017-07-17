using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	/// <summary>
	/// Executes the method immediately (on the caller thread) upon any push call.
	/// </summary>
	public class OrderedSyncActor<T> : OrderedSyncActor<T, bool>
	{
		public OrderedSyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedSyncActor(Action<T, CancellationToken> action)
			: base((t, c) => { action.Invoke(t, c); return true; })
		{
		}

		public OrderedSyncActor(SetAction<T> action)
			: base((t, c, f, l) => { action.Invoke(t, c, f, l); return true; })
		{
		}
	}

	public class OrderedSyncActor<T, R> : IActor<T, R>
	{
		private readonly SetFunc<T, R> _action;
		public OrderedSyncActor(Func<T, R> action)
			: this((t, c, f, l) => action.Invoke(t))
		{
		}

		public OrderedSyncActor(Func<T, CancellationToken, R> action)
			: this((t, c, f, l) => action.Invoke(t, c))
		{
		}

		public OrderedSyncActor(SetFunc<T, R> action)
		{
			_action = action;
		}

		public Task<R> Push(T value)
		{
			return Task.FromResult(_action.Invoke(value, CancellationToken.None, true, true));
		}

		public Task<R[]> PushMany(IReadOnlyList<T> values)
		{
			var rs = new List<R>();
			for (var i = 0; i < values.Count; i++)
				rs.Add(_action.Invoke(values[i], CancellationToken.None, i == 0, i == values.Count - 1));
			return Task.FromResult(rs.ToArray());
		}


		public Task<R> Push(T value, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				var ret = _action.Invoke(value, token, true, true);
				var tret = ret as Task;
				tret?.ConfigureAwait(false).GetAwaiter().GetResult(); // we can't move on until this one is done or we might get out of order
				return Task.FromResult(ret); // looks like you'll have to call Unwrap on the outside because C# won't let me "is Task<R>" here
			}
			return Task.FromCanceled<R>(token);
		}

		public Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				var rs = new List<R>();
				for (var i = 0; i < values.Count; i++)
				{
					try
					{
						rs.Add(_action.Invoke(values[i], token, i == 0, i == values.Count - 1));
					}
					catch (OperationCanceledException) { }
					if (token.IsCancellationRequested)
						return Task.Run(() => rs.ToArray(), token);
				}
				return Task.FromResult(rs.ToArray());
			}
			return Task.FromCanceled<R[]>(token);
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
