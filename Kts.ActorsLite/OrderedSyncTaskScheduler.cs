using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class OrderedSyncTaskScheduler: TaskScheduler
	{
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return new Task[0];
		}

		public override int MaximumConcurrencyLevel => 1;

		protected override void QueueTask(Task task)
		{
			TryExecuteTask(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			throw new InvalidOperationException("Not expecting to get here with this implementation.");
		}

		protected override bool TryDequeue(Task task)
		{
			return false;
		}
	}
}
