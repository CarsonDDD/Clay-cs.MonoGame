# Examples

## 1. Introduction To Clay

Shows the renderer doing the IntroductionToClay example from the original project

## 2. Image Viewer

Demo showing off using `Texture2D`'s inside UIs, requiring `ClayTexture2DCollection` to manage the pointers and lifetimes of the textures, much like the original Clay-cs `ClayStringCollection`.

## 3. Pong With HUD

Basic Example showing off a UI used for a game. In this example the UI is rendered after the game, thus being overlayed (hud)

## 4. Custom Render Example

Example to show off how to use the Custom Renderer feature from/for Clay, using `CustomRenderRegister` to define the renderer delegate with an id, and potentially `UserDataRegister` for passing args to the renderer.

## 5. Game Inside UI

Example building off the Pong With HUD and Custom Renderer examples to show off rendering the game view inside a UI element (by defining a custom renderer which translates and renders the game), allowing the view size and scale to be determined by the layout engine