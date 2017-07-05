using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class OrderedAsyncTaskScheduler: TaskScheduler
	{
		private readonly ConcurrentQueue<Task> _queue = new ConcurrentQueue<Task>();
		private readonly MostRecentAsyncActor<object> _processor;
		public OrderedAsyncTaskScheduler()
		{
			_processor = new MostRecentAsyncActor<object>(o => Process());
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return _queue;
		}

		public int ScheduledTasksCount => _queue.Count;

		public override int MaximumConcurrencyLevel => 1;

		private void Process()
		{
			while (_queue.TryDequeue(out Task task))
				TryExecuteTask(task);
		}

		protected override void QueueTask(Task task)
		{
			_queue.Enqueue(task);
			_processor.Push(null);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override bool TryDequeue(Task task)
		{
			return false; // ConcurrentQueue doesn't lend itself to arbitrary removal
		}
	}
}
