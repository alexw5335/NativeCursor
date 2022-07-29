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
		Main.instance.IsMouseVisible = true;
		Main.MouseBorderColor = Color.Transparent;
		IL.Terraria.Main.DrawCursor += Ret;
		IL.Terraria.Main.DrawInterface_36_Cursor += Ret;
	}

	public override void Unload() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		Main.instance.IsMouseVisible = false;
		Main.MouseBorderColor = new Color(64, 64, 64, 64);
		IL.Terraria.Main.DrawCursor -= Ret;
		IL.Terraria.Main.DrawInterface_36_Cursor -= Ret;
	}
	
	private static void Ret(ILContext context) => new ILCursor(context).Emit(OpCodes.Ret);
	
}



public class NativeCursorModSystem : ModSystem {

	public override void PostUpdateInput() {
		if(Main.netMode == NetmodeID.Server || Main.instance == null) return;
		Main.instance.IsMouseVisible = true; // Must be updated every frame
	}
	
}