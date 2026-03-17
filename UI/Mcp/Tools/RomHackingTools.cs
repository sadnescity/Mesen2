using Mesen.Interop;
using Mesen.Mcp.Models;
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
	public class RomHackingTools
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
			return McpToolHelper.Serialize(new SaveRomResponse {
				Success = success,
				File = filepath,
				SaveAsIps = saveAsIps
			});
		}

		[McpServerTool(Name = "mesen_get_rom_header", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get parsed ROM header information. Currently only supported for NES ROMs (iNes/NES 2.0). Returns structured header fields. For other consoles, use mesen_read_memory to read the internal header from PRG ROM directly.")]
		public static string GetRomHeader()
		{
			McpToolHelper.EnsureDebuggerReady();

			RomInfo romInfo = EmuApi.GetRomInfo();
			if(romInfo.ConsoleType != ConsoleType.Nes) {
				throw new McpException("ROM header parsing is currently only supported for NES. For " + romInfo.ConsoleType + ", use mesen_read_memory to read the internal header from PRG ROM.");
			}

			byte[] header = DebugApi.GetRomHeader();
			if(header.Length < 16 || header[0] != 0x4E || header[1] != 0x45 || header[2] != 0x53 || header[3] != 0x1A) {
				throw new McpException("Invalid or missing NES header");
			}

			// Parse iNes/NES 2.0 header
			bool isNes2 = (header[7] & 0x0C) == 0x08;
			int mapper = isNes2
				? ((header[8] & 0x0F) << 8) | (header[7] & 0xF0) | (header[6] >> 4)
				: (header[7] & 0xF0) | (header[6] >> 4);
			int subMapper = isNes2 ? (header[8] & 0xF0) >> 4 : 0;

			int prgSize;
			int chrSize;
			if(isNes2) {
				prgSize = ((header[9] & 0x0F) == 0x0F)
					? (int)(Math.Pow(2, header[4] >> 2) * ((header[4] & 0x03) * 2 + 1))
					: (((header[9] & 0x0F) << 8) | header[4]) * 16384;
				chrSize = ((header[9] & 0xF0) == 0xF0)
					? (int)(Math.Pow(2, header[5] >> 2) * ((header[5] & 0x03) * 2 + 1))
					: (((header[9] & 0xF0) >> 4 << 8) | header[5]) * 8192;
			} else {
				prgSize = header[4] * 16384;
				chrSize = header[5] * 8192;
			}

			string mirroring = (header[6] & 0x08) != 0 ? "FourScreen"
				: (header[6] & 0x01) != 0 ? "Vertical" : "Horizontal";

			// Build raw bytes string (just the 16-byte header)
			StringBuilder raw = new();
			for(int i = 0; i < 16 && i < header.Length; i++) {
				if(i > 0) raw.Append(' ');
				raw.Append(header[i].ToString("X2"));
			}

			return McpToolHelper.Serialize(new NesRomHeaderResponse {
				Format = isNes2 ? "NES 2.0" : "iNes",
				Mapper = mapper,
				SubMapper = subMapper > 0 ? subMapper : null,
				PrgRomSize = prgSize,
				PrgRomSizeKB = prgSize / 1024,
				ChrRomSize = chrSize,
				ChrRomSizeKB = chrSize / 1024,
				Mirroring = mirroring,
				Battery = (header[6] & 0x02) != 0,
				Trainer = (header[6] & 0x04) != 0,
				RawBytes = raw.ToString()
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

			List<PaletteColorEntry> colors = new();
			for(int i = 0; i < rgbPalette.Length; i++) {
				UInt32 rgb = rgbPalette[i];
				colors.Add(new PaletteColorEntry {
					Index = i,
					Rgb = "#" + (rgb & 0xFFFFFF).ToString("X6"),
					Raw = "$" + rawPalette[i].ToString("X4")
				});
			}

			return McpToolHelper.Serialize(new PaletteInfoResponse {
				ColorCount = info.ColorCount,
				BgColorCount = info.BgColorCount,
				SpriteColorCount = info.SpriteColorCount,
				ColorsPerPalette = info.ColorsPerPalette,
				RawFormat = info.RawFormat.ToString(),
				Colors = colors
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
			return McpToolHelper.Serialize(new SetPaletteColorResponse {
				Success = true,
				ColorIndex = colorIndex,
				Color = "#" + (color & 0xFFFFFF).ToString("X6")
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

			return McpToolHelper.Serialize(new GetTilePixelResponse {
				TileAddress = "$" + addr.ToString("X4"),
				X = x,
				Y = y,
				ColorIndex = pixel
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

			return McpToolHelper.Serialize(new SetTilePixelResponse {
				Success = true,
				TileAddress = "$" + addr.ToString("X4"),
				X = x,
				Y = y,
				ColorIndex = color
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

			return McpToolHelper.Serialize(new RunLuaScriptResponse {
				Success = true,
				ScriptId = scriptId,
				Persistent = persistent,
				Log = log
			});
		}

		[McpServerTool(Name = "mesen_remove_lua_script", ReadOnly = false, Destructive = false, OpenWorld = false), Description("Remove a running Lua script by its script ID (returned by mesen_run_lua_script with persistent=true).")]
		public static string RemoveLuaScript(
			[Description("Script ID returned by mesen_run_lua_script")] int scriptId)
		{
			McpToolHelper.EnsureDebuggerReady();

			string log = DebugApi.GetScriptLog(scriptId);
			DebugApi.RemoveScript(scriptId);

			return McpToolHelper.Serialize(new RemoveLuaScriptResponse {
				Success = true,
				ScriptId = scriptId,
				Log = log
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

			return McpToolHelper.Serialize(new SetCheatsResponse {
				Success = errors.Count == 0,
				CheatsApplied = cheats.Count,
				Errors = errors
			});
		}

		[McpServerTool(Name = "mesen_clear_cheats", ReadOnly = false, Destructive = true, OpenWorld = false), Description("Clear all active cheat codes.")]
		public static string ClearCheats()
		{
			McpToolHelper.EnsureRunning();

			EmuApi.ClearCheats();
			return McpToolHelper.Serialize(new SuccessResponse { Success = true });
		}

	}
}
