namespace Clay_cs.MonoGame;


// Very basic struct with only int id.
// There are cases for example in custom rendering, we have a void*, then
// we will always cast void* into this struct so we can ALWAYS assume the first field is int id.
// This generally means void* = structs with first first int id.
// This is essentially a parent-child class relationship, but for structs because we cannot cast void* to classes
/*[StructLayout(LayoutKind.Sequential)]
public struct CustomRendererData()
{
    public int id;
    public bool maintainAspectRatio = false;
}*/

// To hide the uglyness of this maybe I should just encode into a byte and hide it away from anyones eyes?
// first bit the bool, remaining 7 bits the id?
// Maybe a bigger type to support expansion?
public static class CustomElementData
{
    // This class now is essentially useless as aspect ratio and other render stuff now seemingly magically and automatically work??!?!?!
    // I will use it for future expansion anyway
    public const uint IdMask = 255;
    public const int FlagsShift =8;

    [Flags] public enum Flags : uint
    {
        None = 0,
        //PreserveAspect =1u <<0,
    }

    public static unsafe void* SetData(int id, Flags rendererFlags)
    {
        if ((uint)id > IdMask) throw new ArgumentOutOfRangeException(nameof(id), "id must fit in 8 bits. Max amount of id's is 255");
         
        uint token = ((uint)id & IdMask) | ((uint)rendererFlags << FlagsShift);
        return (void*)(nint)token;
    }

    public static unsafe void* SetData(int id)
    {
        return SetData(id, Flags.None);
    }

    public static unsafe void DecodeData(void* data, out int id, out Flags flags)
    {
        uint token = (uint)(nint)(data);
        id = (int)(token & IdMask);
        flags = (Flags)((token >> FlagsShift) &0xFFFFFFu);
    }
}