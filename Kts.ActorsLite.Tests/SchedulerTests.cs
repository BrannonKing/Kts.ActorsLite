using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Kts.ActorsLite;

namespace Kts.ActorsLite.Tests
{
	public class SchedulerTests
	{
		private readonly ITestOutputHelper _output;

		public SchedulerTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public async Task TestMostRecentAsyncTaskScheduler()
		{
			// queue some tasks that take 200ms 100ms apart
			var hits = new bool[10];
			Action<int, CancellationToken> t = (r, tok) =>
			{
				if (!tok.IsCancellationRequested)
				{
					hits[r] = true;
					Task.Delay(100).Wait();
				}
			};

			var scheduler = new MostRecentAsyncTaskScheduler();
			Task last = null;
			for (var i = 0; i < hits.Length; i++)
			{
				var local = i;
				last = new Task(() => t.Invoke(local, CancellationToken.None));
				last.Start(scheduler);
				await Task.Delay(45);
			}
			if (last != null)
				await last;

			Assert.True(hits.Last());
		}

		[Fact]
		public async Task TestOrderedAsyncScheduler()
		{
			// queue some tasks that take 200ms 100ms apart
			var hits = new int[50];
			Action<int, CancellationToken> t = (r, tok) =>
			{
				if (!tok.IsCancellationRequested)
				{
					hits[r] = r;
					Task.Delay(20).Wait();
				}
			};

			var scheduler = new OrderedAsyncTaskScheduler();
			Task last = null;
			for (var i = 0; i < hits.Length; i++)
			{
				var local = i;
				last = new Task(() => t.Invoke(local, CancellationToken.None));
				last.Start(scheduler);
			}
			if (last != null)
				await last;

			for (var i = 0; i < hits.Length; i++)
				Assert.Equal(i, hits[i]);
		}

		[Fact]
		public void TestOrderedSyncTaskScheduler()
		{
			int x = 0;
			var task = new Task(() => x++);
			var scheduler = new OrderedSyncTaskScheduler();
			task.Start(scheduler);
			Assert.True(task.IsCompleted);
			Assert.Equal(1, x);
		}

		[Fact]
		public async Task TestPeriodicAsyncTaskScheduler()
		{
			int x = 0;
			var scheduler = new PeriodicAsyncTaskScheduler(TimeSpan.FromMilliseconds(5));
			for (int i = 0; i < 10; i++)
				new Task(() => x++).Start(scheduler);

			await Task.Delay(15);
			Assert.Equal(10, x);
			scheduler.Dispose();
		}
	}
}
