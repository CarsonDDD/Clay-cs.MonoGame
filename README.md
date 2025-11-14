todo:
- rounded corners
- figure out better way to do fonts

# Clay.MonoGame


A MonoGame renderer for the \[csharp bindings](https://github.com/Orcolom/clay-cs) for \[Clay.h](https://github.com/nicbarker/clay)


This README will focus on the MonoGame specifics, for more complete documentation pages check out the \[Clay Github](https://github.com/nicbarker/clay) or the \[Clay-cs Github](https://github.com/Orcolom/clay-cs)



---


## Current **ISSUE**


`ExecutionEngineException` with mouse pointer or hover or something?

Fixing this is beyond my level of understanding.

In the demo, after a random amount of time, moving the mouse over the UI will throw this Exception, pointing to either:

```cs
//Clay.cs

[MethodImpl(MethodImplOptions.AggressiveInlining)]
public static bool IsPointerOver(Clay_ElementId elementId)
{
	return ClayInterop.Clay_PointerOver(elementId);
}
```

or

```cs
// Game1.cs.Update

Clay.SetPointerState(new System.Numerics.Vector2(mouse.X, mouse.Y), mouse.LeftButton == ButtonState.Pressed);

```

depending on debugging.

This is not an issue with the default Raylib Renderer the [Csharp Clay Bindings](https://github.com/Orcolom/clay-cs) uses, which deeply confuses me as the sample code is nearly identical.

Removing/Commenting out the `Clay.OnHover` from `Game1.Draw` seems to stop the Exception, but, you know...


---



\# Install

Make sure you have monogame installed.

remember to also `git clone --recurse-submodules blah blah` if you want access to the library

Add `Clay-cs.MonoGame.csporj` as a package reference or build the library yourself to add the dll

