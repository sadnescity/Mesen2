using Mesen.Interop;
using ModelContextProtocol;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Mesen.Mcp.Tools
{
	public static class McpToolHelper
	{
		private static volatile bool _debuggerInitialized;

		public static readonly JsonSerializerOptions JsonOptions = new() {
			TypeInfoResolver = new DefaultJsonTypeInfoResolver()
		};

		public static bool IsDebuggerReady()
		{
			return _debuggerInitialized && EmuApi.IsRunning();
		}

		public static void MarkDebuggerInitialized()
		{
			_debuggerInitialized = true;
		}

		public static string Serialize<T>(T value)
		{
			return JsonSerializer.Serialize(value, JsonOptions);
		}

		public static void EnsureDebuggerReady()
		{
			if(!_debuggerInitialized || !EmuApi.IsRunning()) {
				throw new McpException("Debugger not initialized. Call mesen_init_debugger first.");
			}
		}

		public static void EnsureRunning()
		{
			if(!EmuApi.IsRunning()) {
				throw new McpException("No ROM loaded.");
			}
		}

		public static uint ParseAddress(string address)
		{
			if(!TryParseAddress(address, out uint result)) {
				throw new McpException("Invalid address: " + address);
			}
			return result;
		}

		public static MemoryType ParseMemoryType(string memoryType)
		{
			if(!Enum.TryParse<MemoryType>(memoryType, true, out MemoryType memType)) {
				throw new McpException("Invalid memory type: " + memoryType + ". Call mesen_list_memory_types to see valid values.");
			}
			return memType;
		}

		public static CpuType ParseCpuType(string cpuType)
		{
			if(!Enum.TryParse<CpuType>(cpuType, true, out CpuType cpu)) {
				throw new McpException("Invalid CPU type: " + cpuType + ". Call mesen_list_cpu_types to see valid values.");
			}
			return cpu;
		}

		public static bool TryParseAddress(string address, out uint result)
		{
			address = address.Trim();
			if(address.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				return uint.TryParse(address.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out result);
			} else if(address.StartsWith("$")) {
				return uint.TryParse(address.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out result);
			} else {
				return uint.TryParse(address, out result);
			}
		}

		public static bool TryParseValue(string value, out byte result)
		{
			value = value.Trim();
			if(value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) {
				return byte.TryParse(value.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out result);
			} else if(value.StartsWith("$")) {
				return byte.TryParse(value.Substring(1), System.Globalization.NumberStyles.HexNumber, null, out result);
			} else {
				return byte.TryParse(value, out result);
			}
		}
	}
}
