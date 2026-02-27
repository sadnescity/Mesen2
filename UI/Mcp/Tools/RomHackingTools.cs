using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class RomHackingTools
	{
		[McpServerTool(Name = "mesen_save_modified_rom", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Save the currently loaded (and possibly modified) ROM to a file. Can create an IPS patch instead.")]
		public static string SaveModifiedRom(
			[Description("Absolute file path to save the ROM or IPS patch")] string filepath,
			[Description("If true, saves an IPS patch file instead of the full ROM (default false)")] bool saveAsIps = false,
			[Description("CDL strip option: StripNone (default), StripUnused, StripUsed")] string stripOption = "StripNone")
		{
			McpToolHelper.EnsureDebuggerReady();

			if(!Enum.TryParse<CdlStripOption>(stripOption, true, out CdlStripOption strip)) {
				throw new McpException("Invalid strip option: " + stripOption);
			}

			bool success = DebugApi.SaveRomToDisk(filepath, saveAsIps, strip);
			return McpToolHelper.Serialize(new {
				success = success,
				file = filepath,
				saveAsIps = saveAsIps
			});
		}

		[McpServerTool(Name = "mesen_get_rom_header", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false), Description("Get the raw ROM header bytes as a hex dump.")]
		public static string GetRomHeader()
		{
			McpToolHelper.EnsureDebuggerReady();

			byte[] header = DebugApi.GetRomHeader();
			if(header.Length == 0) {
				throw new McpException("No ROM header available");
			}

			StringBuilder hexDump = new();
			for(int i = 0; i < header.Length; i += 16) {
				hexDump.Append($"${i:X4}: ");
				for(int j = 0; j < 16 && (i + j) < header.Length; j++) {
					hexDump.Append($"{header[i + j]:X2} ");
				}
				for(int j = header.Length - i; j < 16; j++) {
					hexDump.Append("   ");
				}
				hexDump.Append(" | ");
				for(int j = 0; j < 16 && (i + j) < header.Length; j++) {
					byte b = header[i + j];
					hexDump.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
				}
				hexDump.AppendLine();
			}

			return McpToolHelper.Serialize(new {
				size = header.Length,
				hexDump = hexDump.ToString()
			});
		}

		[McpServerTool(Name = "mesen_get_palette", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false), Description("Get the palette colors for the current system.")]
		public static string GetPalette(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();

			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			DebugPaletteInfo info = DebugApi.GetPaletteInfo(cpu);
			UInt32[] rgbPalette = info.GetRgbPalette();
			UInt32[] rawPalette = info.GetRawPalette();

			List<object> colors = new();
			for(int i = 0; i < rgbPalette.Length; i++) {
				UInt32 rgb = rgbPalette[i];
				colors.Add(new {
					index = i,
					rgb = "#" + (rgb & 0xFFFFFF).ToString("X6"),
					raw = "$" + rawPalette[i].ToString("X4")
				});
			}

			return McpToolHelper.Serialize(new {
				colorCount = info.ColorCount,
				bgColorCount = info.BgColorCount,
				spriteColorCount = info.SpriteColorCount,
				colorsPerPalette = info.ColorsPerPalette,
				rawFormat = info.RawFormat.ToString(),
				colors = colors
			});
		}

		[McpServerTool(Name = "mesen_set_palette_color", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Set a palette color at runtime.")]
		public static string SetPaletteColor(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType,
			[Description("Color index in the palette (0-based)")] int colorIndex,
			[Description("RGB color value as 6-digit hex (e.g. 'FF0000' for red). Optionally prefixed with '#' or '$'.")] string colorHex)
		{
			McpToolHelper.EnsureDebuggerReady();

			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			colorHex = colorHex.TrimStart('#').TrimStart('$');
			if(!UInt32.TryParse(colorHex, System.Globalization.NumberStyles.HexNumber, null, out UInt32 color)) {
				throw new McpException("Invalid color hex: " + colorHex);
			}

			// Ensure alpha is set
			color |= 0xFF000000;

			DebugApi.SetPaletteColor(cpu, colorIndex, color);
			return McpToolHelper.Serialize(new {
				success = true,
				colorIndex = colorIndex,
				color = "#" + (color & 0xFFFFFF).ToString("X6")
			});
		}

		[McpServerTool(Name = "mesen_get_tile_pixel", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false), Description("Read a single pixel from a tile in memory.")]
		public static string GetTilePixel(
			[Description("Tile address in memory (decimal or 0x/$ hex)")] string tileAddress,
			[Description("Tile pixel format (case-insensitive): Bpp2, Bpp4, Bpp8, NesBpp2, SmsBpp4, GbaBpp4, GbaBpp8, PceBpp4, WsBpp2, DirectColor")] string format,
			[Description("X coordinate within the tile (0-7 or 0-15)")] int x,
			[Description("Y coordinate within the tile (0-7 or 0-15)")] int y,
			[Description("Memory type for the tile data (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType)
		{
			McpToolHelper.EnsureDebuggerReady();

			uint addr = McpToolHelper.ParseAddress(tileAddress);

			if(!Enum.TryParse<TileFormat>(format, true, out TileFormat tileFormat)) {
				throw new McpException("Invalid tile format: " + format);
			}

			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			AddressInfo addressInfo = new() { Address = (int)addr, Type = memType };
			int pixel = DebugApi.GetTilePixel(addressInfo, tileFormat, x, y);

			return McpToolHelper.Serialize(new {
				tileAddress = "$" + addr.ToString("X4"),
				x = x,
				y = y,
				colorIndex = pixel
			});
		}

		[McpServerTool(Name = "mesen_set_tile_pixel", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Set a single pixel in a tile in memory.")]
		public static string SetTilePixel(
			[Description("Tile address in memory (decimal or 0x/$ hex)")] string tileAddress,
			[Description("Tile pixel format (case-insensitive): Bpp2, Bpp4, Bpp8, NesBpp2, SmsBpp4, GbaBpp4, GbaBpp8, PceBpp4, WsBpp2, DirectColor")] string format,
			[Description("X coordinate within the tile")] int x,
			[Description("Y coordinate within the tile")] int y,
			[Description("Color index to set")] int color,
			[Description("Memory type for the tile data (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType)
		{
			McpToolHelper.EnsureDebuggerReady();

			uint addr = McpToolHelper.ParseAddress(tileAddress);

			if(!Enum.TryParse<TileFormat>(format, true, out TileFormat tileFormat)) {
				throw new McpException("Invalid tile format: " + format);
			}

			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			AddressInfo addressInfo = new() { Address = (int)addr, Type = memType };
			DebugApi.SetTilePixel(addressInfo, tileFormat, x, y, color);

			return McpToolHelper.Serialize(new {
				success = true,
				tileAddress = "$" + addr.ToString("X4"),
				x = x,
				y = y,
				colorIndex = color
			});
		}

		[McpServerTool(Name = "mesen_run_lua_script", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Run a Lua script in the emulator and return its log output. Set persistent=true to keep the script running (e.g. for callback-based scripts); use mesen_remove_lua_script to stop it later.")]
		public static string RunLuaScript(
			[Description("Lua script code to execute")] string code,
			[Description("Time to wait for script output in milliseconds (default 200, max 5000)")] int waitMs = 200,
			[Description("If true, keep the script running after returning (default false). Returns scriptId for later removal.")] bool persistent = false)
		{
			McpToolHelper.EnsureDebuggerReady();

			Int32 scriptId = DebugApi.LoadScript("mcp_script", "", code);
			if(scriptId < 0) {
				throw new McpException("Failed to load script");
			}

			// Wait for script to produce output
			System.Threading.Thread.Sleep(Math.Min(waitMs, 5000));

			string log = DebugApi.GetScriptLog(scriptId);

			if(!persistent) {
				DebugApi.RemoveScript(scriptId);
			}

			return McpToolHelper.Serialize(new {
				success = true,
				scriptId = scriptId,
				persistent = persistent,
				log = log
			});
		}

		[McpServerTool(Name = "mesen_remove_lua_script", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Remove a running Lua script by its script ID (returned by mesen_run_lua_script with persistent=true).")]
		public static string RemoveLuaScript(
			[Description("Script ID returned by mesen_run_lua_script")] int scriptId)
		{
			McpToolHelper.EnsureDebuggerReady();

			string log = DebugApi.GetScriptLog(scriptId);
			DebugApi.RemoveScript(scriptId);

			return McpToolHelper.Serialize(new {
				success = true,
				scriptId = scriptId,
				log = log
			});
		}

		[McpServerTool(Name = "mesen_set_cheats", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Set cheat codes. Each code is in the format 'Type:Code' (e.g. 'NesGameGenie:SXIOPO', 'SnesProActionReplay:7E0DBF01').")]
		public static string SetCheats(
			[Description("Array of cheat code strings in 'Type:Code' format. Valid types: NesGameGenie, NesCustom, SnesGameGenie, SnesProActionReplay, GbGameGenie, GbGameShark. Example: 'SnesProActionReplay:7E0DBF01'")] string[] codes)
		{
			McpToolHelper.EnsureRunning();

			List<InteropCheatCode> cheats = new();
			List<string> errors = new();

			foreach(string codeStr in codes) {
				int colonIdx = codeStr.IndexOf(':');
				if(colonIdx < 0) {
					errors.Add("Invalid format (expected 'Type:Code'): " + codeStr);
					continue;
				}

				string typePart = codeStr.Substring(0, colonIdx).Trim();
				string codePart = codeStr.Substring(colonIdx + 1).Trim();

				if(!Enum.TryParse<CheatType>(typePart, true, out CheatType cheatType)) {
					errors.Add("Invalid cheat type: " + typePart);
					continue;
				}

				cheats.Add(new InteropCheatCode(cheatType, codePart));
			}

			if(cheats.Count > 0) {
				EmuApi.SetCheats(cheats.ToArray(), (UInt32)cheats.Count);
			}

			return McpToolHelper.Serialize(new {
				success = errors.Count == 0,
				cheatsApplied = cheats.Count,
				errors = errors
			});
		}

		[McpServerTool(Name = "mesen_clear_cheats", ReadOnly = false, Destructive = true, OpenWorld = false), Description("Clear all active cheat codes.")]
		public static string ClearCheats()
		{
			McpToolHelper.EnsureRunning();

			EmuApi.ClearCheats();
			return McpToolHelper.Serialize(new { success = true });
		}

	}
}
