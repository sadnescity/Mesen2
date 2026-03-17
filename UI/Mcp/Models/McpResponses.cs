using System.Collections.Generic;

namespace Mesen.Mcp.Models
{
	// ── Common / Shared ──────────────────────────────────────────

	public record SuccessResponse { public bool Success { get; init; } }
	public record SuccessActionResponse { public bool Success { get; init; } public string Action { get; init; } = ""; }
	public record SuccessActionFileResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public string File { get; init; } = ""; }
	public record FileOutputResponse { public bool Success { get; init; } public string File { get; init; } = ""; public int LineCount { get; init; } }
	public record PausedStatusResponse { public bool Running { get; init; } public bool Paused { get; init; } }
	public record RecordingStatusResponse { public bool Recording { get; init; } }

	// ── Emulator Config ──────────────────────────────────────────

	public record VersionResponse { public string Version { get; init; } = ""; public string BuildDate { get; init; } = ""; }
	public record ScreenInfoResponse { public uint Width { get; init; } public uint Height { get; init; } public double AspectRatio { get; init; } }
	public record ShortcutListResponse { public int Count { get; init; } public List<string> Shortcuts { get; init; } = new(); }
	public record ShortcutExecuteResponse { public bool Success { get; init; } public string Shortcut { get; init; } = ""; }
	public record ResetResponse { public bool Success { get; init; } public string Type { get; init; } = ""; }
	public record SetFlagResponse { public bool Success { get; init; } public string Flag { get; init; } = ""; public bool Enabled { get; init; } }
	public record TimingInfoResponse { public double Fps { get; init; } public uint FrameCount { get; init; } public ulong MasterClock { get; init; } public double MasterClockRate { get; init; } }
	public record LogResponse { public string Log { get; init; } = ""; }
	public record MemoryTypeEntry { public string Name { get; init; } = ""; public int Size { get; init; } public string SizeHex { get; init; } = ""; public double SizeKB { get; init; } }
	public record MemoryTypeListResponse { public int Count { get; init; } public List<MemoryTypeEntry> MemoryTypes { get; init; } = new(); }
	public record CpuTypeEntry { public string Name { get; init; } = ""; public bool IsMain { get; init; } public string MemoryType { get; init; } = ""; public int MemorySize { get; init; } }
	public record CpuTypeListResponse { public string ConsoleType { get; init; } = ""; public string MainCpu { get; init; } = ""; public List<CpuTypeEntry> AvailableCpus { get; init; } = new(); }

	// ── Emulator Tools ───────────────────────────────────────────

	public record RomInfoDetail { public string Name { get; init; } = ""; public string Format { get; init; } = ""; public string ConsoleType { get; init; } = ""; public string Hash { get; init; } = ""; }
	public record LoadRomResponse { public bool Success { get; init; } public RomInfoDetail RomInfo { get; init; } = new(); }
	public record PlaybackResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public bool Paused { get; init; } }
	public record FullStatusResponse { public bool Running { get; init; } public bool Paused { get; init; } public string ConsoleType { get; init; } = ""; public string RomName { get; init; } = ""; public double Fps { get; init; } public uint FrameCount { get; init; } public ulong MasterClock { get; init; } }
	public record ScreenshotSavedResponse { public string SavedTo { get; init; } = ""; public int Size { get; init; } }
	public record SaveStateSlotResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public int Slot { get; init; } }

	// ── Debug Execution ──────────────────────────────────────────

	public record CpuStateResponse { public string CpuType { get; init; } = ""; public string Pc { get; init; } = ""; public bool Paused { get; init; } public Dictionary<string, string>? Registers { get; init; } }

	public record BreakpointDetail { public string StartAddress { get; init; } = ""; public string EndAddress { get; init; } = ""; public string Type { get; init; } = ""; public string MemoryType { get; init; } = ""; public string CpuType { get; init; } = ""; public string Condition { get; init; } = ""; }
	public record BreakpointSetResponse { public bool Success { get; init; } public BreakpointDetail Breakpoint { get; init; } = new(); }

	public record RemovedBreakpointInfo { public string StartAddress { get; init; } = ""; public string EndAddress { get; init; } = ""; public string MemoryType { get; init; } = ""; }
	public record BreakpointRemoveResponse { public bool Success { get; init; } public RemovedBreakpointInfo Removed { get; init; } = new(); }

	public record BreakpointListEntry { public int Index { get; init; } public bool Enabled { get; init; } public string StartAddress { get; init; } = ""; public string EndAddress { get; init; } = ""; public string MemoryType { get; init; } = ""; public string CpuType { get; init; } = ""; public bool BreakOnExec { get; init; } public bool BreakOnRead { get; init; } public bool BreakOnWrite { get; init; } public string Condition { get; init; } = ""; }
	public record BreakpointListResponse { public int Count { get; init; } public List<BreakpointListEntry> Breakpoints { get; init; } = new(); }

	public record SetPcResponse { public bool Success { get; init; } public string Pc { get; init; } = ""; }

	public record CallstackFrameEntry { public string Source { get; init; } = ""; public string Target { get; init; } = ""; public string ReturnAddress { get; init; } = ""; public string Flags { get; init; } = ""; }
	public record CallstackResponse { public List<CallstackFrameEntry> Callstack { get; init; } = new(); }

	public record EvalExpressionResponse { public long Value { get; init; } public string Hex { get; init; } = ""; public string ResultType { get; init; } = ""; }

	// PPU state per console
	public record NesPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public uint Cycle { get; init; } public uint FrameCount { get; init; } public bool VerticalBlank { get; init; } public bool Sprite0Hit { get; init; } public bool SpriteOverflow { get; init; } public string VideoRamAddr { get; init; } = ""; public byte ScrollX { get; init; } }
	public record SnesPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int Cycle { get; init; } public int HClock { get; init; } public uint FrameCount { get; init; } public bool ForcedBlank { get; init; } public byte ScreenBrightness { get; init; } public byte BgMode { get; init; } public string VramAddress { get; init; } = ""; }
	public record GbPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int Cycle { get; init; } public uint FrameCount { get; init; } public byte Ly { get; init; } public byte LyCompare { get; init; } public string Mode { get; init; } = ""; public byte ScrollX { get; init; } public byte ScrollY { get; init; } public byte WindowX { get; init; } public byte WindowY { get; init; } public bool CgbEnabled { get; init; } }
	public record GbaPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int Cycle { get; init; } public uint FrameCount { get; init; } public byte BgMode { get; init; } public bool ForcedBlank { get; init; } }
	public record SmsPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int Cycle { get; init; } public uint FrameCount { get; init; } public int VCounter { get; init; } public byte HorizontalScroll { get; init; } public byte VerticalScroll { get; init; } }
	public record PcePpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int HClock { get; init; } public uint FrameCount { get; init; } public int RcrCounter { get; init; } }
	public record WsPpuStateResponse { public string CpuType { get; init; } = ""; public int Scanline { get; init; } public int Cycle { get; init; } public uint FrameCount { get; init; } public string Mode { get; init; } = ""; public bool LcdEnabled { get; init; } }

	// ── Disassembly ──────────────────────────────────────────────

	public record DisassemblyLineEntry { public string Address { get; init; } = ""; public string ByteCode { get; init; } = ""; public string Text { get; init; } = ""; public string Comment { get; init; } = ""; }
	public record DisassemblyResponse { public string StartAddress { get; init; } = ""; public int LineCount { get; init; } public List<DisassemblyLineEntry> Lines { get; init; } = new(); }

	public record FindOccurrenceEntry { public string Address { get; init; } = ""; public string Text { get; init; } = ""; public string ByteCode { get; init; } = ""; }
	public record FindOccurrencesResponse { public string SearchString { get; init; } = ""; public int MatchCount { get; init; } public List<FindOccurrenceEntry> Matches { get; init; } = new(); }

	public record AssembleResponse { public bool Success { get; init; } public string StartAddress { get; init; } = ""; public int ByteCount { get; init; } public string HexBytes { get; init; } = ""; public List<string> Errors { get; init; } = new(); }

	public record LabelSetResponse { public bool Success { get; init; } public string Address { get; init; } = ""; public string Label { get; init; } = ""; public string Comment { get; init; } = ""; }

	public record CdlStatisticsResponse { public uint TotalBytes { get; init; } public uint CodeBytes { get; init; } public uint DataBytes { get; init; } public uint UnknownBytes { get; init; } public double CodePercent { get; init; } public double DataPercent { get; init; } public double UnknownPercent { get; init; } public uint JumpTargetCount { get; init; } public uint FunctionCount { get; init; } public uint DrawnChrBytes { get; init; } public uint TotalChrBytes { get; init; } }

	public record CdlFunctionsResponse { public int FunctionCount { get; init; } public List<string> Addresses { get; init; } = new(); }

	public record MarkBytesResponse { public bool Success { get; init; } public string Range { get; init; } = ""; public string Flags { get; init; } = ""; }

	// ── Memory ───────────────────────────────────────────────────

	public record MemoryDumpFileResponse { public bool Success { get; init; } public string File { get; init; } = ""; public string StartAddress { get; init; } = ""; public int Length { get; init; } }
	public record ReadMemoryResponse { public string StartAddress { get; init; } = ""; public int Length { get; init; } public string HexDump { get; init; } = ""; }
	public record WriteMemoryResponse { public bool Success { get; init; } public string Address { get; init; } = ""; public int BytesWritten { get; init; } }
	public record MemorySizeResponse { public string MemoryType { get; init; } = ""; public int Size { get; init; } public string SizeHex { get; init; } = ""; public double SizeKB { get; init; } }
	public record SearchMemoryResponse { public string Pattern { get; init; } = ""; public int MatchCount { get; init; } public List<string> Addresses { get; init; } = new(); public string SearchedRange { get; init; } = ""; }
	public record FreezeAddressResponse { public bool Success { get; init; } public bool Frozen { get; init; } public string Range { get; init; } = ""; }
	public record AddressInfoResponse { public string RelativeAddress { get; init; } = ""; public string AbsoluteAddress { get; init; } = ""; public string AbsoluteMemoryType { get; init; } = ""; }
	public record AccessCountEntry { public string Address { get; init; } = ""; public ulong ReadCount { get; init; } public ulong WriteCount { get; init; } public ulong ExecCount { get; init; } }
	public record MemoryAccessCountsResponse { public string StartAddress { get; init; } = ""; public int Length { get; init; } public int ActiveAddresses { get; init; } public List<AccessCountEntry> Counters { get; init; } = new(); }

	// ── Recording ────────────────────────────────────────────────

	public record VideoRecordStartResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public string File { get; init; } = ""; public string Codec { get; init; } = ""; }
	public record MovieRecordResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public string File { get; init; } = ""; public string RecordFrom { get; init; } = ""; }
	public record MovieStatusResponse { public bool Playing { get; init; } public bool Recording { get; init; } }

	// ── ROM Hacking ──────────────────────────────────────────────

	public record SaveRomResponse { public bool Success { get; init; } public string File { get; init; } = ""; public bool SaveAsIps { get; init; } }

	public record NesRomHeaderResponse { public string Format { get; init; } = ""; public int Mapper { get; init; } public int? SubMapper { get; init; } public int PrgRomSize { get; init; } public int PrgRomSizeKB { get; init; } public int ChrRomSize { get; init; } public int ChrRomSizeKB { get; init; } public string Mirroring { get; init; } = ""; public bool Battery { get; init; } public bool Trainer { get; init; } public string RawBytes { get; init; } = ""; }

	public record SnesRomHeaderResponse { public string Title { get; init; } = ""; public string MapMode { get; init; } = ""; public string MapModeName { get; init; } = ""; public bool FastRom { get; init; } public string RomType { get; init; } = ""; public string? RomTypeName { get; init; } public int RomSizeKB { get; init; } public int SramSizeKB { get; init; } public string Region { get; init; } = ""; public string DestinationCode { get; init; } = ""; public string? DeveloperId { get; init; } public string? MakerCode { get; init; } public string? GameCode { get; init; } public string Version { get; init; } = ""; public string Checksum { get; init; } = ""; public string ChecksumComplement { get; init; } = ""; public bool ChecksumValid { get; init; } public string HeaderOffset { get; init; } = ""; public string RawBytes { get; init; } = ""; }

	public record PaletteColorEntry { public int Index { get; init; } public string Rgb { get; init; } = ""; public string Raw { get; init; } = ""; }
	public record PaletteInfoResponse { public uint ColorCount { get; init; } public uint BgColorCount { get; init; } public uint SpriteColorCount { get; init; } public uint ColorsPerPalette { get; init; } public string RawFormat { get; init; } = ""; public List<PaletteColorEntry> Colors { get; init; } = new(); }

	public record SetPaletteColorResponse { public bool Success { get; init; } public int ColorIndex { get; init; } public string Color { get; init; } = ""; }
	public record GetTilePixelResponse { public string TileAddress { get; init; } = ""; public int X { get; init; } public int Y { get; init; } public int ColorIndex { get; init; } }
	public record SetTilePixelResponse { public bool Success { get; init; } public string TileAddress { get; init; } = ""; public int X { get; init; } public int Y { get; init; } public int ColorIndex { get; init; } }

	public record RunLuaScriptResponse { public bool Success { get; init; } public int ScriptId { get; init; } public bool Persistent { get; init; } public string Log { get; init; } = ""; }
	public record RemoveLuaScriptResponse { public bool Success { get; init; } public int ScriptId { get; init; } public string Log { get; init; } = ""; }

	public record SetCheatsResponse { public bool Success { get; init; } public int CheatsApplied { get; init; } public List<string> Errors { get; init; } = new(); }

	// ── Sprites & Tilemaps ───────────────────────────────────────

	public record SpriteEntry { public int Index { get; init; } public int X { get; init; } public int Y { get; init; } public int RawX { get; init; } public int RawY { get; init; } public int Width { get; init; } public int Height { get; init; } public string TileIndex { get; init; } = ""; public string TileAddress { get; init; } = ""; public int Palette { get; init; } public string PaletteAddress { get; init; } = ""; public int Bpp { get; init; } public string Format { get; init; } = ""; public string Priority { get; init; } = ""; public string Mode { get; init; } = ""; public string Visibility { get; init; } = ""; public string HorizontalMirror { get; init; } = ""; public string VerticalMirror { get; init; } = ""; }
	public record SpriteListResponse { public int SpriteCount { get; init; } public uint ScreenWidth { get; init; } public uint ScreenHeight { get; init; } public List<SpriteEntry> Sprites { get; init; } = new(); }

	public record TilemapInfoResponse { public int Layer { get; init; } public uint Width { get; init; } public uint Height { get; init; } public uint TileWidth { get; init; } public uint TileHeight { get; init; } public uint Bpp { get; init; } public string Format { get; init; } = ""; public string Mirroring { get; init; } = ""; public uint ScrollX { get; init; } public uint ScrollY { get; init; } public uint ScrollWidth { get; init; } public uint ScrollHeight { get; init; } public uint RowCount { get; init; } public uint ColumnCount { get; init; } public string TilemapAddress { get; init; } = ""; public string TilesetAddress { get; init; } = ""; public int Priority { get; init; } }

	public record TilemapTileInfoResponse { public int Row { get; init; } public int Column { get; init; } public int Width { get; init; } public int Height { get; init; } public int TileIndex { get; init; } public string TileMapAddress { get; init; } = ""; public string TileAddress { get; init; } = ""; public int PaletteIndex { get; init; } public string PaletteAddress { get; init; } = ""; public int BasePaletteIndex { get; init; } public string AttributeAddress { get; init; } = ""; public string AttributeData { get; init; } = ""; public string HorizontalMirroring { get; init; } = ""; public string VerticalMirroring { get; init; } = ""; public string HighPriority { get; init; } = ""; }

	// ── Text Search ──────────────────────────────────────────────

	public record RelativeSearchMatchEntry { public string Address { get; init; } = ""; public string FirstByte { get; init; } = ""; public int BaseOffset { get; init; } public Dictionary<string, string> InferredTable { get; init; } = new(); }
	public record RelativeSearchResponse { public string SearchText { get; init; } = ""; public string SignatureDescription { get; init; } = ""; public int MatchCount { get; init; } public List<RelativeSearchMatchEntry> Matches { get; init; } = new(); public string Tip { get; init; } = ""; }

	public record LoadTblResponse { public bool Success { get; init; } public string Source { get; init; } = ""; public int EntryCount { get; init; } public int MaxByteSequenceLength { get; init; } public List<string> SampleEntries { get; init; } = new(); public List<string> ParseErrors { get; init; } = new(); }

	public record SearchTextResponse { public string SearchText { get; init; } = ""; public string EncodedPattern { get; init; } = ""; public int MatchCount { get; init; } public List<string> Addresses { get; init; } = new(); }

	public record DecodeTextResponse { public string StartAddress { get; init; } = ""; public int BytesConsumed { get; init; } public string DecodedText { get; init; } = ""; public string RawHex { get; init; } = ""; }

	public record TblMappingEntry { public string Hex { get; init; } = ""; public string Character { get; init; } = ""; }
	public record TblInfoResponse { public bool Loaded { get; init; } public int EntryCount { get; init; } public int MaxByteSequenceLength { get; init; } public List<TblMappingEntry> Mappings { get; init; } = new(); }

	// ── Trace / Profiler ─────────────────────────────────────────

	public record SetTraceOptionsResponse { public bool Success { get; init; } public string CpuType { get; init; } = ""; public bool Enabled { get; init; } public string Format { get; init; } = ""; }

	public record TraceLineEntry { public string CpuType { get; init; } = ""; public string Pc { get; init; } = ""; public string ByteCode { get; init; } = ""; public string Output { get; init; } = ""; }
	public record ExecutionTraceResponse { public int LineCount { get; init; } public List<TraceLineEntry> Trace { get; init; } = new(); }

	public record ProfiledFunctionEntry { public string Address { get; init; } = ""; public string MemoryType { get; init; } = ""; public ulong CallCount { get; init; } public ulong InclusiveCycles { get; init; } public ulong ExclusiveCycles { get; init; } public ulong MinCycles { get; init; } public ulong MaxCycles { get; init; } public double AvgCycles { get; init; } public string Flags { get; init; } = ""; }
	public record ProfilerDataResponse { public int FunctionCount { get; init; } public List<ProfiledFunctionEntry> Functions { get; init; } = new(); }

	// ── History ───────────────────────────────────────────────────

	public record HistoryEnabledResponse { public bool Enabled { get; init; } }
	public record HistoryStateResponse { public uint Position { get; init; } public uint Length { get; init; } public bool IsPaused { get; init; } public double Fps { get; init; } public uint Volume { get; init; } public uint SegmentCount { get; init; } }
	public record HistorySetOptionsResponse { public bool Success { get; init; } public bool IsPaused { get; init; } public int Volume { get; init; } }
	public record HistoryNavigateResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public int Position { get; init; } }
	public record HistorySaveStateResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public string File { get; init; } = ""; public int Position { get; init; } }
	public record HistorySaveMovieResponse { public bool Success { get; init; } public string Action { get; init; } = ""; public string File { get; init; } = ""; public int StartPosition { get; init; } public int EndPosition { get; init; } }

	// ── Input ────────────────────────────────────────────────────

	public record KeySetResponse { public bool Success { get; init; } public int ScanCode { get; init; } public bool Pressed { get; init; } }
	public record KeyEntry { public int ScanCode { get; init; } public string KeyName { get; init; } = ""; }
	public record PressedKeysResponse { public int Count { get; init; } public List<KeyEntry> Keys { get; init; } = new(); }
	public record KeyInfoResponse { public string KeyName { get; init; } = ""; public int ScanCode { get; init; } }
	public record MouseResponse { public bool Success { get; init; } public string Mode { get; init; } = ""; public double X { get; init; } public double Y { get; init; } }
	public record AvailablePortsResponse { public List<int> AvailablePorts { get; init; } = new(); }
	public record InputOverrideResponse { public bool Success { get; init; } public int Port { get; init; } public string Buttons { get; init; } = ""; }
	public record DisableKeysResponse { public bool Success { get; init; } public bool InputDisabled { get; init; } }
	public record HasControlDeviceResponse { public string ControllerType { get; init; } = ""; public bool Available { get; init; } }
}
