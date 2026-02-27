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
	public static class TraceTools
	{
		[McpServerTool(Name = "mesen_set_trace_options", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Configure trace logging options for a CPU. Must be called before reading execution trace.")]
		public static string SetTraceOptions(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws, Spc, Sa1, Gsu, Cx4")] string cpuType,
			[Description("Enable tracing for this CPU (default true)")] bool enabled = true,
			[Description("Trace format string (e.g. '[Disassembly][Align,24] A:[A,2h] X:[X,2h]'). Leave empty for default.")] string? format = null,
			[Description("Condition expression to filter trace (e.g. 'A == $42'). Empty = no filter.")] string? condition = null,
			[Description("Use labels in trace output (default true)")] bool useLabels = true,
			[Description("Indent code based on call depth (default false)")] bool indentCode = false)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			string traceFormat = format ?? GetDefaultTraceFormat(cpu);

			InteropTraceLoggerOptions options = new() {
				Enabled = enabled,
				UseLabels = useLabels,
				IndentCode = indentCode,
				Format = Encoding.UTF8.GetBytes(traceFormat),
				Condition = Encoding.UTF8.GetBytes(condition ?? "")
			};

			Array.Resize(ref options.Format, 1000);
			Array.Resize(ref options.Condition, 1000);

			DebugApi.SetTraceOptions(cpu, options);

			return McpToolHelper.Serialize(new {
				success = true,
				cpuType = cpuType,
				enabled = enabled,
				format = traceFormat
			});
		}

		[McpServerTool(Name = "mesen_get_execution_trace", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the execution trace (last N executed instructions with register state).")]
		public static string GetExecutionTrace(
			[Description("Number of trace rows to retrieve (max 30000)")] int count,
			[Description("Optional CPU type filter. If omitted, returns all CPU types.")] string? cpuType = null,
			[Description("Optional file path to save trace output")] string? outputFile = null)
		{
			McpToolHelper.EnsureDebuggerReady();

			CpuType? filterCpu = null;
			if(cpuType != null) {
				filterCpu = McpToolHelper.ParseCpuType(cpuType);
			}

			count = Math.Min(count, DebugApi.TraceLogBufferSize);
			TraceRow[] rows = DebugApi.GetExecutionTrace(0, (uint)count);

			StringBuilder sb = new();
			List<object> traceLines = new();

			foreach(TraceRow row in rows) {
				if(filterCpu.HasValue && row.Type != filterCpu.Value) {
					continue;
				}

				string output = row.GetOutput();
				string byteCode = row.GetByteCodeStr();
				string pc = "$" + row.ProgramCounter.ToString("X4");

				sb.Append("[").Append(row.Type).Append("] ").Append(pc).Append(": ").Append(output).AppendLine();

				traceLines.Add(new {
					cpuType = row.Type.ToString(),
					pc = pc,
					byteCode = byteCode,
					output = output
				});
			}

			if(!string.IsNullOrEmpty(outputFile)) {
				File.WriteAllText(outputFile, sb.ToString());
				return McpToolHelper.Serialize(new {
					success = true,
					file = outputFile,
					lineCount = traceLines.Count
				});
			}

			return McpToolHelper.Serialize(new {
				lineCount = traceLines.Count,
				trace = traceLines
			});
		}

		[McpServerTool(Name = "mesen_clear_execution_trace", ReadOnly = false, Destructive = true, OpenWorld = false),
		 Description("Clear the execution trace buffer.")]
		public static string ClearExecutionTrace()
		{
			McpToolHelper.EnsureDebuggerReady();

			DebugApi.ClearExecutionTrace();
			return McpToolHelper.Serialize(new { success = true });
		}

		[McpServerTool(Name = "mesen_trace_file", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Start or stop logging execution trace to a file. Use action 'start' to begin logging, 'stop' to end.")]
		public static string TraceFile(
			[Description("Action: 'start' or 'stop'")] string action,
			[Description("File path for trace log (required for start)")] string? filepath = null)
		{
			McpToolHelper.EnsureDebuggerReady();

			switch(action.ToLowerInvariant()) {
				case "start":
					if(string.IsNullOrEmpty(filepath)) {
						throw new McpException("File path is required when action is 'start'.");
					}
					DebugApi.StartLogTraceToFile(filepath);
					return McpToolHelper.Serialize(new {
						success = true,
						action = "start",
						file = filepath
					});

				case "stop":
					DebugApi.StopLogTraceToFile();
					return McpToolHelper.Serialize(new {
						success = true,
						action = "stop"
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'start' or 'stop'.");
			}
		}

		[McpServerTool(Name = "mesen_profiler", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Get or reset profiler data for a CPU. Use action 'get' for per-function statistics (call count, cycles, min/max), or 'reset' to clear.")]
		public static string Profiler(
			[Description("Action: 'get' or 'reset'")] string action,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws, Spc, Sa1")] string cpuType,
			[Description("Optional file path to save profiler data (only used for 'get')")] string? outputFile = null)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			switch(action.ToLowerInvariant()) {
				case "get": {
					ProfiledFunction[] buffer = new ProfiledFunction[100000];
					int count = DebugApi.GetProfilerData(cpu, ref buffer);

					List<object> functions = new();
					StringBuilder sb = new();

					for(int i = 0; i < count; i++) {
						ProfiledFunction f = buffer[i];
						if(f.CallCount == 0) {
							continue;
						}

						string addrStr = "$" + f.Address.Address.ToString("X4");
						string memType = f.Address.Type.ToString();

						functions.Add(new {
							address = addrStr,
							memoryType = memType,
							callCount = f.CallCount,
							inclusiveCycles = f.InclusiveCycles,
							exclusiveCycles = f.ExclusiveCycles,
							minCycles = f.MinCycles,
							maxCycles = f.MaxCycles,
							avgCycles = f.GetAvgCycles(),
							flags = f.Flags.ToString()
						});

						sb.Append(addrStr).Append(" (").Append(memType).Append(")")
							.Append("  calls=").Append(f.CallCount)
							.Append("  incl=").Append(f.InclusiveCycles)
							.Append("  excl=").Append(f.ExclusiveCycles)
							.Append("  avg=").Append(f.GetAvgCycles())
							.AppendLine();
					}

					if(!string.IsNullOrEmpty(outputFile)) {
						File.WriteAllText(outputFile, sb.ToString());
						return McpToolHelper.Serialize(new {
							success = true,
							file = outputFile,
							functionCount = functions.Count
						});
					}

					return McpToolHelper.Serialize(new {
						functionCount = functions.Count,
						functions = functions
					});
				}

				case "reset":
					DebugApi.ResetProfiler(cpu);
					return McpToolHelper.Serialize(new { success = true });

				default:
					throw new McpException("Invalid action: " + action + ". Use 'get' or 'reset'.");
			}
		}

		private static string GetDefaultTraceFormat(CpuType cpuType)
		{
			return cpuType switch {
				CpuType.Snes or CpuType.Sa1 =>
					"[Disassembly][Align,24] A:[A,4h] X:[X,4h] Y:[Y,4h] S:[SP,4h] D:[D,4h] DB:[DB,2h] P:[P,8]",
				CpuType.Nes =>
					"[Disassembly][Align,24] A:[A,2h] X:[X,2h] Y:[Y,2h] S:[SP,2h] P:[P,8]",
				CpuType.Gameboy =>
					"[Disassembly][Align,24] A:[A,2h] B:[B,2h] C:[C,2h] D:[D,2h] E:[E,2h] F:[F,2h] H:[H,2h] L:[L,2h] SP:[SP,4h]",
				CpuType.Gba =>
					"[Disassembly][Align,42] ",
				_ =>
					"[Disassembly][Align,24] "
			};
		}
	}
}
