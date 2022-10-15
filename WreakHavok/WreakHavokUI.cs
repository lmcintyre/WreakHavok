using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Graphics.Render;
using FFXIVClientStructs.Havok;

namespace WreakHavok;

public unsafe class WreakHavokUI : IDisposable
{
    private bool _inspectorVisible = true;
    public bool InspectorVisible
    {
        get => _inspectorVisible;
        set => _inspectorVisible = value;
    }
    
    private bool _experimentsVisible = true;
    public bool ExperimentsVisible
    {
        get => _inspectorVisible;
        set => _inspectorVisible = value;
    }

    public WreakHavokUI()
    {
        Util.DrawOverrides = new Dictionary<string, Dictionary<string, Action<ulong>>>
        {
            {
                "FFXIVClientStructs.FFXIV.Client.Graphics.Render.Skeleton",
                new Dictionary<string, Action<ulong>>
                {
                    { "PartialSkeletons", addr => { HavokDraw.DrawPartialSkeletons((Skeleton*)addr); } },
                    { "SkeletonResourceHandles", addr => { HavokDraw.DrawSkeletonResourceHandles((Skeleton*)addr); } },
                    { "AnimationResourceHandles", addr => { HavokDraw.DrawAnimationResourceHandles((Skeleton*)addr); } },
                }
            },
            {
                "FFXIVClientStructs.FFXIV.Client.Graphics.Render.PartialSkeleton",
                new Dictionary<string, Action<ulong>>
                {
                    { "Jobs", addr => { HavokDraw.DrawJobs((PartialSkeleton*)addr); } },
                    { "HavokAnimatedSkeleton", addr => { HavokDraw.DrawHavokAnimatedSkeleton((PartialSkeleton*)addr); } },
                    { "HavokPoses", addr => { HavokDraw.DrawHavokPoses((PartialSkeleton*)addr); } },

                }
            },
            {
                "FFXIVClientStructs.Havok.hkaAnimatedSkeleton",
                new Dictionary<string, Action<ulong>>
                {
                    { "AnimationControls", addr => { HavokDraw.DrawAnimationControls((hkaAnimatedSkeleton*)addr); } },

                }
            },
            {
                "FFXIVClientStructs.Havok.hkaAnimationControl",
                new Dictionary<string, Action<ulong>>
                {
                    { "LocalTime", addr => { HavokDraw.DrawAnimationControlLocalTime((hkaAnimationControl*)addr); } },

                }
            },
            {
                "FFXIVClientStructs.Havok.hkaDefaultAnimationControl",
                new Dictionary<string, Action<ulong>>
                {
                    { "PlaybackSpeed", addr => { HavokDraw.DrawPlaybackSpeed((hkaDefaultAnimationControl*)addr); } },

                }
            }
        };

        addAnimationControlHook = Hook<addAnimationControlDelegate>.FromAddress(DalamudContainer.SigScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 8B 41 2C 48 8B DA"), addAnimationControlDetour);
        removeAnimationControlHook = Hook<removeAnimationControlDelegate>.FromAddress(DalamudContainer.SigScanner.ScanText("40 53 48 83 EC 20 48 63 41 28"), removeAnimationControlDetour);
    }
    
    private void addAnimationControlDetour(hkaAnimatedSkeleton* thisptr, hkaAnimationControl* control)
    {
        PluginLog.Debug($"[addAnimationControl] {(ulong)thisptr:X} {(ulong)control:X}");
    }
    
    private void removeAnimationControlDetour(hkaAnimatedSkeleton* thisptr, hkaAnimationControl* control)
    {
        PluginLog.Debug($"[removeAnimationControl] {(ulong)thisptr:X} {(ulong)control:X}");
    }

    private delegate void addAnimationControlDelegate(hkaAnimatedSkeleton* thisptr, hkaAnimationControl* control);
    private delegate void removeAnimationControlDelegate(hkaAnimatedSkeleton* thisptr, hkaAnimationControl* control);
    private Hook<addAnimationControlDelegate> addAnimationControlHook;
    private Hook<removeAnimationControlDelegate> removeAnimationControlHook;

    public void Dispose()
    {
        addAnimationControlHook?.Disable();            
        addAnimationControlHook?.Dispose();            
        removeAnimationControlHook?.Disable();
        removeAnimationControlHook?.Dispose();
    }

    private bool doingSearch;
    private string searchInput = string.Empty;
    private GameObject* selectedGameObject;

    public void Draw()
    {
        DrawInspector();
        DrawExperiments();
    }

    public void DrawInspector()
    {
        if (!InspectorVisible) return;
        
        if (Util.LocalNameIsNull)
        {
            var localName = DalamudContainer.ClientState?.LocalPlayer?.Name?.ToString();
            if (localName != null)
                Util.SetLocalName(localName);
        }

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 400), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Wreak Havok Inspector", ref _inspectorVisible))
        {
            DrawObjectList();
            ImGui.SameLine();
            DrawGameObject();
        }
        ImGui.End();
    }

