﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ZeroMQ.Test
{
	static class ProgramRunner
	{

		static int Main(string[] args)
		{
			// HACK
			// Program.Start(args);
			// return 0;


			// REAL
			var fields = typeof(Program).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => field.Name);

			int leaveOut = 0;
			var dict = new Dictionary<string, string>();
			if (args != null && args.Length > 0)
			{
				foreach (string arg in args)
				{
					if (arg.StartsWith("--"))
					{
						leaveOut++;

						int iOfEquals = arg.IndexOf('=');
						string key, value;
						if (-1 < iOfEquals)
						{
							key = arg.Substring(0, iOfEquals);
							value = arg.Substring(iOfEquals + 1);
						}
						else {
							key = arg.Substring(0);
							value = null;
						}
						dict.Add(key, value);

						FieldInfo keyField = fields.Where(field => string.Equals(field.Name, key.Substring(2), StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
						if (keyField != null)
						{
							keyField.SetValue(null, value);
						}
					}
				}
			}

			int returnMain = 0;
			string command = (args == null || args.Length == 0) ? "help" : args[0 + leaveOut].ToLower();

			var methods = typeof(Program).GetMethods(BindingFlags.Public | BindingFlags.Static).OrderBy(method => method.Name);
			if (command != "help")
			{

				var method = methods.FirstOrDefault(m => m.Name.Equals(command, StringComparison.OrdinalIgnoreCase));
				if (method != null)
				{

					// INFO: Invoking the Sample by "the Delegate.Invoke" makes it hard to debug!
					// Using DebugInvoke
					object result
						= DebugStackTrace<TargetInvocationException>.Invoke(
							method,
							null,
							new object[] { 
                                dict,
							    args.Skip(1 + leaveOut).ToArray() /* string[] args */
					        });

					if (method.ReturnType == typeof(bool) && true == (bool)result)
					{
						return 0; // C good
					}

					return -1; // C bad
				}

				returnMain = -1;
				Console.WriteLine();
				Console.WriteLine("Command invalid.");
			}

			Console.WriteLine();
			Console.WriteLine("Usage: ./" + AppDomain.CurrentDomain.FriendlyName + " [--option=++] [--option=tcp://192.168.1.1:8080] <command> World Edward Ulrich");

			Console.WriteLine();
			Console.WriteLine("Available [option]s:");
			Console.WriteLine();
			foreach (FieldInfo field in fields)
			{
				Console.WriteLine("  --{0}", field.Name);
			}

			Console.WriteLine();
			Console.WriteLine("Available <command>s:");
			Console.WriteLine();

			foreach (MethodInfo meth in methods)
			{
				if (meth.Name == "Main")
					continue;
				if (0 < meth.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), true).Length)
					continue;

				Console.WriteLine("    {0}", meth.Name);
			}

			Console.WriteLine();
			return returnMain;
		}
	}


	internal static class DebugStackTrace<TException>
		where TException : Exception
	{
		[System.Diagnostics.DebuggerNonUserCode]
		[System.Diagnostics.DebuggerStepThrough]
		public static object Invoke(MethodInfo method, object target, params object[] args)
		{
			// source : http://csharptest.net/350/throw-innerexception-without-the-loosing-stack-trace/

			try
			{
				return method.Invoke(target, args);
			}
			catch (TException te)
			{
				if (te.InnerException == null)
					throw;

				Exception innerException = te.InnerException;

				var savestack = (ThreadStart)Delegate.CreateDelegate(typeof(ThreadStart), innerException, "InternalPreserveStackTrace", false, false);
				if (savestack != null) savestack();

				throw innerException; // -- now we can re-throw without trashing the stack
			}
		}

	}
}