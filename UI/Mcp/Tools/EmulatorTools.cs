using Mesen.Config;
using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class EmulatorTools
	{
		[McpServerTool(Name = "mesen_load_rom", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Load a ROM file into the emulator. Returns ROM info on success.")]
		public static string LoadRom(
			[Description("Absolute path to the ROM file")] string filepath,
			[Description("Optional path to an IPS/BPS patch file")] string? patchFile = null)
		{
			if(!File.Exists(filepath)) {
				throw new McpException("File not found: " + filepath);
			}

			bool result = EmuApi.LoadRom(filepath, patchFile ?? string.Empty);
			if(!result) {
				throw new McpException("Failed to load ROM: " + filepath);
			}

			RomInfo info = EmuApi.GetRomInfo();
			return McpToolHelper.Serialize(new {
				success = true,
				romInfo = new {
					name = info.GetRomName(),
					format = info.Format.ToString(),
					consoleType = info.ConsoleType.ToString(),
					hash = EmuApi.GetRomHash(HashType.Sha1)
				}
			});
		}

		[McpServerTool(Name = "mesen_get_rom_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get information about the currently loaded ROM.")]
		public static string GetRomInfo()
		{
			McpToolHelper.EnsureRunning();

			RomInfo info = EmuApi.GetRomInfo();
			return McpToolHelper.Serialize(new {
				name = info.GetRomName(),
				format = info.Format.ToString(),
				consoleType = info.ConsoleType.ToString(),
				hash = EmuApi.GetRomHash(HashType.Sha1)
			});
		}

		[McpServerTool(Name = "mesen_playback", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control emulation playback. Use action 'pause' to pause or 'resume' to resume emulation.")]
		public static string Playback(
			[Description("Action to perform: 'pause' or 'resume'")] string action)
		{
			McpToolHelper.EnsureRunning();

			switch(action.ToLowerInvariant()) {
				case "pause":
					EmuApi.Pause();
					break;
				case "resume":
					EmuApi.Resume();
					break;
				default:
					throw new McpException("Invalid action: " + action + ". Use 'pause' or 'resume'.");
			}

			return McpToolHelper.Serialize(new {
				success = true,
				action = action.ToLowerInvariant(),
				paused = EmuApi.IsPaused()
			});
		}

		[McpServerTool(Name = "mesen_get_status", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the current emulator status: running, paused, timing info.")]
		public static string GetStatus()
		{
			bool running = EmuApi.IsRunning();
			bool paused = EmuApi.IsPaused();

			if(!running) {
				return McpToolHelper.Serialize(new { running, paused });
			}

			RomInfo info = EmuApi.GetRomInfo();
			CpuType mainCpu = info.ConsoleType.GetMainCpuType();
			TimingInfo timing = EmuApi.GetTimingInfo(mainCpu);

			return McpToolHelper.Serialize(new {
				running,
				paused,
				consoleType = info.ConsoleType.ToString(),
				romName = info.GetRomName(),
				fps = Math.Round(timing.Fps, 2),
				frameCount = timing.FrameCount,
				masterClock = timing.MasterClock
			});
		}

		[McpServerTool(Name = "mesen_take_screenshot", ReadOnly = true, Destructive = false, OpenWorld = false),
		 Description("Take a screenshot of the current frame. Returns the image as base64 PNG. Optionally saves to file.")]
		public static IEnumerable<ContentBlock> TakeScreenshot(
			[Description("Optional file path to also save the PNG to disk")] string? outputFile = null)
		{
			McpToolHelper.EnsureRunning();

			string tempPath = Path.Combine(Path.GetTempPath(), "mesen_screenshot_" + Guid.NewGuid().ToString("N") + ".png");
			try {
				Int32 bytesWritten = EmuApi.TakeScreenshotToFile(tempPath);
				if(bytesWritten <= 0) {
					throw new McpException("Failed to capture screenshot.");
				}

				byte[] pngData = File.ReadAllBytes(tempPath);

				if(!string.IsNullOrEmpty(outputFile)) {
					File.Copy(tempPath, outputFile, true);
					yield return new TextContentBlock { Text = McpToolHelper.Serialize(new { savedTo = outputFile, size = pngData.Length }) };
				}

				yield return ImageContentBlock.FromBytes(pngData, "image/png");
			} finally {
				try { File.Delete(tempPath); } catch { }
			}
		}

		[McpServerTool(Name = "mesen_save_state", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Save or load emulator state. Use action 'save' to save state, or 'load' to load state, from a numbered slot (1-10) or a file path.")]
		public static string SaveState(
			[Description("Action to perform: 'save' or 'load'")] string action,
			[Description("Slot number (1-10) or absolute file path")] string slotOrPath)
		{
			McpToolHelper.EnsureRunning();

			switch(action.ToLowerInvariant()) {
				case "save":
					if(int.TryParse(slotOrPath, out int saveSlot) && saveSlot >= 1 && saveSlot <= 10) {
						EmuApi.SaveState((uint)saveSlot);
						return McpToolHelper.Serialize(new { success = true, action = "save", slot = saveSlot });
					} else {
						EmuApi.SaveStateFile(slotOrPath);
						return McpToolHelper.Serialize(new { success = true, action = "save", file = slotOrPath });
					}

				case "load":
					if(int.TryParse(slotOrPath, out int loadSlot) && loadSlot >= 1 && loadSlot <= 10) {
						EmuApi.LoadState((uint)loadSlot);
						return McpToolHelper.Serialize(new { success = true, action = "load", slot = loadSlot });
					} else {
						if(!File.Exists(slotOrPath)) {
							throw new McpException("File not found: " + slotOrPath);
						}
						EmuApi.LoadStateFile(slotOrPath);
						return McpToolHelper.Serialize(new { success = true, action = "load", file = slotOrPath });
					}

				default:
					throw new McpException("Invalid action: " + action + ". Use 'save' or 'load'.");
			}
		}
	}
}
