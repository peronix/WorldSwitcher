using FFXIVClientStructs.FFXIV.Component.GUI;

namespace WorldSwitcher; 

public static unsafe partial class UiHelper {
    public static void SetSize(AtkResNode* node, int? width, int? height) {
        if (width != null && width >= ushort.MinValue && width <= ushort.MaxValue) node->Width = (ushort) width.Value;
        if (height != null && height >= ushort.MinValue && height <= ushort.MaxValue) node->Height = (ushort) height.Value;
        node->DrawFlags |= 0x1;
    }

    public static void SetPosition(AtkResNode* node, float? x, float? y) {
        if (x != null) node->X = x.Value;
        if (y != null) node->Y = y.Value;
        node->DrawFlags |= 0x1;
    }
    
    public static void SetWindowSize(AtkComponentNode* windowNode, ushort? width, ushort? height) {
        if (((AtkUldComponentInfo*) windowNode->Component->UldManager.Objects)->ComponentType != ComponentType.Window) return;

        width ??= windowNode->AtkResNode.Width;
        height ??= windowNode->AtkResNode.Height;

        if (width < 64) width = 64;
        if (height < 16) height = 16;

        SetSize(&windowNode->AtkResNode, width, height);  // Window
        var n = windowNode->Component->UldManager.RootNode;
        SetSize(n, width, height);  // Collision
        n = n->PrevSiblingNode;
        SetSize(n, (ushort)(width - 14), null); // Header Collision
        n = n->PrevSiblingNode;
        SetSize(n, width, height); // Background
        n = n->PrevSiblingNode;
        SetSize(n, width, height); // Focused Border
        n = n->PrevSiblingNode;
        SetSize(n, width, height); // Gradient
        n = n->PrevSiblingNode;
        SetSize(n, (ushort) (width - 5), null); // Header Node
        n = n->ChildNode;
        SetSize(n, (ushort) (width - 20), null); // Header Seperator
        n = n->PrevSiblingNode;
        SetPosition(n, width - 33, 6); // Close Button
        n = n->PrevSiblingNode;
        SetPosition(n, width - 47, 8); // Gear Button
        n = n->PrevSiblingNode;
        SetPosition(n, width - 61, 8); // Help Button

        windowNode->AtkResNode.DrawFlags |= 0x1;
    }
}
