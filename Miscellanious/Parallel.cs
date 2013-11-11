using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LoggingMonkey {
	static class Parallel {
		public static void Do( Queue<Action> queue ) {
			int ts = 8;
			var threads = new Thread[ts];
			ThreadStart worker = () => {
				for (;;) {
					Action work;
					lock (queue) {
						if ( queue.Count==0 ) return;
						work = queue.Dequeue();
					}
					work();
				}
			};
			for ( int i=0; i<ts; ++i ) {
				threads[i] = new Thread(worker);
				threads[i].Start();
			}
			for ( int i=0; i<ts; ++i ) {
				threads[i].Join();
			}
		}

		public static void For( int start, int end, Action<int> loop ) {
			var queue = new Queue<Action>( Enumerable.Range(start,end-start).Select( i => new Action(() => loop(i)) ) );
			Do(queue);
		}
	}
}
