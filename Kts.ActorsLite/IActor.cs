﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public interface IActor<T>
	{
		Task Push(T value);
		Task PushMany(IReadOnlyList<T> values);
		Task Push(T value, CancellationToken token);
		Task PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

	public interface IActor<T, R>: IActor<T>
	{
		new Task<R> Push(T value);
		new Task<R[]> PushMany(IReadOnlyList<T> values);
		new Task<R> Push(T value, CancellationToken token);
		new Task<R[]> PushMany(IReadOnlyList<T> values, CancellationToken token);
	}

	public enum ActorType { MostRecentAsync, OrderedAsync, OrderedSync, PeriodicAsync, UnorderedAsync }
	public interface IActorFactory
	{
		IActor<T> Create<T>(ActorType type, Action<T> action, int periodMs = -1);
		IActor<T, R> Create<T, R>(ActorType type, Func<T, R> action, int periodMs = -1);
	}

	public interface ITaskSchedulerFactory
	{
		TaskScheduler Create(ActorType type, int periodMs = -1);
	}
}