    private void DrawObjectList()
    {
        var objectTable = DalamudContainer.ObjectTable;

        ImGui.BeginChild("havokDebug##objectList", new Vector2(400, -1), true);
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("###objectSearch", "Search", ref this.searchInput, 0x20);

        foreach (var obj in objectTable)
        {
            if (obj.Address == IntPtr.Zero) continue;
            var gameObject = (GameObject*)obj.Address;
            if (!Util.VerifyRenderSkeletonValid(gameObject)) continue;

            var name = Util.GetName(gameObject);
            
            if (ImGui.Selectable($"{name} - {obj.ObjectKind} - X{obj.Position.X:F2} Y{obj.Position.Y:F2} Z{obj.Position.Z:F2} R{obj.Rotation:F2}##{obj.Address.ToInt64():X}",
                    selectedGameObject == gameObject))
            {
                selectedGameObject = gameObject;
            }
        }
        ImGui.EndChild();
    }

    private void DrawGameObject()
    {
        if (!Util.VerifyRenderSkeletonValid(selectedGameObject))
        {
            ImGui.Text("Select an object to view its Havok details.");
            selectedGameObject = null;
            return;
        }
        
        // verified valid
        var skeleton = selectedGameObject->DrawObject->Skeleton;

        var name = Util.GetName(selectedGameObject);
        
        ImGui.BeginChild("havokDebug##skeletonInfo", new Vector2(-1, -1), true);
        
        ImGui.Text($"{name} - {selectedGameObject->ObjectKind} - X{selectedGameObject->Position.X:F2} Y{selectedGameObject->Position.Y:F2} Z{selectedGameObject->Position.Z:F2} R{selectedGameObject->Rotation:F2}");
        HavokDraw.Draw("Skeleton", skeleton);
        
        ImGui.EndChild();
    }

    private bool allocated;
    private bool replaced;
    private hkaDefaultAnimationControl* _savedControl;
    private hkaDefaultAnimationControl* _ourControl;
    
