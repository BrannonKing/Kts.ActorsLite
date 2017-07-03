using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public delegate void SetAction<in T>(T value, CancellationToken token, bool isFirstInSet = false, bool isLastInSet = false);
	public delegate R SetFunc<in T, out R>(T value, CancellationToken token, bool isFirstInSet = false, bool isLastInSet = false);

	public interface IActor<in T>
	{
		Task Push(T value);
		Task PushMany(IReadOnlyList<T> values);
		Task Push(T value, CancellationToken token);
		Task PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

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
		IActor<T> Create<T>(ActorType type, SetAction<T> action, int periodMs = -1);
		IActor<T, R> Create<T, R>(ActorType type, SetFunc<T, R> action, int periodMs = -1);
	}

	public interface ITaskSchedulerFactory
	{
		TaskScheduler Create(ActorType type, int periodMs = -1);
	}
}
