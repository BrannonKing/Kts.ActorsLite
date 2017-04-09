using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kts.Actors;

namespace Kts.Actors.Tests
{
	class GeneratorTests
	{
	}

	public class Tester
	{
		//[GenerateActorCode(ActorType.OrderedAsynchronous)]
		public Task<int> Add(int a, int b)
		{
			return Task.FromResult(a + b);
		}

		//[GenerateActorCode(ActorType.OrderedAsynchronous)]
		public Task<int> AddRun(int a, int b)
		{
			return Task.Run(() => a + b);
		}

		//[GenerateActorCode(ActorType.OrderedAsynchronous)]
		public async Task<int> AddAsync(int a, int b)
		{
			return a + b;
		}

		public Task<int> AddMethod(int a, int b)
		{
			return Task.FromResult(a + b);
		}

		public Task<int> AddMethodModified(int a, int b)
		{
			return AddHandler.Push((a, b));
		}

		private OrderedAsyncActor<(int, int), int> AddHandler
		{
			get => new OrderedAsyncActor<(int a, int b), int>((t, r) => AddMethod(t.a, t.b).Result);
		}
	}
}
