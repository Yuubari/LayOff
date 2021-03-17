using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LayOff
{
	public class KeyboardLayout : IEquatable<KeyboardLayout>
	{
		public uint Id { get; }
		public bool MatchLanguage { get; }
		public ushort LanguageId { get; }
		public ushort KeyboardId { get; }
		public string LanguageName { get; }
		public string KeyboardName { get; }

		internal KeyboardLayout(uint id, bool lang = false)
		{
			Id = id;

			MatchLanguage = (id >> 16) == 0;

			LanguageId = (ushort)(id & 0xFFFF);
			LanguageName = GetCultureInfoName(LanguageId);

			if (!MatchLanguage)
			{
				KeyboardId = (ushort)(id >> 16);
				KeyboardName = GetCultureInfoName(KeyboardId);
			}
		}

		private string GetCultureInfoName(ushort cultureId)
		{
			try
			{
				return CultureInfo.GetCultureInfo(cultureId).DisplayName;
			}
			catch (CultureNotFoundException)
			{
				return "Unknown";
			}
		}

		public bool Equals(KeyboardLayout other)
		{
			return Id == other.Id;
		}

		public override string ToString()
		{
			return string.Format("0x" + (MatchLanguage ? "{0:X4} (by language ID)" : "{0:X8}"), Id);
		}

		public void PrintLayoutInfo()
		{
			Console.Write("  ID ");
			Console.ForegroundColor = ConsoleColor.White;
			Console.Write(this.ToString());
			Console.ResetColor();
			Console.WriteLine(":");
			Console.WriteLine(
				"    Language: {0} (0x{1:X4})",
				LanguageName,
				LanguageId);
			if (!MatchLanguage)
			{
				Console.WriteLine(
					"    Layout:   {0} (0x{1:X4})",
					KeyboardName,
					KeyboardId);
			}
		}
	}

	class Program
	{
		private static List<KeyboardLayout> layoutList = new List<KeyboardLayout> {
			new KeyboardLayout(0x0809),
			new KeyboardLayout(0x0409),
		};

		private const string syntax =
@"Unloads keyboard layouts.

Syntax:

layoff /?
  Shows this help message.

layoff /L
  Lists layouts currently present.

layoff ID …
  Unloads listed layout IDs. Layout IDs must be hexadecimal numbers
  prefixed with 0x. Four-digit IDs are treated as language IDs, and
  all layouts related to that language ID are unloaded.

layoff
  Unloads layouts associated with US English and UK English; equals
  to ""layoff 0x0809 0x0409"".";

		[DllImport("user32.dll")]
		private static extern uint GetKeyboardLayoutList(int nBuff, IntPtr[] lpList);

		[DllImport("user32.dll")]
		private static extern bool UnloadKeyboardLayout(uint hkl);

		static bool ArgumentParser(string[] args)
		{
			if (args.Length > 0)
			{
				switch (args[0].ToLower())
				{
					case "/?":
						Console.WriteLine(syntax);
						return false;

					case "/l":
						List<KeyboardLayout> klList;

						if (GetLayouts(out klList) > 0)
						{
							Console.WriteLine("Currently installed layouts:\n");
						}

						foreach (var kbLayout in klList)
						{
							kbLayout.PrintLayoutInfo();
							Console.WriteLine();
						}

						return false;
				}

				List<KeyboardLayout> layoutsToUnload = new List<KeyboardLayout> { };

				foreach (var arg in args)
				{
					try
					{
						string hexNumber = arg.Substring(2);

						if (uint.TryParse(hexNumber, NumberStyles.HexNumber, null, out uint layoutId))
						{
							var layout = new KeyboardLayout(layoutId);

							if (!layoutsToUnload.Contains(layout))
							{
								layoutsToUnload.Add(layout);
							}
						}
#if DEBUG
						else
						{
							Console.WriteLine("Failed to parse: {0}", arg);
						}
#endif
					}
					catch (ArgumentOutOfRangeException)
					{
						continue;
					}
				}

				if (layoutsToUnload.Count > 0)
				{
					layoutList = layoutsToUnload;
				}

			}

#if DEBUG
			Console.WriteLine("Layouts to be laid off:");
			foreach (var kbLayout in layoutList)
			{
				kbLayout.PrintLayoutInfo();
				Console.WriteLine();
			}
			Console.WriteLine();
#endif

			return true;
		}

		static private uint GetLayouts(out List<KeyboardLayout> klList)
		{
			var klSize = GetKeyboardLayoutList(0, null);

			klList = new List<KeyboardLayout> { };

			if (klSize > 0)
			{
				var klIds = new IntPtr[klSize];
				GetKeyboardLayoutList(klIds.Length, klIds);

				foreach (var klId in klIds)
				{
					klList.Add(new KeyboardLayout((uint)klId));
				}
			}

			return klSize;
		}

		static int Main(string[] args)
		{
			if (!ArgumentParser(args))
			{
				return 2;
			}

			if (GetLayouts(out List<KeyboardLayout> klList) < 0)
			{
				Console.WriteLine("No layouts at all, nothing to do.");
				return 1;
			}

			foreach (var layout in klList)
			{
				if (layoutList.Contains(layout))
				{
					Console.WriteLine("Found a layout to lay off: {0}.", layout);
					if (!UnloadKeyboardLayout(layout.Id))
					{
						Console.WriteLine("Failed to unload {0}.", layout);
					}
				}
			}

			return 0;
		}
	}
}
