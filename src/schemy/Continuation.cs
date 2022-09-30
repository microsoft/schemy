using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
namespace Schemy
{
	class Continuation : Exception
	{
		object Value { get; set; }
		StackTrace Stack { get; set; }
		Thread Thread { get; set; }

		public static object CallWithCurrentContinuation(ICallable fc1)
		{
			var ccc = new Continuation { Stack = new StackTrace(), Thread = Thread.CurrentThread };
			try
			{
				var exitproc = NativeProcedure.Create<object, object>(v =>
						{
							var f1 = new StackTrace().GetFrames();
							var c1 = ccc.Stack.GetFrames();
							var offset = f1.Length - c1.Length;
							if (ccc.Thread == Thread.CurrentThread)
							{
								for (int i = c1.Length - 1; i >= 0; i--)
								{
									if (c1[i].GetMethod() != f1[i + offset].GetMethod())
									{
										throw new NotImplementedException("not supported, continuation called outside dynamic extent");
									}
								}
							}
							ccc.Value = v;
							throw ccc;
						});
				return fc1.Call(new List<object> { exitproc });
			}
			catch (Continuation c)
			{
				if (ccc == c)
				{
					return c.Value;
				}
				else
				{
					throw;
				}
			}
		}
	}
}
