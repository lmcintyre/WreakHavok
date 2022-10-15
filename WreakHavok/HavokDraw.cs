using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dalamud;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;
using FFXIVClientStructs.STD;
using ImGuiNET;

namespace WreakHavok;

public static unsafe class HavokDraw
{
	private static Vector4 FieldColor = new Vector4(0.2f, 0.9f, 0.4f, 1);
	private static Vector4 TypeColor = new Vector4(0.2f, 0.9f, 0.9f, 1);
	private static Vector4 PropertyColor = new Vector4(0.2f, 0.6f, 0.4f, 1);
	
	private static int _partialSkeletonIndexer = 0;
	public static void DrawPartialSkeletons(Skeleton* skeleton)
	{
		var length = skeleton->PartialSkeletonCount;
		IndexResetter(ref _partialSkeletonIndexer, length);
		Indexer("PartialSkeletonIndexer", ref _partialSkeletonIndexer, 0, length);
		var thisPartialSkeleton = &skeleton->PartialSkeletons[_partialSkeletonIndexer];
		// var addr = $"{(ulong)thisPartialSkeleton:X}";
		Draw($"PartialSkeleton[{_partialSkeletonIndexer}]", thisPartialSkeleton);
		// Util.RightClickCopy(addr);
	}

	private static int _skeletonResourceHandleIndexer = 0;
	public static void DrawSkeletonResourceHandles(Skeleton* skeleton)
	{
		var length = skeleton->PartialSkeletonCount;
		IndexResetter(ref _skeletonResourceHandleIndexer, length);
		Indexer("SkeletonResourceHandleIndexer", ref _skeletonResourceHandleIndexer, 0, length);
		var thisSkeletonResourceHandle = skeleton->SkeletonResourceHandles[_skeletonResourceHandleIndexer];
		// var addr = $"{(ulong)thisSkeletonResourceHandle:X}";
		Draw($"SkeletonResourceHandle[{_skeletonResourceHandleIndexer}]", thisSkeletonResourceHandle);
		// Util.RightClickCopy(addr);
	}

	private static int _animationResourceHandleIndexer = 0;
	public static void DrawAnimationResourceHandles(Skeleton* skeleton)
	{
		var length = skeleton->PartialSkeletonCount;
		IndexResetter(ref _animationResourceHandleIndexer, length);
		Indexer("AnimationResourceHandleIndexer", ref _animationResourceHandleIndexer, 0, length);
		var thisAnimationResourceHandle = skeleton->AnimationResourceHandles[_animationResourceHandleIndexer];
		// var addr = $"{(ulong)thisAnimationResourceHandle:X}";
		ImGui.Text($"{(ulong)thisAnimationResourceHandle:X} sorry not implemented lol");
		// Draw($"AnimationResourceHandle[{_animationResourceHandleIndexer}]", thisAnimationResourceHandle);
		// Util.RightClickCopy(addr);
	}
	
	private static int _jobIndexer = 0;
	public static void DrawJobs(PartialSkeleton* partialSkeleton)
	{
		var length = 2;
		IndexResetter(ref _jobIndexer, length);
		Indexer("JobIndexer", ref _jobIndexer, 0, length);
		var thisJob = (hkaSampleBlendJob*)(partialSkeleton->Jobs + (_jobIndexer * 0x80));
		Draw($"Job[{_partialSkeletonIndexer}]", thisJob);
	}

	private static int _havokAnimatedSkeletonIndexer = 0;
	public static void DrawHavokAnimatedSkeleton(PartialSkeleton* addr)
	{
		var length = 2;
		IndexResetter(ref _havokAnimatedSkeletonIndexer, length);
		Indexer("HavokAnimatedSkeletonIndexer", ref _havokAnimatedSkeletonIndexer, 0, length);
		var thisSkeleton = (hkaAnimatedSkeleton*)addr->HavokAnimatedSkeleton[_havokAnimatedSkeletonIndexer];
		Draw($"HavokAnimatedSkeleton[{_havokAnimatedSkeletonIndexer}]", thisSkeleton);
	}

	private static int _havokPoseIndexer = 0;
	public static void DrawHavokPoses(PartialSkeleton* addr)
	{
		var length = 4;
		IndexResetter(ref _havokPoseIndexer, length);
		Indexer("HavokAnimatedSkeletonIndexer", ref _havokPoseIndexer, 0, length);
		var thisSkeleton = (hkaAnimatedSkeleton*)addr->HavokPoses[_havokPoseIndexer];
		Draw($"HavokPoses[{_havokPoseIndexer}]", thisSkeleton);
	}