    private void DrawExperiments()
    {
        if (!ExperimentsVisible) return;
        
        ImGui.SetNextWindowSize(new Vector2(500, 200), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Wreak Havok Experiments", ref _experimentsVisible))
        {
            if (selectedGameObject == null)
            {
                ImGui.Text($"Selected game object: null");
                ImGui.End();
                return;
            }
            
            ImGui.Text($"Selected game object: {Util.GetName(selectedGameObject)}");

            var hooksEnabled = addAnimationControlHook.IsEnabled && removeAnimationControlHook.IsEnabled;
            if (ImGui.Button("Toggle AnimationControl hooks"))
            {
                if (hooksEnabled)
                {
                    addAnimationControlHook.Disable();
                    removeAnimationControlHook.Disable();
                }
                else
                {
                    addAnimationControlHook.Enable();
                    removeAnimationControlHook.Enable();
                }
            }
            ImGui.SameLine();
            ImGui.Text(hooksEnabled ? "Hooks: enabled" : "Hooks: disabled");

            if (!allocated)
                ImGui.Text("Not allocated");
            else 
                HavokDraw.Draw("ourControl", _ourControl);

            var animatedSkeleton = selectedGameObject->DrawObject->Skeleton->PartialSkeletons[0].GetHavokAnimatedSkeleton(0);
            var animationControls = animatedSkeleton->AnimationControls;
            _savedControl = animationControls[0];
            
            if (ImGui.Button(allocated ? "Deallocate control" : "Allocate control"))
            {
                if (allocated)
                {
                    Marshal.FreeHGlobal((IntPtr)_ourControl);
                    allocated = false;
                }
                else
                {
                    _ourControl = (hkaDefaultAnimationControl*) Marshal.AllocHGlobal(sizeof(hkaDefaultAnimationControl));
                    _ourControl->Ctor1(_savedControl);
                    allocated = true;
                }
            }
            ImGui.BeginDisabled(!allocated);
            if (ImGui.Button(replaced ? "Put back" : "Replace"))
            {
                if (replaced)
                {
                    removeAnimationControlHook.Original(animatedSkeleton, (hkaAnimationControl*)_ourControl);
                    addAnimationControlHook.Original(animatedSkeleton, (hkaAnimationControl*)_savedControl);
                    replaced = false;
                }
                else
                {
                    removeAnimationControlHook.Original(animatedSkeleton, (hkaAnimationControl*)_savedControl);
                    addAnimationControlHook.Original(animatedSkeleton, (hkaAnimationControl*)_ourControl);
                    replaced = true;
                }
            }

            if (replaced)
            {
                DrawControls(_ourControl);
            }
            
            ImGui.EndDisabled();
        }
        ImGui.End();
    }

    private void DrawControls(hkaDefaultAnimationControl* control)
    {
        ImGui.BeginChild("animControl");

        var duration = control->hkaAnimationControl.Binding.ptr->Animation.ptr->Duration;
        
        ImGui.SliderFloat("Seek", ref control->hkaAnimationControl.LocalTime, 0, duration - 0.01f);
        ImGui.SliderFloat("Speed", ref control->PlaybackSpeed, 0f, 1f);
        if (ImGui.Button("Play/Pause"))
        {
            if (control->PlaybackSpeed == 0f)
                control->PlaybackSpeed = 1f;
            else
                control->PlaybackSpeed = 0f;
        }
        
        ImGui.EndChild();
    }
    
    // public void Draw()
    // {
    //     ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(3, 2));
    //     ImGui.BeginChild("st_uiDebug_unitBaseSelect", new Vector2(250, -1), true);
    //
    //     ImGui.SetNextItemWidth(-1);
    //     ImGui.InputTextWithHint("###atkUnitBaseSearch", "Search", ref this.searchInput, 0x20);
    //
    //     this.DrawUnitBaseList();
    //     ImGui.EndChild();
    //     if (this.selectedUnitBase != null)
    //     {
    //         ImGui.SameLine();
    //         ImGui.BeginChild("st_uiDebug_selectedUnitBase", new Vector2(-1, -1), true);
    //         this.DrawUnitBase(this.selectedUnitBase);
    //         ImGui.EndChild();
    //     }
    //
    //     ImGui.PopStyleVar();
    // }
    //
    // private void DrawUnitBase(AtkUnitBase* atkUnitBase)
    // {
    //     var isVisible = (atkUnitBase->Flags & 0x20) == 0x20;
    //     var addonName = Marshal.PtrToStringAnsi(new IntPtr(atkUnitBase->Name));
    //     var agent = Service<GameGui>.Get().FindAgentInterface(atkUnitBase);
    //
    //     ImGui.Text($"{addonName}");
    //     ImGui.SameLine();
    //     ImGui.PushStyleColor(ImGuiCol.Text, isVisible ? 0xFF00FF00 : 0xFF0000FF);
    //     ImGui.Text(isVisible ? "Visible" : "Not Visible");
    //     ImGui.PopStyleColor();
    //
    //     ImGui.SameLine(ImGui.GetWindowContentRegionMax().X - ImGui.GetWindowContentRegionMin().X - 25);
    //     if (ImGui.SmallButton("V"))
    //     {
    //         atkUnitBase->Flags ^= 0x20;
    //     }
    //
    //     ImGui.Separator();
    //     ImGuiHelpers.ClickToCopyText($"Address: {(ulong)atkUnitBase:X}", $"{(ulong)atkUnitBase:X}");
    //     ImGuiHelpers.ClickToCopyText($"Agent: {(ulong)agent:X}", $"{(ulong)agent:X}");
    //     ImGui.Separator();
    //
    //     ImGui.Text($"Position: [ {atkUnitBase->X} , {atkUnitBase->Y} ]");
    //     ImGui.Text($"Scale: {atkUnitBase->Scale * 100}%%");
    //     ImGui.Text($"Widget Count {atkUnitBase->UldManager.ObjectCount}");
    //
    //     ImGui.Separator();
    //
    //     object addonObj = *atkUnitBase;
    //
    //     Util.ShowStruct(addonObj, (ulong)atkUnitBase);
    //
    //     ImGui.Dummy(new Vector2(25 * ImGui.GetIO().FontGlobalScale));
    //     ImGui.Separator();
    //     if (atkUnitBase->RootNode != null)
    //         this.PrintNode(atkUnitBase->RootNode);
    //
    //     if (atkUnitBase->UldManager.NodeListCount > 0)
    //     {
    //         ImGui.Dummy(new Vector2(25 * ImGui.GetIO().FontGlobalScale));
    //         ImGui.Separator();
    //         ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
    //         if (ImGui.TreeNode($"Node List##{(ulong)atkUnitBase:X}"))
    //         {
    //             ImGui.PopStyleColor();
    //
    //             for (var j = 0; j < atkUnitBase->UldManager.NodeListCount; j++)
    //             {
    //                 this.PrintNode(atkUnitBase->UldManager.NodeList[j], false, $"[{j}] ");
    //             }
    //
    //             ImGui.TreePop();
    //         }
    //         else
    //         {
    //             ImGui.PopStyleColor();
    //         }
    //     }
    // }
    //
    // private void PrintNode(AtkResNode* node, bool printSiblings = true, string treePrefix = "")
    // {
    //     if (node == null)
    //         return;
    //
    //     if ((int)node->Type < 1000)
    //         this.PrintSimpleNode(node, treePrefix);
    //     else
    //         this.PrintComponentNode(node, treePrefix);
    //
    //     if (printSiblings)
    //     {
    //         var prevNode = node;
    //         while ((prevNode = prevNode->PrevSiblingNode) != null)
    //             this.PrintNode(prevNode, false, "prev ");
    //
    //         var nextNode = node;
    //         while ((nextNode = nextNode->NextSiblingNode) != null)
    //             this.PrintNode(nextNode, false, "next ");
    //     }
    // }
    //
    // private void PrintSimpleNode(AtkResNode* node, string treePrefix)
    // {
    //     var popped = false;
    //     var isVisible = (node->Flags & 0x10) == 0x10;
    //
    //     if (isVisible)
    //         ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));
    //
    //     if (ImGui.TreeNode($"{treePrefix}{node->Type} Node (ptr = {(long)node:X})###{(long)node}"))
    //     {
    //         if (ImGui.IsItemHovered())
    //             this.DrawOutline(node);
    //
    //         if (isVisible)
    //         {
    //             ImGui.PopStyleColor();
    //             popped = true;
    //         }
    //
    //         ImGui.Text("Node: ");
    //         ImGui.SameLine();
    //         ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
    //         ImGui.SameLine();
    //         switch (node->Type)
    //         {
    //             case NodeType.Text: Util.ShowStruct(*(AtkTextNode*)node, (ulong)node); break;
    //             case NodeType.Image: Util.ShowStruct(*(AtkImageNode*)node, (ulong)node); break;
    //             case NodeType.Collision: Util.ShowStruct(*(AtkCollisionNode*)node, (ulong)node); break;
    //             case NodeType.NineGrid: Util.ShowStruct(*(AtkNineGridNode*)node, (ulong)node); break;
    //             case NodeType.Counter: Util.ShowStruct(*(AtkCounterNode*)node, (ulong)node); break;
    //             default: Util.ShowStruct(*node, (ulong)node); break;
    //         }
    //
    //         this.PrintResNode(node);
    //
    //         if (node->ChildNode != null)
    //             this.PrintNode(node->ChildNode);
    //
    //         switch (node->Type)
    //         {
    //             case NodeType.Text:
    //                 var textNode = (AtkTextNode*)node;
    //                 ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(textNode->NodeText.StringPtr))}");
    //
    //                 ImGui.InputText($"Replace Text##{(ulong)textNode:X}", new IntPtr(textNode->NodeText.StringPtr), (uint)textNode->NodeText.BufSize);
    //
    //                 ImGui.Text($"AlignmentType: {(AlignmentType)textNode->AlignmentFontType}  FontSize: {textNode->FontSize}");
    //                 int b = textNode->AlignmentFontType;
    //                 if (ImGui.InputInt($"###setAlignment{(ulong)textNode:X}", ref b, 1))
    //                 {
    //                     while (b > byte.MaxValue) b -= byte.MaxValue;
    //                     while (b < byte.MinValue) b += byte.MaxValue;
    //                     textNode->AlignmentFontType = (byte)b;
    //                     textNode->AtkResNode.Flags_2 |= 0x1;
    //                 }
    //
    //                 ImGui.Text($"Color: #{textNode->TextColor.R:X2}{textNode->TextColor.G:X2}{textNode->TextColor.B:X2}{textNode->TextColor.A:X2}");
    //                 ImGui.SameLine();
    //                 ImGui.Text($"EdgeColor: #{textNode->EdgeColor.R:X2}{textNode->EdgeColor.G:X2}{textNode->EdgeColor.B:X2}{textNode->EdgeColor.A:X2}");
    //                 ImGui.SameLine();
    //                 ImGui.Text($"BGColor: #{textNode->BackgroundColor.R:X2}{textNode->BackgroundColor.G:X2}{textNode->BackgroundColor.B:X2}{textNode->BackgroundColor.A:X2}");
    //
    //                 ImGui.Text($"TextFlags: {textNode->TextFlags}");
    //                 ImGui.SameLine();
    //                 ImGui.Text($"TextFlags2: {textNode->TextFlags2}");
    //
    //                 break;
    //             case NodeType.Counter:
    //                 var counterNode = (AtkCounterNode*)node;
    //                 ImGui.Text($"text: {Marshal.PtrToStringAnsi(new IntPtr(counterNode->NodeText.StringPtr))}");
    //                 break;
    //             case NodeType.Image:
    //                 var imageNode = (AtkImageNode*)node;
    //                 if (imageNode->PartsList != null)
    //                 {
    //                     if (imageNode->PartId > imageNode->PartsList->PartCount)
    //                     {
    //                         ImGui.Text("part id > part count?");
    //                     }
    //                     else
    //                     {
    //                         var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
    //                         var texType = textureInfo->AtkTexture.TextureType;
    //                         ImGui.Text($"texture type: {texType} part_id={imageNode->PartId} part_id_count={imageNode->PartsList->PartCount}");
    //                         if (texType == TextureType.Resource)
    //                         {
    //                             var texFileNameStdString = &textureInfo->AtkTexture.Resource->TexFileResourceHandle->ResourceHandle.FileName;
    //                             var texString = texFileNameStdString->Length < 16
    //                                 ? Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->Buffer)
    //                                 : Marshal.PtrToStringAnsi((IntPtr)texFileNameStdString->BufferPtr);
    //
    //                             ImGui.Text($"texture path: {texString}");
    //                             var kernelTexture = textureInfo->AtkTexture.Resource->KernelTextureObject;
    //
    //                             if (ImGui.TreeNode($"Texture##{(ulong)kernelTexture->D3D11ShaderResourceView:X}"))
    //                             {
    //                                 ImGui.Image(new IntPtr(kernelTexture->D3D11ShaderResourceView), new Vector2(kernelTexture->Width, kernelTexture->Height));
    //                                 ImGui.TreePop();
    //                             }
    //                         }
    //                         else if (texType == TextureType.KernelTexture)
    //                         {
    //                             if (ImGui.TreeNode($"Texture##{(ulong)textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView:X}"))
    //                             {
    //                                 ImGui.Image(new IntPtr(textureInfo->AtkTexture.KernelTexture->D3D11ShaderResourceView), new Vector2(textureInfo->AtkTexture.KernelTexture->Width, textureInfo->AtkTexture.KernelTexture->Height));
    //                                 ImGui.TreePop();
    //                             }
    //                         }
    //                     }
    //                 }
    //                 else
    //                 {
    //                     ImGui.Text("no texture loaded");
    //                 }
    //
    //                 break;
    //         }
    //
    //         ImGui.TreePop();
    //     }
    //     else if (ImGui.IsItemHovered())
    //     {
    //         this.DrawOutline(node);
    //     }
    //
    //     if (isVisible && !popped)
    //         ImGui.PopStyleColor();
    // }
    //
    // private void PrintComponentNode(AtkResNode* node, string treePrefix)
    // {
    //     var compNode = (AtkComponentNode*)node;
    //
    //     var popped = false;
    //     var isVisible = (node->Flags & 0x10) == 0x10;
    //
    //     var componentInfo = compNode->Component->UldManager;
    //
    //     var childCount = componentInfo.NodeListCount;
    //
    //     var objectInfo = (AtkUldComponentInfo*)componentInfo.Objects;
    //     if (objectInfo == null)
    //     {
    //         return;
    //     }
    //
    //     if (isVisible)
    //         ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 255, 0, 255));
    //
    //     if (ImGui.TreeNode($"{treePrefix}{objectInfo->ComponentType} Component Node (ptr = {(long)node:X}, component ptr = {(long)compNode->Component:X}) child count = {childCount}  ###{(long)node}"))
    //     {
    //         if (ImGui.IsItemHovered())
    //             this.DrawOutline(node);
    //
    //         if (isVisible)
    //         {
    //             ImGui.PopStyleColor();
    //             popped = true;
    //         }
    //
    //         ImGui.Text("Node: ");
    //         ImGui.SameLine();
    //         ImGuiHelpers.ClickToCopyText($"{(ulong)node:X}");
    //         ImGui.SameLine();
    //         Util.ShowStruct(*compNode, (ulong)compNode);
    //         ImGui.Text("Component: ");
    //         ImGui.SameLine();
    //         ImGuiHelpers.ClickToCopyText($"{(ulong)compNode->Component:X}");
    //         ImGui.SameLine();
    //
    //         switch (objectInfo->ComponentType)
    //         {
    //             case ComponentType.Button: Util.ShowStruct(*(AtkComponentButton*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.Slider: Util.ShowStruct(*(AtkComponentSlider*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.Window: Util.ShowStruct(*(AtkComponentWindow*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.CheckBox: Util.ShowStruct(*(AtkComponentCheckBox*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.GaugeBar: Util.ShowStruct(*(AtkComponentGaugeBar*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.RadioButton: Util.ShowStruct(*(AtkComponentRadioButton*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.TextInput: Util.ShowStruct(*(AtkComponentTextInput*)compNode->Component, (ulong)compNode->Component); break;
    //             case ComponentType.Icon: Util.ShowStruct(*(AtkComponentIcon*)compNode->Component, (ulong)compNode->Component); break;
    //             default: Util.ShowStruct(*compNode->Component, (ulong)compNode->Component); break;
    //         }
    //
    //         this.PrintResNode(node);
    //         this.PrintNode(componentInfo.RootNode);
    //
    //         switch (objectInfo->ComponentType)
    //         {
    //             case ComponentType.TextInput:
    //                 var textInputComponent = (AtkComponentTextInput*)compNode->Component;
    //                 ImGui.Text($"InputBase Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText1.StringPtr))}");
    //                 ImGui.Text($"InputBase Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->AtkComponentInputBase.UnkText2.StringPtr))}");
    //                 ImGui.Text($"Text1: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText1.StringPtr))}");
    //                 ImGui.Text($"Text2: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText2.StringPtr))}");
    //                 ImGui.Text($"Text3: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText3.StringPtr))}");
    //                 ImGui.Text($"Text4: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText4.StringPtr))}");
    //                 ImGui.Text($"Text5: {Marshal.PtrToStringAnsi(new IntPtr(textInputComponent->UnkText5.StringPtr))}");
    //                 break;
    //         }
    //
    //         ImGui.PushStyleColor(ImGuiCol.Text, 0xFFFFAAAA);
    //         if (ImGui.TreeNode($"Node List##{(ulong)node:X}"))
    //         {
    //             ImGui.PopStyleColor();
    //
    //             for (var i = 0; i < compNode->Component->UldManager.NodeListCount; i++)
    //             {
    //                 this.PrintNode(compNode->Component->UldManager.NodeList[i], false, $"[{i}] ");
    //             }
    //
    //             ImGui.TreePop();
    //         }
    //         else
    //         {
    //             ImGui.PopStyleColor();
    //         }
    //
    //         ImGui.TreePop();
    //     }
    //     else if (ImGui.IsItemHovered())
    //     {
    //         this.DrawOutline(node);
    //     }
    //
    //     if (isVisible && !popped)
    //         ImGui.PopStyleColor();
    // }
    //
    // private void PrintResNode(AtkResNode* node)
    // {
    //     ImGui.Text($"NodeID: {node->NodeID}");
    //     ImGui.SameLine();
    //     if (ImGui.SmallButton($"T:Visible##{(ulong)node:X}"))
    //     {
    //         node->Flags ^= 0x10;
    //     }
    //
    //     ImGui.SameLine();
    //     if (ImGui.SmallButton($"C:Ptr##{(ulong)node:X}"))
    //     {
    //         ImGui.SetClipboardText($"{(ulong)node:X}");
    //     }
    //
    //     ImGui.Text(
    //         $"X: {node->X} Y: {node->Y} " +
    //         $"ScaleX: {node->ScaleX} ScaleY: {node->ScaleY} " +
    //         $"Rotation: {node->Rotation} " +
    //         $"Width: {node->Width} Height: {node->Height} " +
    //         $"OriginX: {node->OriginX} OriginY: {node->OriginY}");
    //     ImGui.Text(
    //         $"RGBA: 0x{node->Color.R:X2}{node->Color.G:X2}{node->Color.B:X2}{node->Color.A:X2} " +
    //         $"AddRGB: {node->AddRed} {node->AddGreen} {node->AddBlue} " +
    //         $"MultiplyRGB: {node->MultiplyRed} {node->MultiplyGreen} {node->MultiplyBlue}");
    // }
    //
    // private bool DrawUnitListHeader(int index, uint count, ulong ptr, bool highlight)
    // {
    //     ImGui.PushStyleColor(ImGuiCol.Text, highlight ? 0xFFAAAA00 : 0xFFFFFFFF);
    //     if (!string.IsNullOrEmpty(this.searchInput) && !this.doingSearch)
    //     {
    //         ImGui.SetNextItemOpen(true, ImGuiCond.Always);
    //     }
    //     else if (this.doingSearch && string.IsNullOrEmpty(this.searchInput))
    //     {
    //         ImGui.SetNextItemOpen(false, ImGuiCond.Always);
    //     }
    //
    //     var treeNode = ImGui.TreeNode($"{this.listNames[index]}##unitList_{index}");
    //     ImGui.PopStyleColor();
    //
    //     ImGui.SameLine();
    //     ImGui.TextDisabled($"C:{count}  {ptr:X}");
    //     return treeNode;
    // }
    //
    // private void DrawUnitBaseList()
    // {
    //     var foundSelected = false;
    //     var noResults = true;
    //     var stage = this.getAtkStageSingleton();
    //
    //     var unitManagers = &stage->RaptureAtkUnitManager->AtkUnitManager.DepthLayerOneList;
    //
    //     var searchStr = this.searchInput;
    //     var searching = !string.IsNullOrEmpty(searchStr);
    //
    //     for (var i = 0; i < UnitListCount; i++)
    //     {
    //         var headerDrawn = false;
    //
    //         var highlight = this.selectedUnitBase != null && this.selectedInList[i];
    //         this.selectedInList[i] = false;
    //         var unitManager = &unitManagers[i];
    //
    //         var unitBaseArray = &unitManager->AtkUnitEntries;
    //
    //         var headerOpen = true;
    //
    //         if (!searching)
    //         {
    //             headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
    //             headerDrawn = true;
    //             noResults = false;
    //         }
    //
    //         for (var j = 0; j < unitManager->Count && headerOpen; j++)
    //         {
    //             var unitBase = unitBaseArray[j];
    //             if (this.selectedUnitBase != null && unitBase == this.selectedUnitBase)
    //             {
    //                 this.selectedInList[i] = true;
    //                 foundSelected = true;
    //             }
    //
    //             var name = Marshal.PtrToStringAnsi(new IntPtr(unitBase->Name));
    //             if (searching)
    //             {
    //                 if (name == null || !name.ToLower().Contains(searchStr.ToLower())) continue;
    //             }
    //
    //             noResults = false;
    //             if (!headerDrawn)
    //             {
    //                 headerOpen = this.DrawUnitListHeader(i, unitManager->Count, (ulong)unitManager, highlight);
    //                 headerDrawn = true;
    //             }
    //
    //             if (headerOpen)
    //             {
    //                 var visible = (unitBase->Flags & 0x20) == 0x20;
    //                 ImGui.PushStyleColor(ImGuiCol.Text, visible ? 0xFF00FF00 : 0xFF999999);
    //
    //                 if (ImGui.Selectable($"{name}##list{i}-{(ulong)unitBase:X}_{j}", this.selectedUnitBase == unitBase))
    //                 {
    //                     this.selectedUnitBase = unitBase;
    //                     foundSelected = true;
    //                     this.selectedInList[i] = true;
    //                 }
    //
    //                 ImGui.PopStyleColor();
    //             }
    //         }
    //
    //         if (headerDrawn && headerOpen)
    //         {
    //             ImGui.TreePop();
    //         }
    //
    //         if (this.selectedInList[i] == false && this.selectedUnitBase != null)
    //         {
    //             for (var j = 0; j < unitManager->Count; j++)
    //             {
    //                 if (this.selectedUnitBase == null || unitBaseArray[j] != this.selectedUnitBase) continue;
    //                 this.selectedInList[i] = true;
    //                 foundSelected = true;
    //             }
    //         }
    //     }
    //
    //     if (noResults)
    //     {
    //         ImGui.TextDisabled("No Results");
    //     }
    //
    //     if (!foundSelected)
    //     {
    //         this.selectedUnitBase = null;
    //     }
    //
    //     if (this.doingSearch && string.IsNullOrEmpty(this.searchInput))
    //     {
    //         this.doingSearch = false;
    //     }
    //     else if (!this.doingSearch && !string.IsNullOrEmpty(this.searchInput))
    //     {
    //         this.doingSearch = true;
    //     }
    // }
    //
    // private Vector2 GetNodePosition(AtkResNode* node)
    // {
    //     var pos = new Vector2(node->X, node->Y);
    //     var par = node->ParentNode;
    //     while (par != null)
    //     {
    //         pos *= new Vector2(par->ScaleX, par->ScaleY);
    //         pos += new Vector2(par->X, par->Y);
    //         par = par->ParentNode;
    //     }
    //
    //     return pos;
    // }
    //
    // private Vector2 GetNodeScale(AtkResNode* node)
    // {
    //     if (node == null) return new Vector2(1, 1);
    //     var scale = new Vector2(node->ScaleX, node->ScaleY);
    //     while (node->ParentNode != null)
    //     {
    //         node = node->ParentNode;
    //         scale *= new Vector2(node->ScaleX, node->ScaleY);
    //     }
    //
    //     return scale;
    // }
    //
    // private bool GetNodeVisible(AtkResNode* node)
    // {
    //     if (node == null) return false;
    //     while (node != null)
    //     {
    //         if ((node->Flags & (short)NodeFlags.Visible) != (short)NodeFlags.Visible) return false;
    //         node = node->ParentNode;
    //     }
    //
    //     return true;
    // }
    //
    // private void DrawOutline(AtkResNode* node)
    // {
    //     var position = this.GetNodePosition(node);
    //     var scale = this.GetNodeScale(node);
    //     var size = new Vector2(node->Width, node->Height) * scale;
    //
    //     var nodeVisible = this.GetNodeVisible(node);
    //
    //     position += ImGuiHelpers.MainViewport.Pos;
    //
    //     ImGui.GetForegroundDrawList(ImGuiHelpers.MainViewport).AddRect(position, position + size, nodeVisible ? 0xFF00FF00 : 0xFF0000FF);
    // }
}
