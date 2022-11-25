using System;
using System.Runtime.InteropServices;

namespace Pong
{
    public class Utils<T> where T : new()
    {
        public static byte[] ToBytes(object str)
        {
            byte[] arr = new byte[Marshal.SizeOf(str)];
            IntPtr pnt = Marshal.AllocHGlobal(Marshal.SizeOf(str));
            Marshal.StructureToPtr(str, pnt, false);
            Marshal.Copy(pnt, arr, 0, Marshal.SizeOf(str));
            Marshal.FreeHGlobal(pnt);
            return arr;
        }

        public static T FromBytes(byte[] arr)
        {
            T str = new T();
            int size = Marshal.SizeOf(str);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(arr, 0, ptr, size);
            str = (T)Marshal.PtrToStructure(ptr, str.GetType());
            Marshal.FreeHGlobal(ptr);
            return str;
        }
    }
}
