using Mesen.Config;
using Mesen.Config.Shortcuts;
using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class EmulatorConfigTools
	{
		[McpServerTool(Name = "mesen_get_version", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the Mesen emulator version and build date.")]
		public static string GetVersion()
		{
			Version version = EmuApi.GetMesenVersion();
			string buildDate = EmuApi.GetMesenBuildDate();
			return McpToolHelper.Serialize(new {
				version = version.ToString(3),
				buildDate = buildDate
			});
		}

		[McpServerTool(Name = "mesen_get_screen_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get screen size and aspect ratio information.")]
		public static string GetScreenInfo()
		{
			McpToolHelper.EnsureRunning();

			FrameInfo screenSize = EmuApi.GetBaseScreenSize();
			double aspectRatio = EmuApi.GetAspectRatio();

			return McpToolHelper.Serialize(new {
				width = screenSize.Width,
				height = screenSize.Height,
				aspectRatio = Math.Round(aspectRatio, 4)
			});
		}

		[McpServerTool(Name = "mesen_shortcut", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("List or execute emulator shortcuts/actions. Use action='list' to see all available shortcuts, action='execute' to run one. Common shortcuts: FastForward, Rewind, Pause, TakeScreenshot, ToggleCheats, RunSingleFrame, IncreaseSpeed, DecreaseSpeed, MaxSpeed.")]
		public static string Shortcut(
			[Description("Action: 'list' or 'execute'")] string action,
			[Description("Shortcut name for 'execute' (call with action='list' first)")] string? shortcut = null,
			[Description("Optional parameter for shortcut")] int param = 0)
		{
			switch(action.ToLowerInvariant()) {
				case "list": {
					List<string> shortcuts = new();
					foreach(EmulatorShortcut s in Enum.GetValues<EmulatorShortcut>()) {
						if(s != EmulatorShortcut.LastValidValue) {
							shortcuts.Add(s.ToString());
						}
					}
					return McpToolHelper.Serialize(new {
						count = shortcuts.Count,
						shortcuts = shortcuts
					});
				}

				case "execute": {
					if(string.IsNullOrEmpty(shortcut)) {
						throw new McpException("Shortcut name is required for 'execute' action.");
					}
					if(!Enum.TryParse<EmulatorShortcut>(shortcut, true, out EmulatorShortcut emuShortcut)) {
						throw new McpException("Invalid shortcut: " + shortcut + ". Call with action='list' to see valid values.");
					}
					EmuApi.ExecuteShortcut(new ExecuteShortcutParams() {
						Shortcut = emuShortcut,
						Param = (uint)param
					});
					return McpToolHelper.Serialize(new {
						success = true,
						shortcut = shortcut
					});
				}

				default:
					throw new McpException("Invalid action: " + action + ". Use 'list' or 'execute'.");
			}
		}

		[McpServerTool(Name = "mesen_reset", ReadOnly = false, Destructive = true, OpenWorld = false),
		 Description("Reset the console. Soft reset simulates pressing the reset button. Hard reset power cycles the console, clearing all RAM.")]
		public static string Reset(
			[Description("Reset type: 'soft' (reset button) or 'hard' (power cycle, clears RAM)")] string type)
		{
			McpToolHelper.EnsureRunning();

			switch(type.ToLowerInvariant()) {
				case "soft":
					EmuApi.ExecuteShortcut(new ExecuteShortcutParams() { Shortcut = EmulatorShortcut.Reset });
					break;
				case "hard":
					EmuApi.ExecuteShortcut(new ExecuteShortcutParams() { Shortcut = EmulatorShortcut.PowerCycle });
					break;
				default:
					throw new McpException("Invalid reset type: " + type + ". Use 'soft' or 'hard'.");
			}

			return McpToolHelper.Serialize(new {
				success = true,
				type = type.ToLowerInvariant()
			});
		}

		[McpServerTool(Name = "mesen_set_emulation_flag", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Set an emulation flag. Flags: Turbo (fast forward), Rewind, MaximumSpeed (uncapped FPS).")]
		public static string SetEmulationFlag(
			[Description("Flag name: Turbo, Rewind, MaximumSpeed")] string flag,
			[Description("Enable (true) or disable (false)")] bool enabled)
		{
			if(!Enum.TryParse<EmulationFlags>(flag, true, out EmulationFlags emuFlag)) {
				throw new McpException("Invalid flag: " + flag + ". Valid: Turbo, Rewind, MaximumSpeed.");
			}

			// Only allow safe flags to be set from MCP
			if(emuFlag != EmulationFlags.Turbo && emuFlag != EmulationFlags.Rewind && emuFlag != EmulationFlags.MaximumSpeed) {
				throw new McpException("Only Turbo, Rewind, and MaximumSpeed flags can be set via MCP.");
			}

			ConfigApi.SetEmulationFlag(emuFlag, enabled);
			return McpToolHelper.Serialize(new {
				success = true,
				flag = flag,
				enabled = enabled
			});
		}

		[McpServerTool(Name = "mesen_get_timing_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get detailed timing information for the emulated system: FPS, frame count, master clock cycle, scanline, dot position.")]
		public static string GetTimingInfo(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureRunning();

			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);
			TimingInfo timing = EmuApi.GetTimingInfo(cpu);
			return McpToolHelper.Serialize(new {
				fps = Math.Round(timing.Fps, 2),
				frameCount = timing.FrameCount,
				masterClock = timing.MasterClock,
				masterClockRate = timing.MasterClockRate
			});
		}

		[McpServerTool(Name = "mesen_get_log", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the emulator log output (debug messages, warnings, errors).")]
		public static string GetLog()
		{
			string log = EmuApi.GetLog();
			return McpToolHelper.Serialize(new {
				log = log
			});
		}

		[McpServerTool(Name = "mesen_display_message", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Display an on-screen message in the emulator window (OSD).")]
		public static string DisplayMessage(
			[Description("Title for the message")] string title,
			[Description("Message body")] string message)
		{
			EmuApi.DisplayMessage(title, message);
			return McpToolHelper.Serialize(new { success = true });
		}

		[McpServerTool(Name = "mesen_list_memory_types", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("IMPORTANT: Call this tool FIRST before using any tool that requires a 'memoryType' parameter. Returns all valid memory type names and sizes for the currently loaded ROM. Memory types vary by system (NES, SNES, Game Boy, etc.) and by cartridge/mapper.")]
		public static string ListMemoryTypes()
		{
			McpToolHelper.EnsureRunning();

			List<object> types = new();
			foreach(MemoryType memType in Enum.GetValues<MemoryType>()) {
				Int32 size = DebugApi.GetMemorySize(memType);
				if(size > 0) {
					types.Add(new {
						name = memType.ToString(),
						size = size,
						sizeHex = "$" + size.ToString("X"),
						sizeKB = Math.Round(size / 1024.0, 2)
					});
				}
			}

			return McpToolHelper.Serialize(new {
				count = types.Count,
				memoryTypes = types
			});
		}

		[McpServerTool(Name = "mesen_list_cpu_types", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("IMPORTANT: Call this tool FIRST before using any tool that requires a 'cpuType' parameter. Returns all active CPU types for the currently loaded ROM. Main CPU types: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws. Co-processors (SNES only): Spc, Sa1, Gsu, Cx4, NecDsp, St018.")]
		public static string ListCpuTypes()
		{
			McpToolHelper.EnsureRunning();

			RomInfo info = EmuApi.GetRomInfo();
			CpuType mainCpu = info.ConsoleType.GetMainCpuType();

			List<object> cpus = new();
			foreach(CpuType cpu in Enum.GetValues<CpuType>()) {
				// Check if this CPU has any memory mapped (heuristic to see if it's active)
				try {
					MemoryType memType = cpu.ToMemoryType();
					Int32 memSize = DebugApi.GetMemorySize(memType);
					if(memSize > 0) {
						cpus.Add(new {
							name = cpu.ToString(),
							isMain = cpu == mainCpu,
							memoryType = memType.ToString(),
							memorySize = memSize
						});
					}
				} catch {
					// CPU type not available for this system
				}
			}

			return McpToolHelper.Serialize(new {
				consoleType = info.ConsoleType.ToString(),
				mainCpu = mainCpu.ToString(),
				availableCpus = cpus
			});
		}
	}
}
