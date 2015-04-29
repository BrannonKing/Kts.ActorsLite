﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class OrderedSyncActor<T> : IActor<T>
	{
		private readonly Action<T, CancellationToken> _action;
		public OrderedSyncActor(Action<T> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedSyncActor(Action<T, CancellationToken> action)
		{
			_action = action;
		}

		public Task Push(T value)
		{
			_action.Invoke(value, CancellationToken.None);
			return Task.FromResult(true);
		}

		public Task Push(IEnumerable<T> values)
		{
			// should do this with parallel foreach?
			foreach (var value in values)
				_action.Invoke(value, CancellationToken.None);
			return Task.FromResult(true);
		}


		public Task Push(T value, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				try
				{
					_action.Invoke(value, token);
				}
				catch (OperationCanceledException) { }
				if (token.IsCancellationRequested)
					return Task.Run(() => { }, token);
				return Task.FromResult(true);
			}
			else
				return Task.Run(() => { }, token);
		}

		public Task Push(IEnumerable<T> values, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				foreach (var value in values)
				{
					try
					{
						_action.Invoke(value, token);
					}
					catch (OperationCanceledException) { }
					if (token.IsCancellationRequested)
						return Task.Run(() => { }, token);
				}
				return Task.FromResult(true);
			}
			else
				return Task.Run(() => { }, token);
		}
	}

	public class OrderedSyncActor<T, R> : OrderedSyncActor<T>, IActor<T, R>
	{
		private readonly Func<T, CancellationToken, R> _action;
		public OrderedSyncActor(Func<T, R> action)
			: this((t, c) => action.Invoke(t))
		{
		}

		public OrderedSyncActor(Func<T, CancellationToken, R> action)
			: base((t, c) => action.Invoke(t, c))
		{
			_action = action;
		}

		new public Task<R> Push(T value)
		{
			return Task.FromResult(_action.Invoke(value, CancellationToken.None));
		}

		new public Task<R[]> Push(IEnumerable<T> values)
		{
			var rs = new List<R>();
			foreach (var value in values)
				rs.Add(_action.Invoke(value, CancellationToken.None));
			return Task.FromResult(rs.ToArray());
		}


		new public Task<R> Push(T value, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				var ret = default(R);
				try
				{
					ret = _action.Invoke(value, token);
				}
				catch (OperationCanceledException) { }
				if (token.IsCancellationRequested)
					return Task.Run(() => ret, token);
				return Task.FromResult(ret);
			}
			else
				return Task.Run(() => default(R), token);
		}

		new public Task<R[]> Push(IEnumerable<T> values, CancellationToken token)
		{
			if (!token.IsCancellationRequested)
			{
				var rs = new List<R>();
				foreach (var value in values)
				{
					try
					{
						rs.Add(_action.Invoke(value, token));
					}
					catch (OperationCanceledException) { }
					if (token.IsCancellationRequested)
						return Task.Run(() => rs.ToArray(), token);
				}
				return Task.FromResult(rs.ToArray());
			}
			else
				return Task.Run(() => new R[0], token);
		}
	}
}
