using Mesen.Interop;
using Mesen.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public class HistoryTools
	{
		private static void EnsureHistoryEnabled()
		{
			if(!HistoryApi.HistoryViewerEnabled()) {
				throw new McpException("History viewer is not enabled.");
			}
		}

		[McpServerTool(Name = "mesen_history_state", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Check, get, or configure the history/rewind viewer. Use action 'check' to see if enabled, 'get' to retrieve current state, or 'set_options' to change pause/volume.")]
		public static string HistoryState(
			[Description("Action: 'check' (is enabled?), 'get' (state info), or 'set_options' (pause/volume)")] string action,
			[Description("Pause playback (for 'set_options')")] bool isPaused = false,
			[Description("Volume 0-100 (for 'set_options')")] int volume = 100)
		{
			switch(action.ToLowerInvariant()) {
				case "check":
					return McpToolHelper.Serialize(new HistoryEnabledResponse {
						Enabled = HistoryApi.HistoryViewerEnabled()
					});

				case "get":
					EnsureHistoryEnabled();

					HistoryViewerState state = HistoryApi.HistoryViewerGetState();
					return McpToolHelper.Serialize(new HistoryStateResponse {
						Position = state.Position,
						Length = state.Length,
						IsPaused = state.IsPaused,
						Fps = Math.Round(state.Fps, 2),
						Volume = state.Volume,
						SegmentCount = state.SegmentCount
					});

				case "set_options":
					EnsureHistoryEnabled();

					HistoryViewerOptions options = new HistoryViewerOptions() {
						IsPaused = isPaused,
						Volume = (uint)Math.Clamp(volume, 0, 100)
					};

					HistoryApi.HistoryViewerSetOptions(options);
					return McpToolHelper.Serialize(new HistorySetOptionsResponse {
						Success = true,
						IsPaused = isPaused,
						Volume = volume
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'check', 'get', or 'set_options'.");
			}
		}

		[McpServerTool(Name = "mesen_history_navigate", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Navigate the history/rewind timeline. Use action 'seek' to jump to a position, or 'resume' to resume gameplay from a position.")]
		public static string HistoryNavigate(
			[Description("Action: 'seek' (set position) or 'resume' (resume gameplay from position)")] string action,
			[Description("Position in timeline")] int position)
		{
			EnsureHistoryEnabled();

			switch(action.ToLowerInvariant()) {
				case "seek":
					HistoryApi.HistoryViewerSetPosition((uint)position);
					return McpToolHelper.Serialize(new HistoryNavigateResponse {
						Success = true,
						Action = "seek",
						Position = position
					});

				case "resume":
					HistoryApi.HistoryViewerResumeGameplay((uint)position);
					return McpToolHelper.Serialize(new HistoryNavigateResponse {
						Success = true,
						Action = "resume",
						Position = position
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'seek' or 'resume'.");
			}
		}

		[McpServerTool(Name = "mesen_history_export", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Export from the history timeline. Use action 'save_state' to create a save state file at a position, or 'save_movie' to save a segment as a movie file (.mmo).")]
		public static string HistoryExport(
			[Description("Action: 'save_state' or 'save_movie'")] string action,
			[Description("Output file path")] string filepath,
			[Description("Position in timeline (for save_state) or start position (for save_movie)")] int position,
			[Description("End position in timeline (for save_movie only)")] int endPosition = 0)
		{
			EnsureHistoryEnabled();

			switch(action.ToLowerInvariant()) {
				case "save_state": {
					bool success = HistoryApi.HistoryViewerCreateSaveState(filepath, (uint)position);
					return McpToolHelper.Serialize(new HistorySaveStateResponse {
						Success = success,
						Action = "save_state",
						File = filepath,
						Position = position
					});
				}

				case "save_movie": {
					bool success = HistoryApi.HistoryViewerSaveMovie(filepath, (uint)position, (uint)endPosition);
					return McpToolHelper.Serialize(new HistorySaveMovieResponse {
						Success = success,
						Action = "save_movie",
						File = filepath,
						StartPosition = position,
						EndPosition = endPosition
					});
				}

				default:
					throw new McpException("Invalid action: " + action + ". Use 'save_state' or 'save_movie'.");
			}
		}
	}
}
