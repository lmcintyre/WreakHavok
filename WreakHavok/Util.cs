using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ImGuiNET;

namespace WreakHavok;

public static unsafe class Util
{
	private static string localPlayerName = null;
	public static OverrideType DrawOverrides = new(); 

	public static bool LocalNameIsNull => localPlayerName == null;
	
	public static void SetLocalName(string name)
	{
		localPlayerName = name;
	}
	
	public static string GetName(GameObject* gameObject)
	{
		var tmpName = Marshal.PtrToStringUTF8((IntPtr)gameObject->Name);
		if (tmpName == localPlayerName) return "You";
		return string.IsNullOrEmpty(tmpName) ? "UNNAMED" : tmpName;
	}

	public static bool VerifyRenderSkeletonValid(GameObject* gameObject)
	{
		if (gameObject == null) return false;
		if (gameObject->DrawObject == null) return false;
		return gameObject->DrawObject->Skeleton != null;
	}
	
	public static void CopyableText(string display, string copy)
	{
		ImGui.Text(display);
		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			ImGui.SetClipboardText(copy);
	}
	
	public static void RightClickCopy(string addr)
	{
		if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
			ImGui.SetClipboardText(addr);
	}
}