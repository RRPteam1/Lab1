using System.Runtime.InteropServices;

namespace Pong
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TOP
    {
        public int Score;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)] //max size of nickname
        public string Name;
        public override string ToString() => $"Name: {Name}\tScore: {Score}";
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct TOP_ARRAY
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)] //top 10
        public TOP[] arrStruct;
    }
}