	private static int _animationControlIndexer;
	public static void DrawAnimationControls(hkaAnimatedSkeleton* addr)
	{
		DrawHkArray("AnimationControls", (hkArray<Pointer<hkaDefaultAnimationControl>>) addr->AnimationControls, ref _animationControlIndexer);
	}
	
	public static void DrawAnimationControlLocalTime(hkaAnimationControl* addr)
	{
		if (addr != null && addr->Binding.ptr != null && addr->Binding.ptr->Animation.ptr != null)
		{
			var duration = addr->Binding.ptr->Animation.ptr->Duration;
			ImGui.TextColored(TypeColor, "Single");
			ImGui.SameLine();
			ImGui.TextColored(FieldColor, "LocalTime");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(350f);
			ImGui.SliderFloat("##LocalTime", ref addr->LocalTime, 0.0f, duration - 0.05f);
		}
		else
		{
			ImGui.TextColored(TypeColor, "Single");
			ImGui.SameLine();
			ImGui.TextColored(FieldColor, "LocalTime");
			ImGui.SameLine();
			ImGui.Text($"{addr->LocalTime}");	
		}
	}
	
	public static void DrawPlaybackSpeed(hkaDefaultAnimationControl* addr)
	{

			ImGui.TextColored(TypeColor, "Single");
			ImGui.SameLine();
			ImGui.TextColored(FieldColor, "PlaybackSpeed");
			ImGui.SameLine();
			ImGui.SetNextItemWidth(350f);
			ImGui.SliderFloat("##PlaybackSpeed", ref addr->PlaybackSpeed, -1f, 1f);
			ImGui.SameLine();
			if (ImGui.Button("Reset##PlaybackSpeedResetter")) addr->PlaybackSpeed = 0f;

	}
	
	private static void DrawHkArray<T>(string fieldName, hkArray<T> input, ref int indexer) where T : unmanaged
	{
		var length = input.Length;

		var dataTypeName = typeof(T).Name;
		var typeName = $"hkArray[{dataTypeName}]";
		
		ImGui.TextColored(TypeColor, typeName);
		ImGui.SameLine();
		ImGui.TextColored(PropertyColor, fieldName);
		ImGui.SameLine();
		
		ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
		if (ImGui.TreeNode($"FFXIVClientStructs.Havok.{typeName}##print-obj-{(ulong)input.Data:X}"))
		{
			ImGui.PopStyleColor();

			IndexResetter(ref indexer, length);
			Indexer($"{fieldName}indexer", ref indexer, 0, length);
			var thisData = &input.Data[indexer];
			Draw($"Data[{indexer}]", thisData);
			
			ImGui.TextColored(TypeColor, "Int32");
			ImGui.SameLine();
			ImGui.TextColored(FieldColor, "Length");
			ImGui.SameLine();
			ImGui.Text($"{input.Length}");
			
			ImGui.TextColored(TypeColor, "Int32");
			ImGui.SameLine();
			ImGui.TextColored(FieldColor, "CapacityAndFlags");
			ImGui.SameLine();
			ImGui.Text($"{input.CapacityAndFlags}");
			
			ImGui.TextColored(TypeColor, "Int32");
			ImGui.SameLine();
			ImGui.TextColored(PropertyColor, "Capacity");
			ImGui.SameLine();
			ImGui.Text($"{input.Capacity}");
			
			ImGui.TextColored(TypeColor, "Int32");
			ImGui.SameLine();
			ImGui.TextColored(PropertyColor, "Flags");
			ImGui.SameLine();
			ImGui.Text($"{input.Flags}");

			ImGui.TreePop();
		}
		else
		{
			ImGui.PopStyleColor();
		}
	}

	private static void IndexResetter(ref int value, int max)
	{
		if (value >= max)
			value = 0;
	}
	
	private static void Indexer(string text, ref int value, int min, int max)
	{
		ImGui.PushItemWidth(100);
		if (ImGui.InputInt($"##{text}", ref value))
		{
			if (value >= max)
				value = max - 1;
			if (value < min)
				value = min;
		}
		ImGui.PopItemWidth();
		ImGui.SameLine();
	}


	private static ulong moduleEndAddr = 0;
	private static ulong moduleStartAddr = 0;

