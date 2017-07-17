using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public delegate void SetAction<in T>(T value, CancellationToken token, bool isFirstInSet = false, bool isLastInSet = false);
	public delegate R SetFunc<in T, out R>(T value, CancellationToken token, bool isFirstInSet = false, bool isLastInSet = false);

	/// <typeparam name="T">The input data/message type.</typeparam>
	public interface IActor<in T>
	{
		/// <summary>
		/// Process this data value on this actor.
		/// </summary>
		Task Push(T value);
		/// <summary>
		/// Process these data values on this actor.
		/// </summary>
		Task PushMany(IReadOnlyList<T> values);
		/// <summary>
		/// Process this data value on this actor and optionally pull out early.
		/// </summary>
		Task Push(T value, CancellationToken token);
		/// <summary>
		/// Process these data values on this actor and optionally pull out early.
		/// </summary>
		Task PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

	/// <typeparam name="T">The input data/message type.</typeparam>
	/// <typeparam name="R">The output data/message type.</typeparam>
	public interface IActor<in T, R>: IActor<T>
	{
		new Task<R> Push(T value);
		new Task<R[]> PushMany(IReadOnlyList<T> values);
		new Task<R> Push(T value, CancellationToken token);
		new Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

	public enum ActorType { MostRecentAsync, OrderedAsync, OrderedSync, PeriodicAsync, UnorderedAsync }
	public interface IActorFactory
	{
		IActor<T> Create<T>(ActorType type, SetAction<T> action, TimeSpan? period = null);
		IActor<T, R> Create<T, R>(ActorType type, SetFunc<T, R> action, TimeSpan? period = null);
	}

	public interface ITaskSchedulerFactory
	{
		TaskScheduler Create(ActorType type, TimeSpan? period = null);
	}
}
