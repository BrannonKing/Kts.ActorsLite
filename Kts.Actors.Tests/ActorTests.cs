using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Kts.Actors.Tests
{
	public class ActorTests
	{
		private readonly ITestOutputHelper _output;

		public ActorTests(ITestOutputHelper output)
		{
			_output = output;
		}

		// needed tests:
		// 1. make sure a lot of tasks all get done
		// 2. make sure an exception on 1, 3, or 5 of 5 still lets the others run
		// 3. cancelling before it runs or during a run should return cancelled tasks
		// 4. make sure the four that should keep them in order do
		// 5. make sure each actor can handle multiple threads pushing to it

		private IEnumerable<IActor<T, R>> CreateActors<T, R>(Func<T, CancellationToken, R> func)
		{
			yield return new OrderedSyncActor<T, R>(func);
			yield return new OrderedAsyncActor<T, R>(func);
			yield return new UnorderedAsyncActor<T, R>(func);
			yield return new PeriodicAsyncActor<T, R>(func, 20);
			yield return new MostRecentAsyncActor<T, R>(func);
		}

		[Fact]
		public void AllGetDone()
		{
			var rand = new Random(42);
			const int multiplier = 20, cnt = 1000;
			foreach (var actor in CreateActors<int, int>((x, t) => x * multiplier))
			{
				var tasks = new List<Task<int>>(cnt);
				for (int i = 0; i < cnt; i++)
				{
					tasks.Add(actor.Push(i + 1));
				}
				Task.WhenAll(tasks).Wait();

				if (actor is MostRecentAsyncActor<int, int>)
				{
					Assert.Equal(cnt * multiplier, tasks.Last().Result);
					Assert.True(tasks.Last().IsCompleted);
					Assert.False(tasks.Last().IsFaulted);
					Assert.False(tasks.Last().IsCanceled);
				}
				else
				{
					// they should all be done
					Assert.True(tasks.All(t => t.IsCompleted && !t.IsFaulted && !t.IsCanceled));

					var nums = tasks.Select(t => t.Result).ToList();
					var ordered = nums.OrderBy(x => x).ToList();
					if (!(actor is UnorderedAsyncActor<int, int>))
					{
						// they should all be in order
						Assert.Equal(ordered, nums);
					}
					else
					{
						for (int i = 0; i < cnt; i++)
						{
							Assert.Equal((i + 1) * multiplier, ordered[i]);
						}
					}
				}


			}
		}

		[Fact]
		public void TestMiddleSkipped()
		{
			var hits = new bool[3];

			Action<int, CancellationToken> t = (i, tok) =>
			{
				hits[i] = true;
				Thread.Sleep(20);
			};

			var queue = new MostRecentAsyncActor<int>(t);
			queue.Push(0);
			queue.Push(1);
			queue.Push(2).Wait();

			// the hitFirst value is arbitrary (based on timing) and not particularly related to the goal
			Assert.False(hits[1]);
			Assert.True(hits[2]);
		}

		[Fact]
		public void TestRobustness()
		{
			var rand = new Random(42);
			int lastAction = -1, lastAction2 = 42;
			Action<int, CancellationToken> t = (r, tok) =>
			{
				lastAction = r;
				Thread.Sleep(rand.Next(100));
			};
			var queue = new MostRecentAsyncActor<int>(t);
			for (var i = 0; i < 1000; i++)
			{
				var r = rand.Next();
				lastAction2 = r;

				var task = queue.Push(r);
				if (i == 999)
					task.Wait();
				else
					Thread.Sleep(rand.Next(150));
			}

			Assert.Equal(lastAction, lastAction2);
		}

		[Fact]
		public void TestMostRecentSubAdd()
		{
			// queue some tasks that take 200ms 100ms apart
			var hits = new bool[10];
			Action<int, CancellationToken> t = (r, tok) =>
			{
				if (!tok.IsCancellationRequested)
				{
					hits[r] = true;
					Thread.Sleep(100);
				}
			};

			var queue = new MostRecentAsyncActor<int>(t);
			Task last = null;
			for (var i = 0; i < hits.Length; i++)
			{
				last = queue.Push(i);
				Thread.Sleep(45);
			}
			if (last != null)
				last.Wait();

			Assert.True(hits.Last());
		}

		class ExampleClass
		{
			private struct CriticalParams
			{
				public int Key;
				public string Value;
			}

			public ExampleClass()
			{
				_criticalMethod = new MostRecentAsyncActor<CriticalParams>(cp => 
				{
					Console.WriteLine(cp.Key + " = " + cp.Value);
				});
			}

			private readonly MostRecentAsyncActor<CriticalParams> _criticalMethod;

			public void PrintAndSkipSomeIfTheyComeToFast(int key, string value)
			{
				_criticalMethod.Push(new CriticalParams { Key = key, Value = value });
			}
		}
	}
}