	public static void Draw<T>(string fieldName, T* obj) where T : unmanaged
	{
		if (obj == null)
		{
			if (!string.IsNullOrEmpty(fieldName))
			{
				ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
				ImGui.Text(fieldName);
				ImGui.PopStyleColor();
				ImGui.SameLine();	
			}
			ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
			ImGui.SetNextItemOpen(false);
			ImGui.Text("null");
			// ImGui.TreeNode("null");
			ImGui.PopStyleColor();
			return;
		} 
		Draw(fieldName, *obj, (ulong)obj);
	}

	public static void Draw(string fieldName, object obj, ulong addr, IEnumerable<string>? path = null)
	{
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2));
		path ??= new List<string>();

		if (moduleEndAddr == 0 && moduleStartAddr == 0)
		{
			try
			{
				var processModule = Process.GetCurrentProcess().MainModule;
				if (processModule != null)
				{
					moduleStartAddr = (ulong)processModule.BaseAddress.ToInt64();
					moduleEndAddr = moduleStartAddr + (ulong)processModule.ModuleMemorySize;
				}
				else
				{
					moduleEndAddr = 1;
				}
			}
			catch
			{
				moduleEndAddr = 1;
			}
		}

		if (!string.IsNullOrEmpty(fieldName))
		{
			ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
			ImGui.Text(fieldName);
			ImGui.PopStyleColor();
			ImGui.SameLine();
			ImGuiHelpers.ClickToCopyText($"{addr:X}");
			ImGui.SameLine();
		}
		
		ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudYellow);
		if (ImGui.TreeNode($"{obj}##print-obj-{addr:X}-{string.Join("-", path)}"))
		{
			ImGui.PopStyleColor();
			foreach (var f in obj.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
			{
				if (Util.DrawOverrides.TryGetValue(obj.GetType().ToString(), out var overrides) && overrides.TryGetValue(f.Name, out var action))
				{
					action(addr);
					continue;
				}

				var fixedBuffer = (FixedBufferAttribute)f.GetCustomAttribute(typeof(FixedBufferAttribute));
				if (fixedBuffer != null)
				{
					ImGui.Text($"fixed");
					ImGui.SameLine();
					ImGui.TextColored(TypeColor, $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
				}
				else
				{
					ImGui.TextColored(TypeColor, $"{f.FieldType.Name}");
				}

				ImGui.SameLine();
				ImGui.TextColored(FieldColor, $"{f.Name}: ");
				ImGui.SameLine();

				DrawValue(addr, f.FieldType, f.GetValue(obj), new List<string>(path) { f.Name });
			}

			foreach (var p in obj.GetType().GetProperties().Where(p => p.GetGetMethod()?.GetParameters().Length == 0))
			{
				ImGui.TextColored(TypeColor, $"{p.PropertyType.Name}");
				ImGui.SameLine();
				ImGui.TextColored(PropertyColor, $"{p.Name}: ");
				ImGui.SameLine();

				DrawValue(addr, p.PropertyType, p.GetValue(obj), new List<string>(path) { p.Name });
			}

			ImGui.TreePop();
		}
		else
		{
			ImGui.PopStyleColor();
		}

		ImGui.PopStyleVar();
	}
	
	private static unsafe void DrawValue(ulong addr, Type type, object value, IEnumerable<string> path)
	{
		if (type.IsPointer)
		{
			var val = (Pointer)value;
			var unboxed = Pointer.Unbox(val);
			if (unboxed != null)
			{
				var unboxedAddr = (ulong)unboxed;
				ImGuiHelpers.ClickToCopyText($"{(ulong)unboxed:X}");
				if (moduleStartAddr > 0 && unboxedAddr >= moduleStartAddr && unboxedAddr <= moduleEndAddr)
				{
					ImGui.SameLine();
					ImGui.PushStyleColor(ImGuiCol.Text, 0xffcbc0ff);
					ImGuiHelpers.ClickToCopyText($"ffxiv_dx11.exe+{unboxedAddr - moduleStartAddr:X}");
					ImGui.PopStyleColor();
				}

				try
				{
					var eType = type.GetElementType();
					var ptrObj = SafeMemory.PtrToStructure(new IntPtr(unboxed), eType);
					ImGui.SameLine();
					if (ptrObj == null)
					{
						ImGui.Text("null or invalid");
					}
					else
					{
						Draw(string.Empty, ptrObj, (ulong)unboxed, path: new List<string>(path));
					}
				}
				catch
				{
					// Ignored
				}
			}
			else
			{
				ImGui.Text("null");
			}
		}
		else
		{
			if (!type.IsPrimitive)
			{
				Draw(string.Empty, value, addr, new List<string>(path));
			}
			else
			{
				ImGui.Text($"{value}");
			}
		}
	}
}