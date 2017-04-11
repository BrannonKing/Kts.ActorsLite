using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public class MostRecentTaskScheduler: TaskScheduler
	{
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			var task = _previous;
			if (!task.IsCompleted)
				return new[] {task};
			return new Task[0];
		}

		private Task _previous = Task.FromResult(true);
		private readonly object _lock = new object();
		private long _counter;
		protected override void QueueTask(Task task)
		{
			lock (_lock)
			{
				if (_previous == task) return;
				var local = ++_counter;
				var localTask = task;
				task = _previous.ContinueWith(prev =>
				{
					var shouldRun = local == _counter;
					if (shouldRun && !localTask.IsCanceled)
					{
						TryExecuteTask(localTask);
						localTask.Wait();
					}
				});
				_previous = task;
			}
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return false;
		}

		public override int MaximumConcurrencyLevel => 1;

		protected override bool TryDequeue(Task task)
		{
			lock (_lock)
			{
				if (task == _previous)
					++_counter; // this should technically make it skip to the next
			}
			return base.TryDequeue(task);
		}
	}
}
