using System;
using System.ComponentModel;
using Iced.Intel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SDL2;
using static SDL2.SDL.SDL_SystemCursor;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;


namespace NativeCursor;



public enum CursorGraphic {
	
	// System cursors, created using SDL.SDL_CreateSystemCursor
	ARROW,
	HAND,
	IBEAM,
	NO,
	CROSSHAIR,
	SIZE_NWSE,
	SIZE_NESW,
	SIZE_WE,
	SIZE_NS,
	SIZE_ALL,
	
	// Custom cursors, created using SDL.SDL_CreateColorCursor
	CUSTOM_SMART,
	CUSTOM_QUICK_TRASH,
	CUSTOM_FAVOURITE,
	CUSTOM_CHAT_SHARE,
	CUSTOM_SELL

}



public class NativeCursorConfig : ModConfig {
	
	[DefaultValue(CursorGraphic.ARROW)]
	public CursorGraphic DefaultCursor;
	
	[DefaultValue(CursorGraphic.CUSTOM_SMART)]
	public CursorGraphic SmartCursorCursor;

	[DefaultValue(CursorGraphic.CUSTOM_QUICK_TRASH)]
	public CursorGraphic QuickTrashCursor;
	
	[DefaultValue(CursorGraphic.CUSTOM_FAVOURITE)]
	public CursorGraphic FavouriteCursor;
	
	[DefaultValue(CursorGraphic.CUSTOM_CHAT_SHARE)]
	public CursorGraphic ChatShareCursor;
	
	[DefaultValue(CursorGraphic.CUSTOM_SELL)]
	public CursorGraphic SellCursor;
	
	[DefaultValue(CursorGraphic.SIZE_NS)]
	public CursorGraphic TransferCursor;
	
	[DefaultValue(CursorGraphic.SIZE_WE)]
	public CursorGraphic UnequipCursor;

	public override ConfigScope Mode => ConfigScope.ClientSide;

	public override void OnChanged() {
		NativeCursor.ReloadCursors(this);
	}
	
}



public class NativeCursorModSystem : ModSystem {
	
	// Keep track of current cursor to avoid unnecessary SDL calls
	private static int currentIndex;
	
	// Restores the cursor to normal when exiting to main menu in case smart cursor is still active
	public override void OnWorldUnload() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return; 
		if (currentIndex == 0) return;
		currentIndex = 0;
		SDL.SDL_SetCursor(NativeCursor.Cursors[0]);
	}
	
	// Note: Not called in main menu
	public override void PostDrawInterface(SpriteBatch spriteBatch) {
		var index = Main.cursorOverride;
		if (index <= 1 && Main.SmartCursorIsUsed) index = 1;
		if (index < 0 || index > NativeCursor.Cursors.Length) index = 0;
		if (index == currentIndex) return;
		currentIndex = index;
		SDL.SDL_SetCursor(NativeCursor.Cursors[index]);
	}
	
}



public class NativeCursor : Mod {

	
	// SDL cursor handles that correspond to TextureAssets::cursors
	// The cursor to be displayed is determined by Main::mouseOverride and Main::SmartCursorIsUsed
	public static IntPtr[] Cursors = new IntPtr[TextureAssets.Cursors.Length];
	public static IntPtr[] ConfigCursors = new IntPtr[(int) CursorGraphic.CUSTOM_SELL + 1];

	// Restore mouse colours on Unload. These are set in Load
	private Color previousMouseColour = Color.Black;
	private Color previousMouseBorderColour = Color.Black;
	
	private static bool initialised;
	
	
	
	// Must add "-unsafe true" to commandLineArgs in Properties/launchSettings.json
	private static unsafe IntPtr createCursor(int[] data, int xOffset, int yOffset) {
		fixed (int* pData = data) {
			var surface = SDL.SDL_CreateRGBSurfaceWithFormatFrom(
				new IntPtr(pData),
				32,
				32,
				32,
				32 * 4,
				SDL.SDL_PIXELFORMAT_RGBA8888
			);
			return SDL.SDL_CreateColorCursor(surface, xOffset, yOffset);
		}
	}

	
	
