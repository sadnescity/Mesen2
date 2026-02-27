using Mesen.Config;
using Mesen.Interop;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Mesen.Mcp.Tools
{
	[McpServerToolType]
	public static class InputTools
	{
		[McpServerTool(Name = "mesen_key_state", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control keyboard key state. Use 'set' to press/release a key, 'reset' to release all keys, or 'get_pressed' to list currently pressed keys.")]
		public static string KeyState(
			[Description("Action: 'set' (press/release a key), 'reset' (release all keys), or 'get_pressed' (list pressed keys)")] string action,
			[Description("Key scan code (required for 'set' action)")] int scanCode = 0,
			[Description("True to press, false to release (for 'set' action)")] bool pressed = true)
		{
			switch(action.ToLowerInvariant()) {
				case "set":
					InputApi.SetKeyState((UInt16)scanCode, pressed);
					return McpToolHelper.Serialize(new {
						success = true,
						scanCode = scanCode,
						pressed = pressed
					});

				case "reset":
					InputApi.ResetKeyState();
					return McpToolHelper.Serialize(new { success = true });

				case "get_pressed":
					List<UInt16> keys = InputApi.GetPressedKeys();
					List<object> result = new();
					foreach(UInt16 key in keys) {
						result.Add(new {
							scanCode = (int)key,
							keyName = InputApi.GetKeyName(key)
						});
					}
					return McpToolHelper.Serialize(new {
						count = result.Count,
						keys = result
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'set', 'reset', or 'get_pressed'.");
			}
		}

		[McpServerTool(Name = "mesen_key_info", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Look up key info. Provide either scanCode or keyName to get the other.")]
		public static string KeyInfo(
			[Description("Scan code to look up (mutually exclusive with keyName)")] int? scanCode = null,
			[Description("Key name to look up (e.g. 'A', 'Space', 'Enter')")] string? keyName = null)
		{
			if(keyName != null) {
				UInt16 code = InputApi.GetKeyCode(keyName);
				if(code == 0) {
					throw new McpException("Key not found: " + keyName);
				}
				return McpToolHelper.Serialize(new {
					keyName = keyName,
					scanCode = (int)code
				});
			} else if(scanCode != null) {
				string name = InputApi.GetKeyName((UInt16)scanCode.Value);
				return McpToolHelper.Serialize(new {
					scanCode = scanCode.Value,
					keyName = name
				});
			} else {
				throw new McpException("Provide either scanCode or keyName.");
			}
		}

		[McpServerTool(Name = "mesen_mouse", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Control mouse input. Use 'relative' mode for movement deltas or 'absolute' mode for screen position (0.0-1.0).")]
		public static string Mouse(
			[Description("Mode: 'relative' (movement delta) or 'absolute' (screen position 0.0-1.0)")] string mode,
			[Description("X value (pixels for relative, 0.0-1.0 for absolute)")] double x,
			[Description("Y value (pixels for relative, 0.0-1.0 for absolute)")] double y)
		{
			switch(mode.ToLowerInvariant()) {
				case "relative":
					InputApi.SetMouseMovement((Int16)x, (Int16)y);
					return McpToolHelper.Serialize(new {
						success = true,
						mode = "relative",
						x = x,
						y = y
					});

				case "absolute":
					InputApi.SetMousePosition(x, y);
					return McpToolHelper.Serialize(new {
						success = true,
						mode = "absolute",
						x = x,
						y = y
					});

				default:
					throw new McpException("Invalid mode: " + mode + ". Use 'relative' or 'absolute'.");
			}
		}

		[McpServerTool(Name = "mesen_input_override", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Override controller input for automated gameplay. Use 'list' to get available ports or 'set' to override button states.")]
		public static string InputOverride(
			[Description("Action: 'list' (get available ports) or 'set' (override input)")] string action,
			[Description("Controller port (0-7, for 'set' action)")] int port = 0,
			[Description("Comma-separated buttons for 'set': A,B,X,Y,L,R,Up,Down,Left,Right,Select,Start. Empty to release all.")] string buttons = "")
		{
			McpToolHelper.EnsureDebuggerReady();

			switch(action.ToLowerInvariant()) {
				case "list":
					List<int> indexes = DebugApi.GetAvailableInputOverrides();
					return McpToolHelper.Serialize(new {
						availablePorts = indexes
					});

				case "set":
					if(port < 0 || port > 7) {
						throw new McpException("Port must be 0-7.");
					}

					DebugControllerState state = new();
					string[] buttonList = buttons.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
					foreach(string btn in buttonList) {
						switch(btn.ToLowerInvariant()) {
							case "a": state.A = true; break;
							case "b": state.B = true; break;
							case "x": state.X = true; break;
							case "y": state.Y = true; break;
							case "l": state.L = true; break;
							case "r": state.R = true; break;
							case "u": state.U = true; break;
							case "d": state.D = true; break;
							case "up": state.Up = true; break;
							case "down": state.Down = true; break;
							case "left": state.Left = true; break;
							case "right": state.Right = true; break;
							case "select": state.Select = true; break;
							case "start": state.Start = true; break;
						}
					}

					DebugApi.SetInputOverrides((UInt32)port, state);
					return McpToolHelper.Serialize(new {
						success = true,
						port = port,
						buttons = buttons
					});

				default:
					throw new McpException("Invalid action: " + action + ". Use 'list' or 'set'.");
			}
		}

		[McpServerTool(Name = "mesen_disable_all_keys", ReadOnly = false, Destructive = false, OpenWorld = false),
		 Description("Disable or re-enable all keyboard/controller input. Useful when automating input to prevent interference from physical devices.")]
		public static string DisableAllKeys(
			[Description("True to disable all input, false to re-enable")] bool disabled)
		{
			InputApi.DisableAllKeys(disabled);
			return McpToolHelper.Serialize(new {
				success = true,
				inputDisabled = disabled
			});
		}

		[McpServerTool(Name = "mesen_has_control_device", ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false),
		 Description("Check if a specific controller type is connected/available.")]
		public static string HasControlDevice(
			[Description("Controller type (case-insensitive): SnesController, NesController, SnesMouse, SuperScope, NesZapper, GameboyController, GbaController, PceController, SmsController, WsController, etc.")] string controllerType)
		{
			if(!Enum.TryParse<ControllerType>(controllerType, true, out ControllerType type)) {
				throw new McpException("Invalid controller type: " + controllerType);
			}

			bool available = InputApi.HasControlDevice(type);
			return McpToolHelper.Serialize(new {
				controllerType = controllerType,
				available = available
			});
		}
	}
}
