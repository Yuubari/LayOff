using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LayOff
{
	public class KeyboardLayout
	{
		public UInt32 Id { get; }

		public UInt16 LanguageId { get; }
		public UInt16 KeyboardId { get; }

		public String LanguageName { get; }
		public String KeyboardName { get; }

		internal KeyboardLayout(UInt32 id, UInt16 languageId, UInt16 keyboardId, String languageName, String keyboardName)
		{
			this.Id = id;
			this.LanguageId = languageId;
			this.KeyboardId = keyboardId;
			this.LanguageName = languageName;
			this.KeyboardName = keyboardName;
		}
	}

	class Program
	{
		private static List<uint> layoutList = new List<uint> { 0x0809, 0x0409 };

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

		[DllImport("user32.dll")]
		private static extern bool UnloadKeyboardLayout(uint hkl);

		private static KeyboardLayout CreateKeyboardLayout(UInt32 keyboardLayoutId)
		{
			var languageId = (UInt16)(keyboardLayoutId & 0xFFFF);
			var keyboardId = (UInt16)(keyboardLayoutId >> 16);

			return new KeyboardLayout(keyboardLayoutId, languageId, keyboardId, GetCultureInfoName(languageId), GetCultureInfoName(keyboardId));

			String GetCultureInfoName(UInt16 cultureId)
			{
				String dName;
				try
				{
					dName = CultureInfo.GetCultureInfo(cultureId).DisplayName;
				}
				catch (CultureNotFoundException) {
					return "N/A";
				}

				return dName;
			}
		}

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
					"  Shows this help message\n\n" +
					"layoff /L\n" +
					"  Lists layouts currently present\n\n" +
					"layoff [layout ID] …\n" +
					"  Unloads listed layout IDs. Layout IDs must be hexadecimal numbers\n" +
					"  prefixed with 0x. Defaults to \"0x0809 0x0409\".\n\n" +
					"Launch without arguments to use the default layout list."
					);
				return false;
			}

			if (args[0].Equals("/L", StringComparison.OrdinalIgnoreCase))
			{
				// List currently active layouts

				List<uint> klList;

				if (GetLayouts(out klList) > 0)
				{
					Console.WriteLine("Currently installed layouts:\n");
				}

				foreach (var klId in klList)
				{
					var kbLayout = CreateKeyboardLayout(klId);
					Console.Write("  ID ");
					Console.ForegroundColor = ConsoleColor.White;
					Console.Write("0x{0:X4}", kbLayout.KeyboardId);
					Console.WriteLine(":");
					Console.ResetColor();
					Console.WriteLine(
						"    Language: {0} (0x{1:X4})\n" +
						"    Layout: {2} (0x{3:X8})\n\n",
						kbLayout.LanguageName,
						kbLayout.LanguageId,
						kbLayout.KeyboardName,
						klId);
				}

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

		static private uint GetLayouts(out List<uint> klList)
		{
			var klSize = GetKeyboardLayoutList(0, null);

			klList = new List<uint> { };

			if (klSize > 0)
			{
				var klIds = new IntPtr[klSize];
				GetKeyboardLayoutList(klIds.Length, klIds);

				foreach (var klId in klIds)
				{
					klList.Add((uint)klId);
				}
			}

			return klSize;
		}

		static int Main(string[] args)
		{
			if ((args.Length > 0) && !ArgumentParser(args))
			{
				return 2;
			}

			List<uint> klList;
			if (GetLayouts(out klList) < 0)
			{
				Console.WriteLine("No layouts at all, nothing to do.");
				return 1;
			}

			foreach (var klId in klList)
			{
				var testId = klId & 0x0000ffff;

				if (layoutList.Contains(testId))
				{
					Console.WriteLine("Found a layout to lay off: 0x{0:X4} (system layout ID 0x{1:X8}).", testId, (uint)klId);
					if (!UnloadKeyboardLayout(klId))
					{
						Console.WriteLine("Failed to unload this layout.");
					}
				}
			}

			return 0;
		}
	}
}
