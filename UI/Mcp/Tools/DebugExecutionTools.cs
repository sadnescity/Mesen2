using Mesen.Debugger;
using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class DebugExecutionTools
	{
		[McpServerTool(Name = "mesen_init_debugger", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Initialize the debugger. Must be called before using debug tools.")]
		public static string InitDebugger()
		{
			McpToolHelper.EnsureRunning();

			DebugApi.InitializeDebugger();
			McpToolHelper.MarkDebuggerInitialized();
			return McpToolHelper.Serialize(new { success = true });
		}

		[McpServerTool(Name = "mesen_step", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Step the CPU by a number of instructions. Type can be 'Step', 'StepOver', 'StepOut', 'CpuCycleStep', 'PpuStep', 'PpuScanline', 'PpuFrame'.")]
		public static string Step(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws, Spc, NecDsp, Sa1, Gsu, Cx4")] string cpuType,
			[Description("Number of steps (default 1)")] int count = 1,
			[Description("Step type: Step, StepOver, StepOut, CpuCycleStep, PpuStep, PpuScanline, PpuFrame")] string stepType = "Step")
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			if(!Enum.TryParse<StepType>(stepType, true, out StepType step)) {
				throw new McpException("Invalid step type: " + stepType);
			}

			DebugApi.Step(cpu, count, step);

			// Poll until execution pauses, with timeout
			int elapsed = 0;
			int maxWait = step == StepType.StepOut || step == StepType.PpuFrame ? 2000 : 500;
			while(!EmuApi.IsPaused() && elapsed < maxWait) {
				System.Threading.Thread.Sleep(5);
				elapsed += 5;
			}

			return GetCpuStateJson(cpu);
		}

		[McpServerTool(Name = "mesen_resume_execution", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Resume execution after a breakpoint or step.")]
		public static string ResumeExecution()
		{
			McpToolHelper.EnsureDebuggerReady();

			DebugApi.ResumeExecution();
			return McpToolHelper.Serialize(new { success = true });
		}

		[McpServerTool(Name = "mesen_is_execution_paused", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Check if execution is paused (at a breakpoint or after a step).")]
		public static string IsExecutionPaused()
		{
			return McpToolHelper.Serialize(new {
				paused = EmuApi.IsPaused(),
				running = EmuApi.IsRunning()
			});
		}

		[McpServerTool(Name = "mesen_breakpoint", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Manage breakpoints. Action: 'set', 'remove', 'remove_all', 'list'.")]
		public static string Breakpoint(
			[Description("Action: 'set', 'remove', 'remove_all', 'list'")] string action,
			[Description("Address (required for set/remove, decimal or 0x/$ hex)")] string? address = null,
			[Description("Breakpoint type for set: Execute, Read, Write (comma-separated)")] string? type = null,
			[Description("Memory type (required for set/remove)")] string? memoryType = null,
			[Description("CPU type (required for set)")] string? cpuType = null,
			[Description("End address for range breakpoints (for set)")] string? endAddress = null,
			[Description("Condition expression (for set, e.g. 'A == $42')")] string? condition = null)
		{
			McpToolHelper.EnsureDebuggerReady();

			switch(action.ToLowerInvariant()) {
				case "set": {
					if(address == null) {
						throw new McpException("'address' is required for action 'set'.");
					}
					if(type == null) {
						throw new McpException("'type' is required for action 'set'.");
					}
					if(memoryType == null) {
						throw new McpException("'memoryType' is required for action 'set'.");
					}
					if(cpuType == null) {
						throw new McpException("'cpuType' is required for action 'set'.");
					}

					uint addr = McpToolHelper.ParseAddress(address);
					CpuType cpu = McpToolHelper.ParseCpuType(cpuType);
					MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

					bool breakOnExec = false, breakOnRead = false, breakOnWrite = false;
					foreach(string t in type.Split(',', StringSplitOptions.TrimEntries)) {
						if(t.Equals("Execute", StringComparison.OrdinalIgnoreCase)) breakOnExec = true;
						else if(t.Equals("Read", StringComparison.OrdinalIgnoreCase)) breakOnRead = true;
						else if(t.Equals("Write", StringComparison.OrdinalIgnoreCase)) breakOnWrite = true;
					}

					if(!breakOnExec && !breakOnRead && !breakOnWrite) {
						throw new McpException("Invalid breakpoint type. Use Execute, Read, Write, or a combination.");
					}

					UInt32 endAddr = addr;
					if(endAddress != null) {
						endAddr = McpToolHelper.ParseAddress(endAddress);
					}

					Breakpoint bp = new Breakpoint() {
						CpuType = cpu,
						MemoryType = memType,
						BreakOnExec = breakOnExec,
						BreakOnRead = breakOnRead,
						BreakOnWrite = breakOnWrite,
						StartAddress = addr,
						EndAddress = endAddr,
						Enabled = true,
						MarkEvent = false,
						IgnoreDummyOperations = false,
						Condition = condition ?? ""
					};

					BreakpointManager.AddCpuType(cpu);
					BreakpointManager.AddBreakpoint(bp);

					return McpToolHelper.Serialize(new {
						success = true,
						breakpoint = new {
							startAddress = "$" + addr.ToString("X4"),
							endAddress = "$" + endAddr.ToString("X4"),
							type = type,
							memoryType = memoryType,
							cpuType = cpuType,
							condition = condition ?? ""
						}
					});
				}

				case "remove": {
					if(address == null) {
						throw new McpException("'address' is required for action 'remove'.");
					}
					if(memoryType == null) {
						throw new McpException("'memoryType' is required for action 'remove'.");
					}

					uint addr = McpToolHelper.ParseAddress(address);
					MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

					Breakpoint? target = null;
					foreach(Breakpoint bp in BreakpointManager.Breakpoints) {
						if(bp.StartAddress == addr && bp.MemoryType == memType) {
							target = bp;
							break;
						}
					}

					if(target == null) {
						throw new McpException($"No breakpoint found at ${addr:X4} ({memoryType}). Use mesen_breakpoint with action 'list' to see active breakpoints.");
					}

					BreakpointManager.RemoveBreakpoint(target);
					return McpToolHelper.Serialize(new {
						success = true,
						removed = new {
							startAddress = "$" + target.StartAddress.ToString("X4"),
							endAddress = "$" + target.EndAddress.ToString("X4"),
							memoryType = target.MemoryType.ToString()
						}
					});
				}

				case "remove_all": {
					BreakpointManager.ClearBreakpoints();
					return McpToolHelper.Serialize(new { success = true });
				}

				case "list": {
					List<object> bps = new();
					int index = 0;
					foreach(Breakpoint bp in BreakpointManager.Breakpoints) {
						bps.Add(new {
							index = index++,
							enabled = bp.Enabled,
							startAddress = "$" + bp.StartAddress.ToString("X4"),
							endAddress = "$" + bp.EndAddress.ToString("X4"),
							memoryType = bp.MemoryType.ToString(),
							cpuType = bp.CpuType.ToString(),
							breakOnExec = bp.BreakOnExec,
							breakOnRead = bp.BreakOnRead,
							breakOnWrite = bp.BreakOnWrite,
							condition = bp.Condition
						});
					}

					return McpToolHelper.Serialize(new {
						count = bps.Count,
						breakpoints = bps
					});
				}

				default:
					throw new McpException("Invalid action: " + action + ". Use 'set', 'remove', 'remove_all', or 'list'.");
			}
		}

		[McpServerTool(Name = "mesen_get_state", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get CPU or PPU state (registers, flags, program counter). Component: 'cpu' or 'ppu'.")]
		public static string GetState(
			[Description("Component: 'cpu' or 'ppu'")] string component,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws, Spc")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			switch(component.ToLowerInvariant()) {
				case "cpu":
					return GetCpuStateJson(cpu);

				case "ppu": {
					BaseState state = DebugApi.GetPpuState(cpu);
					return SerializePpuState(cpu, state);
				}

				default:
					throw new McpException("Invalid component: " + component + ". Use 'cpu' or 'ppu'.");
			}
		}

		[McpServerTool(Name = "mesen_set_program_counter", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Set the program counter to a specific address.")]
		public static string SetProgramCounter(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType,
			[Description("Address to set the PC to (decimal or 0x/$ hex)")] string address)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);
			uint addr = McpToolHelper.ParseAddress(address);

			DebugApi.SetProgramCounter(cpu, addr);
			return McpToolHelper.Serialize(new { success = true, pc = "$" + addr.ToString("X4") });
		}

		[McpServerTool(Name = "mesen_get_callstack", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the current call stack.")]
		public static string GetCallstack(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			StackFrameInfo[] callstack = DebugApi.GetCallstack(cpu);
			List<object> frames = new();
			foreach(StackFrameInfo frame in callstack) {
				frames.Add(new {
					source = "$" + frame.Source.ToString("X4"),
					target = "$" + frame.Target.ToString("X4"),
					returnAddress = "$" + frame.Return.ToString("X4"),
					flags = frame.Flags.ToString()
				});
			}

			return McpToolHelper.Serialize(new { callstack = frames });
		}

		[McpServerTool(Name = "mesen_evaluate_expression", ReadOnly = true, Destructive = false, OpenWorld = false),
		 Description("Evaluate a debugger expression (e.g. 'A + X', '$4016', '[0x2000]').")]
		public static string EvaluateExpression(
			[Description("Expression to evaluate")] string expression,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			EvalResultType resultType;
			Int64 result = DebugApi.EvaluateExpression(expression, cpu, out resultType, true);

			return McpToolHelper.Serialize(new {
				value = result,
				hex = "$" + result.ToString("X"),
				resultType = resultType.ToString()
			});
		}

		private static string SerializePpuState(CpuType cpu, BaseState state)
		{
			switch(cpu) {
				case CpuType.Nes: {
					NesPpuState s = (NesPpuState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Nes",
						scanline = s.Scanline,
						cycle = s.Cycle,
						frameCount = s.FrameCount,
						verticalBlank = s.StatusFlags.VerticalBlank,
						sprite0Hit = s.StatusFlags.Sprite0Hit,
						spriteOverflow = s.StatusFlags.SpriteOverflow,
						videoRamAddr = "$" + s.VideoRamAddr.ToString("X4"),
						scrollX = s.ScrollX
					});
				}
				case CpuType.Snes: {
					SnesPpuState s = (SnesPpuState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Snes",
						scanline = s.Scanline,
						cycle = s.Cycle,
						hClock = s.HClock,
						frameCount = s.FrameCount,
						forcedBlank = s.ForcedBlank,
						screenBrightness = s.ScreenBrightness,
						bgMode = s.BgMode,
						vramAddress = "$" + s.VramAddress.ToString("X4")
					});
				}
				case CpuType.Gameboy: {
					GbPpuState s = (GbPpuState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Gameboy",
						scanline = s.Scanline,
						cycle = s.Cycle,
						frameCount = s.FrameCount,
						ly = s.Ly,
						lyCompare = s.LyCompare,
						mode = s.Mode.ToString(),
						scrollX = s.ScrollX,
						scrollY = s.ScrollY,
						windowX = s.WindowX,
						windowY = s.WindowY,
						cgbEnabled = s.CgbEnabled
					});
				}
				case CpuType.Gba: {
					GbaPpuState s = (GbaPpuState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Gba",
						scanline = s.Scanline,
						cycle = s.Cycle,
						frameCount = s.FrameCount,
						bgMode = s.BgMode,
						forcedBlank = s.ForcedBlank
					});
				}
				case CpuType.Sms: {
					SmsVdpState s = (SmsVdpState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Sms",
						scanline = s.Scanline,
						cycle = s.Cycle,
						frameCount = s.FrameCount,
						vCounter = s.VCounter,
						horizontalScroll = s.HorizontalScroll,
						verticalScroll = s.VerticalScroll
					});
				}
				case CpuType.Pce: {
					PceVideoState s = (PceVideoState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Pce",
						scanline = s.Vdc.Scanline,
						hClock = s.Vdc.HClock,
						frameCount = s.Vdc.FrameCount,
						rcrCounter = s.Vdc.RcrCounter
					});
				}
				case CpuType.Ws: {
					WsPpuState s = (WsPpuState)state;
					return McpToolHelper.Serialize(new {
						cpuType = "Ws",
						scanline = s.Scanline,
						cycle = s.Cycle,
						frameCount = s.FrameCount,
						mode = s.Mode.ToString(),
						lcdEnabled = s.LcdEnabled
					});
				}
				default: {
					throw new McpException("PPU state not available for CPU type: " + cpu);
				}
			}
		}

		private static string GetCpuStateJson(CpuType cpu)
		{
			UInt32 pc = DebugApi.GetProgramCounter(cpu, false);

			Dictionary<string, object> result = new() {
				["cpuType"] = cpu.ToString(),
				["pc"] = "$" + pc.ToString("X4"),
				["paused"] = EmuApi.IsPaused()
			};

			// Get full register state
			try {
				result["registers"] = GetRegisters(cpu);
			} catch {
				// Register state not available
			}

			// Try to get disassembly at current PC
			try {
				int rowIndex = DebugApi.GetDisassemblyRowAddress(cpu, pc, 0);
				if(rowIndex >= 0) {
					CodeLineData[] lines = DebugApi.GetDisassemblyOutput(cpu, (uint)rowIndex, 10);
					List<string> disasm = new();
					foreach(CodeLineData line in lines) {
						if(line.Address >= 0 && !string.IsNullOrWhiteSpace(line.Text)) {
							string text = "$" + line.Address.ToString("X4") + ": " + line.Text;
							disasm.Add(text);
						}
					}
					result["disassembly"] = disasm;
				}
			} catch {
				// Disassembly not available
			}

			return McpToolHelper.Serialize(result);
		}

		private static Dictionary<string, object> GetRegisters(CpuType cpu)
		{
			switch(cpu) {
				case CpuType.Nes: {
					NesCpuState s = DebugApi.GetCpuState<NesCpuState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X2"),
						["X"] = "$" + s.X.ToString("X2"),
						["Y"] = "$" + s.Y.ToString("X2"),
						["SP"] = "$" + s.SP.ToString("X2"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["PS"] = "$" + s.PS.ToString("X2"),
						["flags"] = FormatFlags6502(s.PS)
					};
				}
				case CpuType.Snes:
				case CpuType.Sa1: {
					SnesCpuState s = DebugApi.GetCpuState<SnesCpuState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X4"),
						["X"] = "$" + s.X.ToString("X4"),
						["Y"] = "$" + s.Y.ToString("X4"),
						["SP"] = "$" + s.SP.ToString("X4"),
						["D"] = "$" + s.D.ToString("X4"),
						["DBR"] = "$" + s.DBR.ToString("X2"),
						["K"] = "$" + s.K.ToString("X2"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["PS"] = "$" + s.PS.ToString("X2"),
						["emulationMode"] = s.EmulationMode
					};
				}
				case CpuType.Gameboy: {
					GbCpuState s = DebugApi.GetCpuState<GbCpuState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X2"),
						["B"] = "$" + s.B.ToString("X2"),
						["C"] = "$" + s.C.ToString("X2"),
						["D"] = "$" + s.D.ToString("X2"),
						["E"] = "$" + s.E.ToString("X2"),
						["H"] = "$" + s.H.ToString("X2"),
						["L"] = "$" + s.L.ToString("X2"),
						["SP"] = "$" + s.SP.ToString("X4"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["flags"] = "$" + s.Flags.ToString("X2")
					};
				}
				case CpuType.Gba: {
					GbaCpuState s = DebugApi.GetCpuState<GbaCpuState>(cpu);
					Dictionary<string, object> regs = new();
					for(int i = 0; i < 16; i++) {
						regs["R" + i] = "$" + s.R[i].ToString("X8");
					}
					regs["mode"] = s.CPSR.Mode.ToString();
					regs["thumb"] = s.CPSR.Thumb;
					regs["N"] = s.CPSR.Negative;
					regs["Z"] = s.CPSR.Zero;
					regs["C"] = s.CPSR.Carry;
					regs["V"] = s.CPSR.Overflow;
					regs["I"] = s.CPSR.IrqDisable;
					regs["F"] = s.CPSR.FiqDisable;
					return regs;
				}
				case CpuType.Spc: {
					SpcState s = DebugApi.GetCpuState<SpcState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X2"),
						["X"] = "$" + s.X.ToString("X2"),
						["Y"] = "$" + s.Y.ToString("X2"),
						["SP"] = "$" + s.SP.ToString("X2"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["PS"] = "$" + s.PS.ToString("X2")
					};
				}
				case CpuType.Pce: {
					PceCpuState s = DebugApi.GetCpuState<PceCpuState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X2"),
						["X"] = "$" + s.X.ToString("X2"),
						["Y"] = "$" + s.Y.ToString("X2"),
						["SP"] = "$" + s.SP.ToString("X2"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["PS"] = "$" + s.PS.ToString("X2"),
						["flags"] = FormatFlags6502(s.PS)
					};
				}
				case CpuType.Sms: {
					SmsCpuState s = DebugApi.GetCpuState<SmsCpuState>(cpu);
					return new Dictionary<string, object> {
						["A"] = "$" + s.A.ToString("X2"),
						["B"] = "$" + s.B.ToString("X2"),
						["C"] = "$" + s.C.ToString("X2"),
						["D"] = "$" + s.D.ToString("X2"),
						["E"] = "$" + s.E.ToString("X2"),
						["H"] = "$" + s.H.ToString("X2"),
						["L"] = "$" + s.L.ToString("X2"),
						["Flags"] = "$" + s.Flags.ToString("X2"),
						["SP"] = "$" + s.SP.ToString("X4"),
						["PC"] = "$" + s.PC.ToString("X4"),
						["IX"] = "$" + ((s.IXH << 8) | s.IXL).ToString("X4"),
						["IY"] = "$" + ((s.IYH << 8) | s.IYL).ToString("X4"),
						["I"] = "$" + s.I.ToString("X2"),
						["R"] = "$" + s.R.ToString("X2"),
						["IM"] = s.IM,
						["IFF1"] = s.IFF1,
						["IFF2"] = s.IFF2
					};
				}
				case CpuType.Ws: {
					WsCpuState s = DebugApi.GetCpuState<WsCpuState>(cpu);
					return new Dictionary<string, object> {
						["AX"] = "$" + s.AX.ToString("X4"),
						["BX"] = "$" + s.BX.ToString("X4"),
						["CX"] = "$" + s.CX.ToString("X4"),
						["DX"] = "$" + s.DX.ToString("X4"),
						["SP"] = "$" + s.SP.ToString("X4"),
						["BP"] = "$" + s.BP.ToString("X4"),
						["SI"] = "$" + s.SI.ToString("X4"),
						["DI"] = "$" + s.DI.ToString("X4"),
						["CS"] = "$" + s.CS.ToString("X4"),
						["DS"] = "$" + s.DS.ToString("X4"),
						["ES"] = "$" + s.ES.ToString("X4"),
						["SS"] = "$" + s.SS.ToString("X4"),
						["IP"] = "$" + s.IP.ToString("X4"),
						["Flags"] = "$" + s.Flags.Get().ToString("X4")
					};
				}
				default: {
					return new Dictionary<string, object> {
						["error"] = "Register state not available for " + cpu
					};
				}
			}
		}

		private static string FormatFlags6502(byte ps)
		{
			return string.Concat(
				(ps & 0x80) != 0 ? "N" : "n",
				(ps & 0x40) != 0 ? "V" : "v",
				"-",
				(ps & 0x10) != 0 ? "B" : "b",
				(ps & 0x08) != 0 ? "D" : "d",
				(ps & 0x04) != 0 ? "I" : "i",
				(ps & 0x02) != 0 ? "Z" : "z",
				(ps & 0x01) != 0 ? "C" : "c"
			);
		}

	}
}
