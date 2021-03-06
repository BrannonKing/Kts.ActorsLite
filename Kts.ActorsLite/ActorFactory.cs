﻿using System;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class ActorFactory : IActorFactory, ITaskSchedulerFactory
	{
		public IActor<T> Create<T>(ActorType type, SetAction<T> action, TimeSpan? period = null)
		{
			switch (type)
			{
				case ActorType.MostRecentAsync:
					return new MostRecentAsyncActor<T>(action);
				case ActorType.OrderedAsync:
					return new OrderedAsyncActor<T>(action);
				case ActorType.OrderedSync:
					return new OrderedSyncActor<T>(action);
				case ActorType.PeriodicAsync:
					return new PeriodicAsyncActor<T>(action, period.GetValueOrDefault(TimeSpan.FromSeconds(0.1)));
				case ActorType.UnorderedAsync:
					return new UnorderedAsyncActor<T>(action);
				default:
					throw new NotSupportedException();
			}
		}

		public IActor<T, R> Create<T, R>(ActorType type, SetFunc<T, R> action, TimeSpan? period = null)
		{
			switch (type)
			{
				case ActorType.MostRecentAsync:
					return new MostRecentAsyncActor<T, R>(action);
				case ActorType.OrderedAsync:
					return new OrderedAsyncActor<T, R>(action);
				case ActorType.OrderedSync:
					return new OrderedSyncActor<T, R>(action);
				case ActorType.PeriodicAsync:
					return new PeriodicAsyncActor<T, R>(action, period.GetValueOrDefault(TimeSpan.FromSeconds(0.1)));
				case ActorType.UnorderedAsync:
					return new UnorderedAsyncActor<T, R>(action);
				default:
					throw new NotSupportedException();
			}
		}

		public TaskScheduler Create(ActorType type, TimeSpan? period = null)
		{
			switch (type)
			{
				case ActorType.MostRecentAsync:
					return new MostRecentAsyncTaskScheduler();
				case ActorType.OrderedAsync:
					return new OrderedAsyncTaskScheduler();
				case ActorType.OrderedSync:
					return new OrderedSyncTaskScheduler();
				case ActorType.PeriodicAsync:
					return new PeriodicAsyncTaskScheduler(period.GetValueOrDefault(TimeSpan.FromSeconds(0.1)));
				case ActorType.UnorderedAsync:
					return TaskScheduler.Default;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
