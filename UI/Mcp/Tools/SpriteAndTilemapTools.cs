using Mesen.Interop;
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
	public static class SpriteAndTilemapTools
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

			List<object> sprites = new();
			foreach(DebugSpriteInfo sprite in spriteList) {
				sprites.Add(new {
					index = sprite.SpriteIndex,
					x = sprite.X,
					y = sprite.Y,
					rawX = sprite.RawX,
					rawY = sprite.RawY,
					width = sprite.Width,
					height = sprite.Height,
					tileIndex = "$" + sprite.TileIndex.ToString("X2"),
					tileAddress = "$" + sprite.TileAddress.ToString("X4"),
					palette = sprite.Palette,
					paletteAddress = "$" + sprite.PaletteAddress.ToString("X4"),
					bpp = sprite.Bpp,
					format = sprite.Format.ToString(),
					priority = sprite.Priority.ToString(),
					mode = sprite.Mode.ToString(),
					visibility = sprite.Visibility.ToString(),
					horizontalMirror = sprite.HorizontalMirror.ToString(),
					verticalMirror = sprite.VerticalMirror.ToString()
				});
			}

			return McpToolHelper.Serialize(new {
				spriteCount = sprites.Count,
				screenWidth = previewInfo.Width,
				screenHeight = previewInfo.Height,
				sprites = sprites
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

			return McpToolHelper.Serialize(new {
				layer = layer,
				width = tilemapSize.Width,
				height = tilemapSize.Height,
				tileWidth = tilemapInfo.TileWidth,
				tileHeight = tilemapInfo.TileHeight,
				bpp = tilemapInfo.Bpp,
				format = tilemapInfo.Format.ToString(),
				mirroring = tilemapInfo.Mirroring.ToString(),
				scrollX = tilemapInfo.ScrollX,
				scrollY = tilemapInfo.ScrollY,
				scrollWidth = tilemapInfo.ScrollWidth,
				scrollHeight = tilemapInfo.ScrollHeight,
				rowCount = tilemapInfo.RowCount,
				columnCount = tilemapInfo.ColumnCount,
				tilemapAddress = "$" + tilemapInfo.TilemapAddress.ToString("X4"),
				tilesetAddress = "$" + tilemapInfo.TilesetAddress.ToString("X4"),
				priority = tilemapInfo.Priority
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
			return McpToolHelper.Serialize(new {
				row = tile.Row,
				column = tile.Column,
				width = tile.Width,
				height = tile.Height,
				tileIndex = tile.TileIndex,
				tileMapAddress = "$" + tile.TileMapAddress.ToString("X4"),
				tileAddress = "$" + tile.TileAddress.ToString("X4"),
				paletteIndex = tile.PaletteIndex,
				paletteAddress = "$" + tile.PaletteAddress.ToString("X4"),
				basePaletteIndex = tile.BasePaletteIndex,
				attributeAddress = "$" + tile.AttributeAddress.ToString("X4"),
				attributeData = "$" + tile.AttributeData.ToString("X4"),
				horizontalMirroring = tile.HorizontalMirroring.ToString(),
				verticalMirroring = tile.VerticalMirroring.ToString(),
				highPriority = tile.HighPriority.ToString()
			});
		}
	}
}
