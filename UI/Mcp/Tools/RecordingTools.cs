using Mesen.Config;
using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.ComponentModel;
using System.IO;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class RecordingTools
	{
		[McpServerTool(Name = "mesen_video_recording", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control video (AVI) recording. Use action 'start' to begin recording, 'stop' to end, or 'status' to check if recording is active.")]
		public static string VideoRecording(
			[Description("Action: 'start', 'stop', or 'status'")] string action,
			[Description("Output AVI file path (required for 'start')")] string? filepath = null,
			[Description("Video codec: None, ZMBV, CSCD, GIF (default ZMBV)")] string codec = "ZMBV",
			[Description("Compression level 1-9 (default 6)")] int compressionLevel = 6,
			[Description("Record system HUD (default false)")] bool recordSystemHud = false,
			[Description("Record input HUD (default false)")] bool recordInputHud = false)
		{
			switch(action.ToLowerInvariant()) {
				case "start":
					McpToolHelper.EnsureRunning();

					if(string.IsNullOrWhiteSpace(filepath)) {
						throw new McpException("filepath is required for 'start' action.");
					}

					if(!Enum.TryParse<VideoCodec>(codec, true, out VideoCodec videoCodec)) {
						throw new McpException("Invalid codec: " + codec + ". Valid: None, ZMBV, CSCD, GIF");
					}

					RecordAviOptions options = new RecordAviOptions() {
						Codec = videoCodec,
						CompressionLevel = (uint)Math.Clamp(compressionLevel, 1, 9),
						RecordSystemHud = recordSystemHud,
						RecordInputHud = recordInputHud
					};

					RecordApi.AviRecord(filepath, options);
					return McpToolHelper.Serialize(new {
						success = true,
						action = "start",
						file = filepath,
						codec = videoCodec.ToString()
					});

				case "stop":
					RecordApi.AviStop();
					return McpToolHelper.Serialize(new { success = true, action = "stop" });

				case "status":
					return McpToolHelper.Serialize(new { recording = RecordApi.AviIsRecording() });

				default:
					throw new McpException("Invalid action: " + action + ". Use 'start', 'stop', or 'status'.");
			}
		}

		[McpServerTool(Name = "mesen_audio_recording", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control audio (WAV) recording. Use action 'start' to begin recording, 'stop' to end, or 'status' to check if recording is active.")]
		public static string AudioRecording(
			[Description("Action: 'start', 'stop', or 'status'")] string action,
			[Description("Output WAV file path (required for 'start')")] string? filepath = null)
		{
			switch(action.ToLowerInvariant()) {
				case "start":
					McpToolHelper.EnsureRunning();

					if(string.IsNullOrWhiteSpace(filepath)) {
						throw new McpException("filepath is required for 'start' action.");
					}

					RecordApi.WaveRecord(filepath);
					return McpToolHelper.Serialize(new {
						success = true,
						action = "start",
						file = filepath
					});

				case "stop":
					RecordApi.WaveStop();
					return McpToolHelper.Serialize(new { success = true, action = "stop" });

				case "status":
					return McpToolHelper.Serialize(new { recording = RecordApi.WaveIsRecording() });

				default:
					throw new McpException("Invalid action: " + action + ". Use 'start', 'stop', or 'status'.");
			}
		}

		[McpServerTool(Name = "mesen_movie", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control movie (input recording/playback). Use action 'record' to start recording inputs, 'play' to play back a movie file, 'stop' to end recording/playback, or 'status' to check current state.")]
		public static string Movie(
			[Description("Action: 'record', 'play', 'stop', or 'status'")] string action,
			[Description("Movie file path (.mmo) (required for 'record' and 'play')")] string? filepath = null,
			[Description("For 'record': StartWithoutSaveData, StartWithSaveData, CurrentState")] string recordFrom = "StartWithoutSaveData",
			[Description("Author name for recording (optional)")] string author = "",
			[Description("Description for recording (optional)")] string description = "")
		{
			switch(action.ToLowerInvariant()) {
				case "record":
					McpToolHelper.EnsureRunning();

					if(string.IsNullOrWhiteSpace(filepath)) {
						throw new McpException("filepath is required for 'record' action.");
					}

					if(!Enum.TryParse<RecordMovieFrom>(recordFrom, true, out RecordMovieFrom from)) {
						throw new McpException("Invalid recordFrom: " + recordFrom + ". Valid: StartWithoutSaveData, StartWithSaveData, CurrentState");
					}

					RecordMovieOptions movieOptions = new RecordMovieOptions(filepath, author, description, from);
					RecordApi.MovieRecord(movieOptions);

					return McpToolHelper.Serialize(new {
						success = true,
						action = "record",
						file = filepath,
						recordFrom = from.ToString()
					});

				case "play":
					McpToolHelper.EnsureRunning();

					if(string.IsNullOrWhiteSpace(filepath)) {
						throw new McpException("filepath is required for 'play' action.");
					}

					if(!File.Exists(filepath)) {
						throw new McpException("File not found: " + filepath);
					}

					RecordApi.MoviePlay(filepath);
					return McpToolHelper.Serialize(new {
						success = true,
						action = "play",
						file = filepath
					});

				case "stop":
					RecordApi.MovieStop();
					return McpToolHelper.Serialize(new { success = true, action = "stop" });

				case "status":
					return McpToolHelper.Serialize(new {
						playing = RecordApi.MoviePlaying(),
						recording = RecordApi.MovieRecording()
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'record', 'play', 'stop', or 'status'.");
			}
		}
	}
}