	public static void ReloadCursors(NativeCursorConfig config) {
		for (var i = 0; i < Cursors.Length; i++)
			Cursors[i] = ConfigCursors[(int) config.DefaultCursor];
		Cursors[0] = ConfigCursors[(int) config.DefaultCursor];
		Cursors[1] = ConfigCursors[(int) config.SmartCursorCursor];
		Cursors[2] = ConfigCursors[(int) config.ChatShareCursor];
		Cursors[3] = ConfigCursors[(int) config.FavouriteCursor];
		Cursors[6] = ConfigCursors[(int) config.QuickTrashCursor];
		Cursors[7] = ConfigCursors[(int) config.UnequipCursor];
		Cursors[8] = ConfigCursors[(int) config.TransferCursor];
		Cursors[9] = ConfigCursors[(int) config.TransferCursor];
		Cursors[10] = ConfigCursors[(int) config.SellCursor];
		// Cursors 4 and 5 are used for the capture interface but are never used as cursor overrides
		// AutoTrash sets the cursor override to 5 for some reason, so this is included for compatibility
		Cursors[5] = ConfigCursors[(int) config.QuickTrashCursor];
		
		SDL.SDL_SetCursor(Cursors[0]); // restore cursor in case smart cursor is being used in the main menu
	}
	
	
	
	public static void Init() {
		ConfigCursors[(int) CursorGraphic.ARROW] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_ARROW);
		ConfigCursors[(int) CursorGraphic.HAND] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_HAND);
		ConfigCursors[(int) CursorGraphic.IBEAM] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_IBEAM);
		ConfigCursors[(int) CursorGraphic.NO] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_NO);
		ConfigCursors[(int) CursorGraphic.CROSSHAIR] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_CROSSHAIR);
		ConfigCursors[(int) CursorGraphic.SIZE_NWSE] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_SIZENWSE);
		ConfigCursors[(int) CursorGraphic.SIZE_NESW] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_SIZENESW);
		ConfigCursors[(int) CursorGraphic.SIZE_WE] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_SIZEWE);
		ConfigCursors[(int) CursorGraphic.SIZE_NS] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_SIZENS);
		ConfigCursors[(int) CursorGraphic.SIZE_ALL] = SDL.SDL_CreateSystemCursor(SDL_SYSTEM_CURSOR_SIZEALL);
		ConfigCursors[(int) CursorGraphic.CUSTOM_SMART] = createCursor(Textures.SMART_CURSOR, 1, 1);
		ConfigCursors[(int) CursorGraphic.CUSTOM_QUICK_TRASH] = createCursor(Textures.BIN, 9, 0);
		ConfigCursors[(int) CursorGraphic.CUSTOM_FAVOURITE] = createCursor(Textures.FAVOURITE, 1, 1);
		ConfigCursors[(int) CursorGraphic.CUSTOM_CHAT_SHARE] = createCursor(Textures.CHAT_SHARE, 7, 7);
		ConfigCursors[(int) CursorGraphic.CUSTOM_SELL] = createCursor(Textures.SELL, 7, 0);
	}
	
	
	
	public override void Load() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		previousMouseColour = Main.mouseColor;
		previousMouseBorderColour = Main.MouseBorderColor;
		Main.MouseBorderColor = Color.Transparent; // Disable drawing of the cursor's outline and background
		IL_Main.DrawCursor += Ret;
		IL_Main.DrawInterface_36_Cursor += Ret;
		IL_Main.DoUpdate += HideCursor;
		Terraria.Graphics.Capture.IL_CaptureInterface.Draw += HideCaptureCursor;
		if (!initialised) Init();
		initialised = true;
		ReloadCursors(ModContent.GetInstance<NativeCursorConfig>());
	}
	
	
	
	public override void Unload() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		Main.mouseColor = previousMouseColour;
		Main.MouseBorderColor = previousMouseBorderColour;
		IL_Main.DrawCursor -= Ret;
		IL_Main.DrawInterface_36_Cursor -= Ret;
		IL_Main.DoUpdate -= HideCursor;
		Terraria.Graphics.Capture.IL_CaptureInterface.Draw -= HideCaptureCursor;
	}
	
	
	
	// Early return from methods that draw the game's custom cursor
	private static void Ret(ILContext context) => new ILCursor(context).Emit(OpCodes.Ret);
	
	
	
	// Main::DoUpdate
	// isMouseVisible = false -> isMouseVisible = true
	private static void HideCursor(ILContext context) {
		var cursor = new ILCursor(context);
		var method = typeof(Main).GetMethod("set_IsMouseVisible");
		if (method == null) throw new Exception();
		while (cursor.TryGotoNext(i => i.MatchCall(method))) {
			if (cursor.Prev.OpCode != OpCodes.Ldc_I4_0) continue;
			cursor.GotoPrev();
			cursor.Remove();
			cursor.Emit(OpCodes.Ldc_I4_1);
		}
	}

	

	// CaptureInterface::Draw
	// Prevent call to DrawCursorSingle
	private static void HideCaptureCursor(ILContext context) {
		var cursor = new ILCursor(context);
		Console.WriteLine(cursor.Instrs[81]);
		cursor.Goto(72);
		cursor.RemoveRange(10);
	}
	
	
}



