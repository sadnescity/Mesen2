using Mesen.Interop;
using Mesen.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Linq;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public class SpriteAndTilemapTools
	{
		[McpServerTool(Name = "mesen_get_sprite_list", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get the list of all sprites currently displayed by the PPU. Returns position, tile, palette, size, mirroring, and priority for each sprite.")]
		public static string GetSpriteList(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			BaseState ppuState = DebugApi.GetPpuState(cpu);
			BaseState ppuToolsState = DebugApi.GetPpuToolsState(cpu);

			GetSpritePreviewOptions options = new GetSpritePreviewOptions() {
				Background = SpriteBackground.Transparent
			};

			DebugSpritePreviewInfo previewInfo = DebugApi.GetSpritePreviewInfo(cpu, options, ppuState, ppuToolsState);

			// Get VRAM and sprite RAM
			MemoryType vramType = cpu.GetVramMemoryType();
			byte[] vram = DebugApi.GetMemoryState(vramType);
			MemoryType vramExtType = cpu.GetVramMemoryType(getExtendedRam: true);
			if(vramType != vramExtType) {
				byte[] extVram = DebugApi.GetMemoryState(vramExtType);
				byte[] combined = new byte[vram.Length + extVram.Length];
				Array.Copy(vram, combined, vram.Length);
				Array.Copy(extVram, 0, combined, vram.Length, extVram.Length);
				vram = combined;
			}

			MemoryType spriteRamType = cpu.GetSpriteRamMemoryType();
			byte[] spriteRam;
			if(spriteRamType == MemoryType.None) {
				spriteRam = Array.Empty<byte>();
			} else {
				spriteRam = DebugApi.GetMemoryState(spriteRamType);
				MemoryType spriteRamExtType = cpu.GetSpriteRamMemoryType(getExtendedRam: true);
				if(spriteRamType != spriteRamExtType) {
					byte[] extSpriteRam = DebugApi.GetMemoryState(spriteRamExtType);
					byte[] combined = new byte[spriteRam.Length + extSpriteRam.Length];
					Array.Copy(spriteRam, combined, spriteRam.Length);
					Array.Copy(extSpriteRam, 0, combined, spriteRam.Length, extSpriteRam.Length);
					spriteRam = combined;
				}
			}

			UInt32[] palette = DebugApi.GetPaletteInfo(cpu).GetRgbPalette();

			DebugSpriteInfo[] spriteList = Array.Empty<DebugSpriteInfo>();
			UInt32[] spritePreviews = Array.Empty<UInt32>();

			// Allocate screen preview buffer (C++ writes to it even if we don't need it)
			int screenPixels = (int)previewInfo.Width * (int)previewInfo.Height;
			UInt32[] screenBuffer = new UInt32[Math.Max(screenPixels, 1)];
			GCHandle screenHandle = GCHandle.Alloc(screenBuffer, GCHandleType.Pinned);
			try {
				DebugApi.GetSpriteList(ref spriteList, ref spritePreviews, cpu, options, ppuState, ppuToolsState, vram, spriteRam, palette, screenHandle.AddrOfPinnedObject());
			} finally {
				screenHandle.Free();
			}

			List<SpriteEntry> sprites = new();
			foreach(DebugSpriteInfo sprite in spriteList) {
				sprites.Add(new SpriteEntry {
					Index = sprite.SpriteIndex,
					X = sprite.X,
					Y = sprite.Y,
					RawX = sprite.RawX,
					RawY = sprite.RawY,
					Width = sprite.Width,
					Height = sprite.Height,
					TileIndex = "$" + sprite.TileIndex.ToString("X2"),
					TileAddress = "$" + sprite.TileAddress.ToString("X4"),
					Palette = sprite.Palette,
					PaletteAddress = "$" + sprite.PaletteAddress.ToString("X4"),
					Bpp = sprite.Bpp,
					Format = sprite.Format.ToString(),
					Priority = sprite.Priority.ToString(),
					Mode = sprite.Mode.ToString(),
					Visibility = sprite.Visibility.ToString(),
					HorizontalMirror = sprite.HorizontalMirror.ToString(),
					VerticalMirror = sprite.VerticalMirror.ToString()
				});
			}

			return McpToolHelper.Serialize(new SpriteListResponse {
				SpriteCount = sprites.Count,
				ScreenWidth = previewInfo.Width,
				ScreenHeight = previewInfo.Height,
				Sprites = sprites
			});
		}

		[McpServerTool(Name = "mesen_get_tilemap_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get tilemap information: dimensions, scroll position, tile size, and per-tile details. Use layer parameter for systems with multiple BG layers (SNES, GBA).")]
		public static string GetTilemapInfo(
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType,
			[Description("BG layer index (0-3, default 0). Used for SNES/GBA which have multiple layers.")] int layer = 0)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			BaseState ppuState = DebugApi.GetPpuState(cpu);
			BaseState ppuToolsState = DebugApi.GetPpuToolsState(cpu);

			GetTilemapOptions options = new GetTilemapOptions() {
				Layer = (byte)layer
			};

			FrameInfo tilemapSize = DebugApi.GetTilemapSize(cpu, options, ppuState);

			MemoryType vramType = cpu.GetVramMemoryType();
			byte[] vram = DebugApi.GetMemoryState(vramType);
			MemoryType vramExtType = cpu.GetVramMemoryType(getExtendedRam: true);
			if(vramType != vramExtType) {
				byte[] extVram = DebugApi.GetMemoryState(vramExtType);
				byte[] combined = new byte[vram.Length + extVram.Length];
				Array.Copy(vram, combined, vram.Length);
				Array.Copy(extVram, 0, combined, vram.Length, extVram.Length);
				vram = combined;
			}

			UInt32[] palette = DebugApi.GetPaletteInfo(cpu).GetRgbPalette();

			// Allocate output buffer (C++ writes to it even if we don't use the pixels)
			int tilemapPixels = (int)tilemapSize.Width * (int)tilemapSize.Height;
			UInt32[] tilemapBuffer = new UInt32[Math.Max(tilemapPixels, 1)];
			GCHandle tilemapHandle = GCHandle.Alloc(tilemapBuffer, GCHandleType.Pinned);
			DebugTilemapInfo tilemapInfo;
			try {
				tilemapInfo = DebugApi.GetTilemap(cpu, options, ppuState, ppuToolsState, vram, palette, tilemapHandle.AddrOfPinnedObject());
			} finally {
				tilemapHandle.Free();
			}

			return McpToolHelper.Serialize(new TilemapInfoResponse {
				Layer = layer,
				Width = tilemapSize.Width,
				Height = tilemapSize.Height,
				TileWidth = tilemapInfo.TileWidth,
				TileHeight = tilemapInfo.TileHeight,
				Bpp = tilemapInfo.Bpp,
				Format = tilemapInfo.Format.ToString(),
				Mirroring = tilemapInfo.Mirroring.ToString(),
				ScrollX = tilemapInfo.ScrollX,
				ScrollY = tilemapInfo.ScrollY,
				ScrollWidth = tilemapInfo.ScrollWidth,
				ScrollHeight = tilemapInfo.ScrollHeight,
				RowCount = tilemapInfo.RowCount,
				ColumnCount = tilemapInfo.ColumnCount,
				TilemapAddress = "$" + tilemapInfo.TilemapAddress.ToString("X4"),
				TilesetAddress = "$" + tilemapInfo.TilesetAddress.ToString("X4"),
				Priority = tilemapInfo.Priority
			});
		}

		[McpServerTool(Name = "mesen_get_tilemap_tile_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Get detailed information about a specific tile in the tilemap by its pixel coordinates.")]
		public static string GetTilemapTileInfo(
			[Description("X pixel coordinate in the tilemap")] int x,
			[Description("Y pixel coordinate in the tilemap")] int y,
			[Description("CPU type: Nes, Snes, Gameboy, Gba, Pce, Sms, Ws")] string cpuType,
			[Description("BG layer index (0-3, default 0)")] int layer = 0)
		{
			McpToolHelper.EnsureDebuggerReady();
			CpuType cpu = McpToolHelper.ParseCpuType(cpuType);

			BaseState ppuState = DebugApi.GetPpuState(cpu);
			BaseState ppuToolsState = DebugApi.GetPpuToolsState(cpu);

			MemoryType vramType = cpu.GetVramMemoryType();
			byte[] vram = DebugApi.GetMemoryState(vramType);
			MemoryType vramExtType = cpu.GetVramMemoryType(getExtendedRam: true);
			if(vramType != vramExtType) {
				byte[] extVram = DebugApi.GetMemoryState(vramExtType);
				byte[] combined = new byte[vram.Length + extVram.Length];
				Array.Copy(vram, combined, vram.Length);
				Array.Copy(extVram, 0, combined, vram.Length, extVram.Length);
				vram = combined;
			}

			GetTilemapOptions options = new GetTilemapOptions() {
				Layer = (byte)layer
			};

			DebugTilemapTileInfo? tileInfo = DebugApi.GetTilemapTileInfo((uint)x, (uint)y, cpu, options, vram, ppuState, ppuToolsState);
			if(tileInfo == null) {
				throw new McpException($"No tile at ({x}, {y})");
			}

			DebugTilemapTileInfo tile = tileInfo.Value;
			return McpToolHelper.Serialize(new TilemapTileInfoResponse {
				Row = tile.Row,
				Column = tile.Column,
				Width = tile.Width,
				Height = tile.Height,
				TileIndex = tile.TileIndex,
				TileMapAddress = "$" + tile.TileMapAddress.ToString("X4"),
				TileAddress = "$" + tile.TileAddress.ToString("X4"),
				PaletteIndex = tile.PaletteIndex,
				PaletteAddress = "$" + tile.PaletteAddress.ToString("X4"),
				BasePaletteIndex = tile.BasePaletteIndex,
				AttributeAddress = "$" + tile.AttributeAddress.ToString("X4"),
				AttributeData = "$" + tile.AttributeData.ToString("X4"),
				HorizontalMirroring = tile.HorizontalMirroring.ToString(),
				VerticalMirroring = tile.VerticalMirroring.ToString(),
				HighPriority = tile.HighPriority.ToString()
			});
		}
	}
}
