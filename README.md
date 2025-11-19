todo:
- rounded corners
- figure out better way to do fonts
- find way to have things be click-through-transparent, so you can have like an overlay over the entire ui.
- Finish Examples and implement features required for said examples
- Add readme to the examples

# Clay.MonoGame

A MonoGame renderer for the [csharp bindings](https://github.com/Orcolom/clay-cs) for [Clay.h](https://github.com/nicbarker/clay)

This README will focus on the MonoGame specifics, for more complete documentation pages check out the [Clay Github](https://github.com/nicbarker/clay) or the [Clay-cs Github](https://github.com/Orcolom/clay-cs)

To stay in alignment with the original Clay, Clay-cs.MonoGame is meant to be very limited in scope, only containing the basic functionality to render as well as helpers/utils to reduce SOME boilderplating. This is far from a proper and fuller "UI Library" for MonoGame, but in its bare boniness, It is fully intended to be used to create the one ideal for any given project (of any level of complexity, as this can be used as-is, or greatly expanded to meet requirements or fit with current code.) 

---

# Install

Make sure you have monogame installed.

remember to also `git clone --recurse-submodules https://github.com/CarsonDDD/Clay-cs.MonoGame.git` if you want access to the library

Add `Clay-cs.MonoGame.csporj` as a package reference or build the library yourself to add the dll

