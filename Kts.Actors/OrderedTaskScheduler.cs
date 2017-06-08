using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class OrderedTaskScheduler: TaskScheduler
	{
		private readonly ConcurrentQueue<Task> _tasks = new ConcurrentQueue<Task>();
		private readonly MostRecentAsyncActor<object> _processor;
		public OrderedTaskScheduler()
		{
			_processor = new MostRecentAsyncActor<object>(o => Process());
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _tasks;
		}

		public override int MaximumConcurrencyLevel => 1;

		private void Process()
		{
			while (_tasks.TryDequeue(out Task task))
				TryExecuteTask(task);
		}

		protected override void QueueTask(Task task)
		{
			_tasks.Enqueue(task);
			_processor.Push((object)null);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}
	}
}
