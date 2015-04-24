using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kts.Actors
{
	public interface IActor<T>
	{
		Task Push(T value);
		Task Push(IEnumerable<T> values);
	}

	public interface IActor<T, R>: IActor<T>
	{
		new Task<R> Push(T value);
		new Task<R[]> Push(IEnumerable<T> values);
	}
}
