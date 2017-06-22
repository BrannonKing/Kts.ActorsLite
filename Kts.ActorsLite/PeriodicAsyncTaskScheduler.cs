using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kts.ActorsLite
{
	public class PeriodicAsyncTaskScheduler: TaskScheduler, IDisposable
	{
		private readonly List<Task> _tasks = new List<Task>();
		private readonly PeriodicAsyncActor<object> _processor;
		public PeriodicAsyncTaskScheduler(int periodMs)
		{
			_processor = new PeriodicAsyncActor<object>(o => Process(), periodMs);
			_processor.Push(null);
		}

		protected override IEnumerable<Task> GetScheduledTasks()
		{
			lock(_tasks)
				return _tasks.ToArray();
		}

		public override int MaximumConcurrencyLevel => 1;

		private void Process()
		{
			foreach (var task in GetScheduledTasks())
				TryExecuteTask(task);
		}

		protected override void QueueTask(Task task)
		{
			lock (_tasks)
				_tasks.Add(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		protected override bool TryDequeue(Task task)
		{
			lock (_tasks)
				return _tasks.Remove(task);
		}

		public void Dispose()
		{
			_processor.Dispose();
		}
	}
}
