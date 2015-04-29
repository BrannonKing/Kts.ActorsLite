using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public interface IActor<T>
	{
		Task Push(T value);
		Task Push(IEnumerable<T> values);
		Task Push(T value, CancellationToken token);
		Task Push(IEnumerable<T> values, CancellationToken token);
	}

	public interface IActor<T, R>: IActor<T>
	{
		new Task<R> Push(T value);
		new Task<R[]> Push(IEnumerable<T> values);
		new Task<R> Push(T value, CancellationToken token);
		new Task<R[]> Push(IEnumerable<T> values, CancellationToken token);
	}
}
