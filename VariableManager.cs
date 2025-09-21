using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

namespace Phidgets2Prosim
{
	public static class VariableManager
	{
		private static readonly ConcurrentDictionary<string, int> Vars =
			new ConcurrentDictionary<string, int>();

		private static readonly ConcurrentDictionary<string, List<Action<string, int>>> Subs =
			new ConcurrentDictionary<string, List<Action<string, int>>>();

		public static int Get(string name)
		{
			int v;
			return Vars.TryGetValue(name, out v) ? v : 0;
		}

		public static void Set(string name, int value)
		{
			int old;
			if (Vars.TryGetValue(name, out old) && old == value)
				return;

			Vars[name] = value;

			// DEBUG: always print
			Debug.WriteLine($"[VAR][SET] {name} = {value}");

			List<Action<string, int>> handlers;
			if (Subs.TryGetValue(name, out handlers) && handlers != null && handlers.Count > 0)
			{
				var snapshot = new List<Action<string, int>>(handlers);
				foreach (var h in snapshot)
				{
					try { h(name, value); } catch { /* swallow */ }
				}
			}
		}

		public static IDisposable Subscribe(string name, Action<string, int> handler)
		{
			Subs.AddOrUpdate(
				name,
				_ => new List<Action<string, int>> { handler },
				(_, list) =>
				{
					var newList = (list == null) ? new List<Action<string, int>>() : new List<Action<string, int>>(list);
					newList.Add(handler);
					return newList;
				});

			// DEBUG: always print
			Debug.WriteLine($"[VAR][SUB]  {name}");

			return new Subscription(name, handler);
		}

		private sealed class Subscription : IDisposable
		{
			private readonly string _name;
			private readonly Action<string, int> _handler;
			private bool _disposed;

			public Subscription(string name, Action<string, int> handler)
			{
				_name = name;
				_handler = handler;
			}

			public void Dispose()
			{
				if (_disposed) return;
				_disposed = true;

				Subs.AddOrUpdate(
					_name,
					_ => new List<Action<string, int>>(),
					(_, list) =>
					{
						var newList = (list == null) ? new List<Action<string, int>>() : new List<Action<string, int>>(list);
						newList.Remove(_handler);
						return newList;
					});

				Debug.WriteLine($"[VAR][UNSUB]  {_name}"); // DEBUG
			}
		}
	}
}
