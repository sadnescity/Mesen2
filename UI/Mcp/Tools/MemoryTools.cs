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
	public static class MemoryTools
	{
		[McpServerTool(Name = "mesen_read_memory", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Read memory from the emulator. Returns hex dump with optional ASCII representation. Use mesen_list_memory_types to see valid memory types for the current ROM.")]
		public static string ReadMemory(
			[Description("Start address (decimal or 0x/$ hex)")] string address,
			[Description("Number of bytes to read (max 4096)")] int length,
			[Description("Memory type (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType,
			[Description("Optional file path to save the raw binary dump")] string? outputFile = null)
		{
			McpToolHelper.EnsureDebuggerReady();
			uint addr = McpToolHelper.ParseAddress(address);
			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			length = Math.Min(length, 4096);
			Int32 memSize = DebugApi.GetMemorySize(memType);
			if(addr >= memSize) {
				throw new McpException($"Address ${addr:X4} out of range. Memory size: ${memSize:X4}");
			}
			length = (int)Math.Min(length, memSize - addr);

			byte[] data = DebugApi.GetMemoryValues(memType, addr, (uint)(addr + length - 1));

			if(!string.IsNullOrEmpty(outputFile)) {
				File.WriteAllBytes(outputFile, data);
				return McpToolHelper.Serialize(new {
					success = true,
					file = outputFile,
					startAddress = "$" + addr.ToString("X4"),
					length = data.Length
				});
			}

			// Format as hex dump with ASCII
			StringBuilder hexDump = new();
			for(int i = 0; i < data.Length; i += 16) {
				hexDump.Append($"${(addr + i):X4}: ");

				// Hex bytes
				for(int j = 0; j < 16 && (i + j) < data.Length; j++) {
					hexDump.Append($"{data[i + j]:X2} ");
				}

				// Pad if less than 16 bytes
				for(int j = data.Length - i; j < 16; j++) {
					hexDump.Append("   ");
				}

				hexDump.Append(" | ");

				// ASCII
				for(int j = 0; j < 16 && (i + j) < data.Length; j++) {
					byte b = data[i + j];
					hexDump.Append(b >= 0x20 && b < 0x7F ? (char)b : '.');
				}

				hexDump.AppendLine();
			}

			return McpToolHelper.Serialize(new {
				startAddress = "$" + addr.ToString("X4"),
				length = data.Length,
				hexDump = hexDump.ToString()
			});
		}

		[McpServerTool(Name = "mesen_write_memory", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Write data to emulator memory.")]
		public static string WriteMemory(
			[Description("Start address (decimal or 0x/$ hex)")] string address,
			[Description("Hex string of bytes to write (e.g. 'EAEA' for two NOPs)")] string hexData,
			[Description("Memory type (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType)
		{
			McpToolHelper.EnsureDebuggerReady();
			uint addr = McpToolHelper.ParseAddress(address);
			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			// Parse hex string to bytes
			hexData = hexData.Replace(" ", "").Replace("-", "");
			if(hexData.Length % 2 != 0) {
				throw new McpException("Hex data must have an even number of characters");
			}

			byte[] data = new byte[hexData.Length / 2];
			for(int i = 0; i < data.Length; i++) {
				if(!byte.TryParse(hexData.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out data[i])) {
					throw new McpException("Invalid hex data at position " + (i * 2));
				}
			}

			DebugApi.SetMemoryValues(memType, addr, data, data.Length);
			return McpToolHelper.Serialize(new {
				success = true,
				address = "$" + addr.ToString("X4"),
				bytesWritten = data.Length
			});
		}

		[McpServerTool(Name = "mesen_get_memory_size", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the size of a memory region.")]
		public static string GetMemorySize(
			[Description("Memory type (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType)
		{
			McpToolHelper.EnsureRunning();
			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			Int32 size = DebugApi.GetMemorySize(memType);
			return McpToolHelper.Serialize(new {
				memoryType = memoryType,
				size = size,
				sizeHex = "$" + size.ToString("X"),
				sizeKB = size / 1024.0
			});
		}

		[McpServerTool(Name = "mesen_search_memory", ReadOnly = true, Destructive = false, OpenWorld = false),
		 Description("Search memory for a hex pattern. Returns matching addresses. Use mesen_list_memory_types to see valid memory types.")]
		public static string SearchMemory(
			[Description("Hex pattern to search for (e.g. 'AD0020' or '03')")] string patternHex,
			[Description("Memory type to search in (call mesen_list_memory_types first to get valid values for the current ROM)")] string memoryType,
			[Description("Start address (default 0)")] string? startAddress = null,
			[Description("End address (default: end of memory, capped at 1MB range)")] string? endAddress = null,
			[Description("Maximum results to return (default 50)")] int maxResults = 50)
		{
			McpToolHelper.EnsureDebuggerReady();
			MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

			// Parse pattern
			patternHex = patternHex.Replace(" ", "").Replace("-", "");
			if(patternHex.Length % 2 != 0) {
				throw new McpException("Pattern must have an even number of hex characters");
			}

			byte[] pattern = new byte[patternHex.Length / 2];
			for(int i = 0; i < pattern.Length; i++) {
				if(!byte.TryParse(patternHex.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out pattern[i])) {
					throw new McpException("Invalid hex at position " + (i * 2));
				}
			}

			Int32 memSize = DebugApi.GetMemorySize(memType);
			uint start = 0;
			uint end = (uint)(memSize - 1);

			if(startAddress != null) start = McpToolHelper.ParseAddress(startAddress);
			if(endAddress != null) end = McpToolHelper.ParseAddress(endAddress);

			end = Math.Min(end, (uint)(memSize - 1));

			// Read memory range and search
			byte[] memory = DebugApi.GetMemoryValues(memType, start, end);
			List<string> matches = new();

			for(int i = 0; i <= memory.Length - pattern.Length && matches.Count < maxResults; i++) {
				bool found = true;
				for(int j = 0; j < pattern.Length; j++) {
					if(memory[i + j] != pattern[j]) {
						found = false;
						break;
					}
				}
				if(found) {
					matches.Add("$" + (start + i).ToString("X4"));
				}
			}

			return McpToolHelper.Serialize(new {
				pattern = patternHex,
				matchCount = matches.Count,
				addresses = matches,
				searchedRange = "$" + start.ToString("X4") + " - $" + end.ToString("X4")
			});
		}

		[McpServerTool(Name = "mesen_freeze_address", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Freeze or unfreeze a memory address range (prevent/allow game writes).")]
		public static string FreezeAddress(
			[Description("Start address (decimal or 0x/$ hex)")] string startAddress,
			[Description("End address (same as start for single byte)")] string endAddress,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType,
			[Description("True to freeze (prevent writes), false to unfreeze")] bool freeze)
		{
			McpToolHelper.EnsureDebuggerReady();
			uint start = McpToolHelper.ParseAddress(startAddress);
			uint end = McpToolHelper.ParseAddress(endAddress);
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			DebugApi.UpdateFrozenAddresses(cpu, start, end, freeze);
			return McpToolHelper.Serialize(new {
				success = true,
				frozen = freeze,
				range = "$" + start.ToString("X4") + " - $" + end.ToString("X4")
			});
		}

		[McpServerTool(Name = "mesen_get_address_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Convert between CPU (relative) and absolute (physical) addresses.")]
		public static string GetAddressInfo(
			[Description("Address to convert (decimal or 0x/$ hex)")] string address,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();
			uint addr = McpToolHelper.ParseAddress(address);
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			AddressInfo relAddr = new AddressInfo() {
				Address = (int)addr,
				Type = cpu.ToMemoryType()
			};

			AddressInfo absAddr = DebugApi.GetAbsoluteAddress(relAddr);

			return McpToolHelper.Serialize(new {
				relativeAddress = "$" + addr.ToString("X4"),
				absoluteAddress = absAddr.Address >= 0 ? "$" + absAddr.Address.ToString("X4") : "unmapped",
				absoluteMemoryType = absAddr.Address >= 0 ? absAddr.Type.ToString() : "none"
			});
		}

		[McpServerTool(Name = "mesen_memory_access_counts", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Get or reset memory access counters. Action: 'get' (returns read/write/exec counts) or 'reset' (clears all counters).")]
		public static string MemoryAccessCounts(
			[Description("Action: 'get' or 'reset'")] string action,
			[Description("Start address for get (decimal or 0x/$ hex)")] string? address = null,
			[Description("Number of bytes for get (max 256)")] int length = 256,
			[Description("Memory type for get")] string? memoryType = null)
		{
			McpToolHelper.EnsureDebuggerReady();

			switch(action.ToLowerInvariant()) {
				case "reset":
					DebugApi.ResetMemoryAccessCounts();
					return McpToolHelper.Serialize(new { success = true });

				case "get":
					if(address == null) {
						throw new McpException("Address is required for 'get' action.");
					}
					if(memoryType == null) {
						throw new McpException("Memory type is required for 'get' action.");
					}

					uint addr = McpToolHelper.ParseAddress(address);
					MemoryType memType = McpToolHelper.ParseMemoryType(memoryType);

					length = Math.Min(length, 256);
					Int32 memSize = DebugApi.GetMemorySize(memType);
					if(addr >= memSize) {
						throw new McpException($"Address ${addr:X4} out of range. Memory size: ${memSize:X4}");
					}
					length = (int)Math.Min(length, memSize - addr);

					AddressCounters[] counts = DebugApi.GetMemoryAccessCounts(addr, (uint)length, memType);

					List<object> entries = new();
					for(int i = 0; i < counts.Length; i++) {
						AddressCounters c = counts[i];
						if(c.ReadCounter > 0 || c.WriteCounter > 0 || c.ExecCounter > 0) {
							entries.Add(new {
								address = "$" + (addr + i).ToString("X4"),
								readCount = c.ReadCounter,
								writeCount = c.WriteCounter,
								execCount = c.ExecCounter
							});
						}
					}

					return McpToolHelper.Serialize(new {
						startAddress = "$" + addr.ToString("X4"),
						length = length,
						activeAddresses = entries.Count,
						counters = entries
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'get' or 'reset'.");
			}
		}
	}
}
