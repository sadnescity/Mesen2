using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Mesen.Mcp.Models
{
	[JsonSourceGenerationOptions(
		PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
		UseStringEnumConverter = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	)]

	// Common
	[JsonSerializable(typeof(SuccessResponse))]
	[JsonSerializable(typeof(SuccessActionResponse))]
	[JsonSerializable(typeof(SuccessActionFileResponse))]
	[JsonSerializable(typeof(FileOutputResponse))]
	[JsonSerializable(typeof(PausedStatusResponse))]
	[JsonSerializable(typeof(RecordingStatusResponse))]

	// Emulator Config
	[JsonSerializable(typeof(VersionResponse))]
	[JsonSerializable(typeof(ScreenInfoResponse))]
	[JsonSerializable(typeof(ShortcutListResponse))]
	[JsonSerializable(typeof(ShortcutExecuteResponse))]
	[JsonSerializable(typeof(ResetResponse))]
	[JsonSerializable(typeof(SetFlagResponse))]
	[JsonSerializable(typeof(TimingInfoResponse))]
	[JsonSerializable(typeof(LogResponse))]
	[JsonSerializable(typeof(MemoryTypeListResponse))]
	[JsonSerializable(typeof(CpuTypeListResponse))]

	// Emulator Tools
	[JsonSerializable(typeof(LoadRomResponse))]
	[JsonSerializable(typeof(RomInfoDetail))]
	[JsonSerializable(typeof(PlaybackResponse))]
	[JsonSerializable(typeof(FullStatusResponse))]
	[JsonSerializable(typeof(ScreenshotSavedResponse))]
	[JsonSerializable(typeof(SaveStateSlotResponse))]

	// Debug Execution
	[JsonSerializable(typeof(CpuStateResponse))]
	[JsonSerializable(typeof(BreakpointSetResponse))]
	[JsonSerializable(typeof(BreakpointRemoveResponse))]
	[JsonSerializable(typeof(BreakpointListResponse))]
	[JsonSerializable(typeof(SetPcResponse))]
	[JsonSerializable(typeof(CallstackResponse))]
	[JsonSerializable(typeof(EvalExpressionResponse))]
	[JsonSerializable(typeof(NesPpuStateResponse))]
	[JsonSerializable(typeof(SnesPpuStateResponse))]
	[JsonSerializable(typeof(GbPpuStateResponse))]
	[JsonSerializable(typeof(GbaPpuStateResponse))]
	[JsonSerializable(typeof(SmsPpuStateResponse))]
	[JsonSerializable(typeof(PcePpuStateResponse))]
	[JsonSerializable(typeof(WsPpuStateResponse))]

	// Disassembly
	[JsonSerializable(typeof(DisassemblyResponse))]
	[JsonSerializable(typeof(FindOccurrencesResponse))]
	[JsonSerializable(typeof(AssembleResponse))]
	[JsonSerializable(typeof(LabelSetResponse))]
	[JsonSerializable(typeof(CdlStatisticsResponse))]
	[JsonSerializable(typeof(CdlFunctionsResponse))]
	[JsonSerializable(typeof(MarkBytesResponse))]

	// Memory
	[JsonSerializable(typeof(MemoryDumpFileResponse))]
	[JsonSerializable(typeof(ReadMemoryResponse))]
	[JsonSerializable(typeof(WriteMemoryResponse))]
	[JsonSerializable(typeof(MemorySizeResponse))]
	[JsonSerializable(typeof(SearchMemoryResponse))]
	[JsonSerializable(typeof(FreezeAddressResponse))]
	[JsonSerializable(typeof(AddressInfoResponse))]
	[JsonSerializable(typeof(MemoryAccessCountsResponse))]

	// Recording
	[JsonSerializable(typeof(VideoRecordStartResponse))]
	[JsonSerializable(typeof(MovieRecordResponse))]
	[JsonSerializable(typeof(MovieStatusResponse))]

	// ROM Hacking
	[JsonSerializable(typeof(SaveRomResponse))]
	[JsonSerializable(typeof(NesRomHeaderResponse))]
	[JsonSerializable(typeof(SnesRomHeaderResponse))]
	[JsonSerializable(typeof(PaletteInfoResponse))]
	[JsonSerializable(typeof(SetPaletteColorResponse))]
	[JsonSerializable(typeof(GetTilePixelResponse))]
	[JsonSerializable(typeof(SetTilePixelResponse))]
	[JsonSerializable(typeof(RunLuaScriptResponse))]
	[JsonSerializable(typeof(RemoveLuaScriptResponse))]
	[JsonSerializable(typeof(SetCheatsResponse))]

	// Sprites & Tilemaps
	[JsonSerializable(typeof(SpriteListResponse))]
	[JsonSerializable(typeof(TilemapInfoResponse))]
	[JsonSerializable(typeof(TilemapTileInfoResponse))]

	// Text Search
	[JsonSerializable(typeof(RelativeSearchResponse))]
	[JsonSerializable(typeof(LoadTblResponse))]
	[JsonSerializable(typeof(SearchTextResponse))]
	[JsonSerializable(typeof(DecodeTextResponse))]
	[JsonSerializable(typeof(TblInfoResponse))]

	// Trace / Profiler
	[JsonSerializable(typeof(SetTraceOptionsResponse))]
	[JsonSerializable(typeof(ExecutionTraceResponse))]
	[JsonSerializable(typeof(ProfilerDataResponse))]

	// History
	[JsonSerializable(typeof(HistoryEnabledResponse))]
	[JsonSerializable(typeof(HistoryStateResponse))]
	[JsonSerializable(typeof(HistorySetOptionsResponse))]
	[JsonSerializable(typeof(HistoryNavigateResponse))]
	[JsonSerializable(typeof(HistorySaveStateResponse))]
	[JsonSerializable(typeof(HistorySaveMovieResponse))]

	// Input
	[JsonSerializable(typeof(KeySetResponse))]
	[JsonSerializable(typeof(PressedKeysResponse))]
	[JsonSerializable(typeof(KeyInfoResponse))]
	[JsonSerializable(typeof(MouseResponse))]
	[JsonSerializable(typeof(AvailablePortsResponse))]
	[JsonSerializable(typeof(InputOverrideResponse))]
	[JsonSerializable(typeof(DisableKeysResponse))]
	[JsonSerializable(typeof(HasControlDeviceResponse))]

	public partial class McpJsonContext : JsonSerializerContext { }
}
