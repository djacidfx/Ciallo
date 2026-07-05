# 视频文案：介绍矢量（油漆桶）填色

这个视频将会向你介绍如何使用**矢量油漆桶**工具给你的手绘图上色

其实“矢量油漆桶”这个工具的名字已经说明了它的用途：做矢量化的填充工作。
现在软件有线稿（演示），使用矢量油漆桶工具点击闭合的区域，颜色就填充上了。
点击的位置被这个新建的带中心点的圆记录下来，这个小圆我们叫它填充标记
随后我可以随意拖拽填充标记的位置（演示），填色块的位置便跟随着标记改变。
标记的中心位置就代表油漆桶填色的目标位置，修改标记，我们就拥有了可编辑的，矢量化的填色。

你可能想问：除了填色位置以外，还有什么可以修改呢？
这张图里**一切**都可以修改
我可以改变填色的颜色（演示），改变标记的形状，改变外轮廓的形状，改变轮廓线用的笔刷，一切都可以修改。这就是为什么我叫它**矢量**油漆桶填色

你可能觉得：Em...可以编辑确实不错啦，但是我下笔，填色都很准根本就不需要后期编辑，这个工具可能对我来说没啥意义。
或许是这样，但是它**真正的隐含的**用途在于：我可以选中上一帧所有的标记，ctrlc复制，然后ctrlv全部粘贴到下一帧，然后轻微的移动各个标记到合适的位置，这样整张图就被快速的上色。
这个才是我们的矢量油漆桶工具最核心的目标：让图像的填色能得到重复使用，只要填好一张图一个角色，全部相似的图，都能复用它。在做帧动画的时候可能有几千张手绘的有高度相似性的图，希望这个矢量油漆桶工具能帮你省下大量的填色工作。

如果要高效的使用这个矢量油漆桶工具，有一些点需要注意：
矢量油漆桶要求专门的填色图层，所有的操作必须在这种特殊的图层里完成。这个图层会自动帮你在**闭合**的区域铺一层灰底，如果你不想要这个行为，你可以在油漆桶工具的设置里把它关闭或者换个颜色。
再有这个工具**仅考虑中线**，不会考虑线条的宽度或者线条实际所占的像素，它仅让这个中心线作为闭合的轮廓。这样子有一些好处，比如你不用费劲的填充线条占领的像素。隐藏线条后可以看到我们色块是完全紧贴的，中间没有一丝缝隙。
但是，仅考虑中线也更容易出现一些未闭合的情况。比如这里（演示），看上去闭合的像素实际上里面的中线并没有闭合。
所以为了避免这种情况，我准备了**修剪工具**，帮你修剪掉线头，那些为了稳定相交而绘制的额外小线头；**缝隙修补工具**，激活这个工具后它会把所有的闭合区域都填上一个随机的颜色，并且尝试找出所有的可疑的未闭合的端点，可疑的缝隙会用这种动态的虚线高亮显示。如果你觉得它找的正确，可以把鼠标移动上去，点击，线条就会自动的做轻微的形变帮你闭合上目标小缝隙。
除了上面说的修改工具来避免缝隙，你还可以在使用笔刷工具在绘制的时候就开启**吸附功能**。把光标移动到画布上，会有小黄点实时的提示你吸附的目标位置，线条画好后就会自动的吸附到小黄点附近。开启缝隙修补之后，你可以看到被吸附的地方是没有任何缝隙的。

⚠️ TODO：如果你喜欢这个功能，来Steam上查找Ciallo；

## English voiceover script for TTS
## THE SCENE: 
A quiet, professional remote workspace.
### DIRECTOR'S NOTES
Pace: Regular product explanation on YouTube.

#### TRANSCRIPT
[calmly] In this video, I want to show you a fast way to color hand-drawn animation, with the "Vector Bucket Fill" tool.

The name already gives away the idea, it is a paint bucket tool, but [enunciate every syllable] "vectorized".

Let me show you how it works. Here, I already have some line drawings.

I choose the Vector Bucket Fill tool, click inside a closed area, and the color appears.

[slightly excited] But notice this.

The click is not just a one-time action. It creates this small circle, with a center point.

This is called a "fill marker."

And now, I can drag that marker anywhere I want, and the filled area follows it.

[calmly] The target position of bucket fill is the center of marker. By modifying the marker, we get the "editable, vectorized" flood fill or bucket fill.

So you might ask, besides the position of the fill, what else can I edit?

[pause] The answer is, everything.

[slightly excited] I can change the fill color, I can change the marker shape, I can change the outer contour, and I can even change the brush used by the outline.

This whole drawing is still alive, still editable.

That is why I call it, "Vector Bucket Fill."

---

Now, you may be thinking, okay, editable coloring is nice, but my drawings are already clean. My colors are already accurate. I do not really need to fix them later.

And honestly, maybe that is true.

But The real power of this tool is not correction, but [pause] reuse.

[calmly] Let me show you this. I can select all the fill markers from the previous frame, press Control C copy them, then go to the next frame, press Control V paste them.

After that, the whole frame is nearly colored. I only need to drag each marker into the right place.

And just like that, the whole frame is colored.

[confidently] This is the core purpose of "Vector Bucket Fill", to make coloring reusable.

Once you color one drawing, or one character, you can reuse that work across all the similar drawings.

And I beieve when making frame-by-frame animation, that matters a lot.

You may have thousands of hand-drawn frames to color, all very similar to each other. This tool will save you a huge amount of work on coloring.

---

[calmly] Now, to use "Vector Bucket Fill" efficiently, there are a few things to know.

First, it needs a dedicated fill layer.

All bucket-fill operations happen inside this special layer.

By default, the layer automatically adds a gray base under closed area.

If you do not want that, you can turn it off in the tool settings, or simply change the color.

Second, this tool only looks at the center line.

Not the stroke width.

Not the actual pixels covered by the stroke.

Only the center line is used as the closed contour.

This has a very nice benefit.

You do not need to carefully paint around the pixels occupied by the line itself.

When I hide the line art, you can see the color shapes fit together tightly, with no gaps at all.

But there is one catch.

Because the tool only looks at center lines, some areas may look closed on the screen, while the actual center lines are still open.

For example, here.

The pixels look connected.

But inside the stroke, the center lines are not actually closed.

---

So, to handle this, I prepared a "Trim" tool.

It helps remove loose line ends, especially those tiny extra strokes to make intersections more stable.

I also prepared a "Gap Repair" tool.

When this tool is active, it fills all closed regions with random colors, then tries to find suspicious open endpoints.

Possible gaps are highlighted with animated dashed lines.

If the highlighted gap looks correct, just move the cursor over it, and click.

The line will gently deform, and close that small gap automatically.

Besides repairing gaps afterward, you can also prevent them while drawing.

When using the brush tool, turn on "Snapping."

As you move the cursor over the canvas, small yellow dots show the snapping targets in real time.

After the stroke is drawn, it snaps near the yellow dot automatically.

[pleased] And once "Gap Repair" is turned on, you can see it clearly, the snapped area has no gap.

Not even a tiny one.

[warmly] If you like this feature, search for "Ciallo" on Steam.
