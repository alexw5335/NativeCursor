using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NativeCursor;

public class NativeCursor : Mod {

	public override void Load() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		Main.MouseBorderColor = Color.Transparent; // Disable drawing of the cursor's outline and background
		IL.Terraria.Main.DrawCursor += Ret;
		IL.Terraria.Main.DrawInterface_36_Cursor += Ret;
		IL.Terraria.Main.DoUpdate += HideCursor;
	}

	public override void Unload() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		Main.MouseBorderColor = new Color(64, 64, 64, 64);
		IL.Terraria.Main.DrawCursor -= Ret;
		IL.Terraria.Main.DrawInterface_36_Cursor -= Ret;
		IL.Terraria.Main.DoUpdate -= HideCursor;
	}
	
	// Early return from methods that draw the game's custom cursor
	private static void Ret(ILContext context) => new ILCursor(context).Emit(OpCodes.Ret);
	
	// Main::DoUpdate
	// 643 IL_08b7: ldarg.0      // this
	// 644 IL_08b8: ldc.i4.0
	// 645 IL_08b9: call         instance void [FNA]Microsoft.Xna.Framework.Game::set_IsMouseVisible(bool)
	private static void HideCursor(ILContext context) {
		var cursor = new ILCursor(context);
		cursor.Goto(644);
		cursor.Remove();
		cursor.Emit(OpCodes.Ldc_I4_1); // isMouseVisible = false -> isMouseVisible = true
	}
	
}