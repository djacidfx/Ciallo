using Godot;

namespace Ciallo.GuiControl;

internal static class TimelineFrameGeometry
{
    public static float FrameToX(int frame, float pixelsPerFrame, float scrollOffset) =>
        frame * pixelsPerFrame - scrollOffset;

    public static int XToFrameFloor(float x, float pixelsPerFrame, float scrollOffset) =>
        pixelsPerFrame > 0f ? Mathf.FloorToInt((x + scrollOffset) / pixelsPerFrame) : 0;

    public static int XToFrameRounded(float x, float pixelsPerFrame, float scrollOffset) =>
        pixelsPerFrame > 0f ? Mathf.RoundToInt((x + scrollOffset) / pixelsPerFrame) : 0;

    public static (int Start, int End) VisibleFrameRange(
        float width,
        float pixelsPerFrame,
        float scrollOffset,
        int leadingBufferFrames = 1,
        int trailingBufferFrames = 2)
    {
        if (pixelsPerFrame <= 0f)
            return (0, 0);

        int start = Mathf.FloorToInt(scrollOffset / pixelsPerFrame) - leadingBufferFrames;
        int end = Mathf.FloorToInt((scrollOffset + width) / pixelsPerFrame) + trailingBufferFrames;
        return (start, end);
    }
}