public class Textures {

	private static int rgba(int r, int g, int b, int a) => a | (b << 8) | (g << 16) | (r << 24);
	private static int B = rgba(0, 0, 0, 255); // black
	private static int W = rgba(255, 255, 255, 255); // white
	private static int A = rgba(255, 180, 51, 255); // gold
	private static int C = rgba(175, 175, 175, 255); // light grey
	private static int D = rgba(125, 125, 125, 255); // grey
	private static int E = rgba(75, 75, 75, 255); // dark grey
	private static int F = rgba(100, 30, 90, 255); // dark red
	private static int G = rgba(200, 70, 170, 255); // light red

	public static int[] BIN = {
		0, 0, 0, 0, 0, 0, W, W, W, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, W, W, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, W, W, W, B, D, D, D, D, D, D, B, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		W, W, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, E, E, D, D, D, C, C, C, C, C, C, D, D, D, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, E, E, D, D, D, C, C, C, C, C, C, D, D, D, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, W, B, E, E, E, D, D, D, D, E, E, E, B, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, B, E, E, E, D, D, D, D, E, E, E, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, W, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, W, W, W, W, W, W, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
	};

	public static int[] SMART_CURSOR = {
		W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, A, A, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, A, A, A, B, B, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, A, B, A, A, B, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, A, B, B, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, A, B, W, W, B, A, A, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, W, B, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, W, W, 0, W, W, B, A, A, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, W, 0, 0, 0, W, B, A, A, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, W, W, B, A, A, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, W, B, A, A, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, W, W, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	};
	
	public static int[] FAVOURITE = {
		W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, W, W, E, D, C, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, W, W, E, D, C, W, W, 0, 0, 0, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, W, W, E, D, C, W, W, 0, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, W, W, E, D, C, W, W, W, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, E, D, C, W, B, B, G, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, E, D, C, B, G, G, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, E, B, G, G, G, G, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, F, F, G, G, G, G, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, F, F, F, F, G, G, G, G, B, B, W, W, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, W, B, B, F, F, F, F, F, F, G, G, G, G, B, B, W, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, W, B, B, B, B, F, F, F, F, F, G, G, G, B, B, W, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, W, W, B, B, F, F, F, F, F, G, B, B, W, W, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, F, F, F, F, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, F, F, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
	};
	
	public static int[] SELL = {
		0, 0, 0, 0, W, W, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, B, B, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, W, W, W, B, B, W, B, B, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, B, B, B, B, W, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, B, W, B, B, W, B, B, W, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		W, B, B, W, W, B, B, W, B, B, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, B, B, W, B, B, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, B, B, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, B, B, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, B, W, B, B, W, B, B, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, W, W, W, B, B, W, B, B, W, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, B, B, W, B, B, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, B, B, W, B, B, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, W, W, W, B, B, W, B, B, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, W, W, B, B, W, B, B, W, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, B, W, B, B, W, B, B, W, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, B, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, B, B, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, W, W, W, B, B, W, B, B, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, B, B, W, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, W, W, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	};
	
	public static int[] CHAT_SHARE = {
		0, 0, 0, 0, 0, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, W, W, B, B, B, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, W, W, B, B, B, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, B, B, D, D, D, D, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, B, B, D, W, W, D, D, D, D, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, D, W, W, E, E, E, E, D, D, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, D, W, E, B, B, B, E, D, D, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, D, D, E, B, B, B, E, D, D, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, B, B, D, D, E, B, B, B, E, D, D, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		W, W, B, D, D, E, E, E, E, E, D, D, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, B, B, D, D, D, D, W, D, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, W, W, B, B, D, D, D, D, D, B, B, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, W, W, B, B, B, B, B, B, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, W, W, W, B, B, B, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, W, W, W, W, W, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, D, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, B, B, B, W, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, W, W, W, W, W, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
	};
	
}