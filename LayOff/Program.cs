using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LayOff
{
	class Program
	{
		private static List<uint> layoutList = new List<uint> { 0x0809, 0x0409 };

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);
		[DllImport("user32.dll")]
		private static extern bool UnloadKeyboardLayout(uint hkl);

		static bool ArgumentParser(string[] args)
		{
			if (args.Length == 0)
			{
				return true;
			}

			if (args[0].Equals("/?"))
			{
				Console.WriteLine(
					"Syntax:\n\n" +
					"layoff /?\n" +
					"  Shows this help message" +
					"layoff [layout ID] …\n" +
					"  Unloads listed layout IDs. Layout IDs must be hexadecimal numbers\n" +
					"  prefixed with 0x.\n" +
					"  Defaults to \"0x0809 0x0409\".\n\n" +
					"Launch without arguments to use the default layout list."
					);
				return false;
			}

			List<uint> layoutsToUnload = new List<uint> { };

			foreach (var arg in args)
			{
				var hexNumber = arg.Substring(2);
				if (uint.TryParse(hexNumber, System.Globalization.NumberStyles.HexNumber, null, out uint layoutId))
				{
					if (!layoutsToUnload.Contains(layoutId))
					{
						layoutsToUnload.Add(layoutId & 0x0000ffff);
					}
				}
#if DEBUG
				else
				{
					Console.WriteLine("Failed to parse: {0}", arg);
				}
#endif
			}

			if (layoutsToUnload.Count > 0)
			{
				layoutList = layoutsToUnload;
			}

#if DEBUG
			Console.WriteLine("Layouts to be laid off:");
			foreach (var klId in layoutList)
			{
				Console.WriteLine("  0x{0:X4}", klId);
			}
			Console.WriteLine();
#endif

			return true;
		}

		static int Main(string[] args)
		{
			if ((args.Length > 0) && !ArgumentParser(args))
			{
				return 2;
			}
#if DEBUG
			Console.Write("Fetching layout list: ");
#endif
			var klSize = GetKeyboardLayoutList(0, null);
#if DEBUG
			Console.WriteLine("{0:D} layouts present.", klSize);
#endif
			if (klSize < 1)
			{
				Console.WriteLine("No layouts at all, nothing to do.");
				return 1;
			}

			var klIds = new IntPtr[klSize];
			GetKeyboardLayoutList(klIds.Length, klIds);

			foreach (var klId in klIds)
			{
				var testId = (uint)klId & 0x0000ffff;

				if (layoutList.Contains(testId))
				{
					Console.WriteLine("Found a layout to lay off: 0x{0:X4} (system layout ID 0x{1:X8}).", testId, (uint)klId);
					if (!UnloadKeyboardLayout(testId))
					{
						Console.WriteLine("Failed to unload this layout.");
					}
				}
			}

			return 0;
		}
	}
}
