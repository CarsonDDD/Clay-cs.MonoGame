using System.Runtime.InteropServices;

namespace Clay_cs.MonoGame;


// Very basic struct with only int id.
// There are cases for example in custom rendering, we have a void*, then
// we will always cast void* into this struct so we can ALWAYS assume the first field is int id.
// This generally means void* = structs with first first int id.
// This is essentially a parent-child class relationship, but for structs because we cannot cast void* to classes
[StructLayout(LayoutKind.Sequential)]
public struct IDStruct
{
    public int id;
}