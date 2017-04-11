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
	public class SchedulerTests
	{
		private readonly ITestOutputHelper _output;

		public SchedulerTests(ITestOutputHelper output)
		{
			_output = output;
		}

		[Fact]
		public void TestMostRecentScheduler()
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

			var scheduler = new MostRecentTaskScheduler();
			Task last = null;
			for (var i = 0; i < hits.Length; i++)
			{
				var local = i;
				last = new Task(() => t.Invoke(local, CancellationToken.None));
				last.Start(scheduler);
				Thread.Sleep(45);
			}
			if (last != null)
				last.Wait();

			Assert.True(hits.Last());
		}
	}
}
