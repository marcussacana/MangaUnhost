//UnixGate.cs - BY MARCUSSACANA
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace UnixGate
{
    internal static class UnixGate
    {

        static bool Enabled;

        static byte[] OriLoadLibExW;
        static byte[] JmpLoadLibExW;

        static byte[] OriGetProc;
        static byte[] JmpGetProc;

        static IntPtr LoadLibraryExWAddr;
        static IntPtr GetProcAddr;
        static IntPtr BaseLoadLibExAddr;
        static IntPtr BaseGetProcAddr;
        static IntPtr dlsymAddr;
        static IntPtr dlopenAddr;
        static IntPtr GetModFN;

        static IntPtr GetProcHookAddr;
        static IntPtr LoadLibExWHookAddr;

        static IntPtr Kernel;
        static IntPtr KernelBase;

        internal static void Initialize()
        {

            if (LoadLibraryExWAddr != IntPtr.Zero)
                return;

            Kernel = LoadLibraryW("kernel32");
            KernelBase = LoadLibraryW("kernelbase");

            LoadLibraryExWAddr = GetProcAddressManaged(Kernel, "LoadLibraryExW");
            GetProcAddr = GetProcAddressManaged(Kernel, "GetProcAddress");
            GetModFN = GetProcAddressManaged(Kernel, "GetModuleFileNameA");

            BaseGetProcAddr = GetProcAddressManaged(KernelBase, "GetProcAddress");
            BaseLoadLibExAddr = GetProcAddressManaged(KernelBase, "LoadLibraryExW");

            if (BaseLoadLibExAddr == IntPtr.Zero || BaseGetProcAddr == IntPtr.Zero)
                throw new Exception("Unsupported Wine Version");

            OriLoadLibExW = Read(LoadLibraryExWAddr, (uint)JmpSize);
            OriGetProc = Read(GetProcAddr, (uint)JmpSize);

            dGetProcAddr = new GetProcAddressDel(GetProcHook);
            dLoadLibExW = new LoadLibraryExWDel(LoadLibraryExHook);

            dLoadLibExWReal = (LoadLibraryExWDel)Marshal.GetDelegateForFunctionPointer(BaseLoadLibExAddr, typeof(LoadLibraryExWDel));

            dGetModuleFileName = (GetModuleFileNameDel)Marshal.GetDelegateForFunctionPointer(GetModFN, typeof(GetModuleFileNameDel));

            GetProcHookAddr = Marshal.GetFunctionPointerForDelegate(dGetProcAddr);
            LoadLibExWHookAddr = Marshal.GetFunctionPointerForDelegate(dLoadLibExW);

            dlopenAddr = GetSymbol("libwine.so.1!wine_dlopen");
            dlsymAddr = GetSymbol("libwine.so.1!wine_dlsym");

            if (dlopenAddr == IntPtr.Zero || dlsymAddr == IntPtr.Zero)
                throw new Exception("Invalid Wine Envoriment: Failed to Load the libwine.so");

            byte[] PEBData = NTCurPebData;
            var PEBAddr = Marshal.AllocHGlobal(PEBData.Length);
            Write(PEBAddr, PEBData, Protection.PAGE_EXECUTE_READWRITE);
            NtCurrentPeb = (Ret0)Marshal.GetDelegateForFunctionPointer(PEBAddr, typeof(Ret0));

            ddlopen = (dlopenDel)Marshal.GetDelegateForFunctionPointer(dlopenAddr, typeof(dlopenDel));
            ddlsym = (dlsymDel)Marshal.GetDelegateForFunctionPointer(dlsymAddr, typeof(dlsymDel));


            JmpGetProc = AssembleJump(GetProcAddr, GetProcHookAddr);
            JmpLoadLibExW = AssembleJump(LoadLibraryExWAddr, LoadLibExWHookAddr);
        }

        internal static void Enable() {
            if (Enabled)
                return;

            Initialize();

            Write(GetProcAddr, JmpGetProc);
            Write(LoadLibraryExWAddr, JmpLoadLibExW);

            Enabled = true;
        }

        internal static void Disable() {
            if (!Enabled)
                return;

            Write(GetProcAddr, OriGetProc);
            Write(LoadLibraryExWAddr, OriLoadLibExW);

            Enabled = false;
        }

        static IntPtr GetProcHook(IntPtr hModule, IntPtr Proc) {
            IntPtr WinFunc = GetProcAddressManaged(hModule, Proc);
            if (WinFunc == IntPtr.Zero && hModule != IntPtr.Zero)
            {
                string ModuleFN = GetModuleFileName(hModule);
                if (string.IsNullOrEmpty(ModuleFN))                
                    return dlsym(hModule, Proc);
            }
            return WinFunc;
        }
        
        static IntPtr LoadLibraryExHook(string Name, IntPtr Reserved, LoadLibraryFlags Flags) {
            IntPtr hModule = dLoadLibExWReal(Name, Reserved, Flags);
            if (hModule == IntPtr.Zero) 
                return dlopen(Name, RTLD_NOW);
            return hModule;
        }

        static IntPtr GetProcAddressManaged(IntPtr hModule, string ProcName) {
            IntPtr Proc = Marshal.StringToHGlobalAnsi(ProcName);
            IntPtr Rst = GetProcAddressManaged(hModule, Proc);
            Marshal.FreeHGlobal(Proc);
            return Rst;
        }
        static IntPtr GetProcAddressManaged(IntPtr hModule, IntPtr ProcNamePtr) { 
            ushort Ordinal  = 0;

            if (ProcNamePtr.ToUlong() > ushort.MaxValue)
            {
                var ProcedureName = new ANSI_STRING();
                var ProcName = Marshal.PtrToStringAnsi(ProcNamePtr);

                ProcedureName.Length = (short)ProcName.Length;
                ProcedureName.MaximumLength = ProcedureName.Length;
                ProcedureName.Buffer = ProcName;

                var Addr = Marshal.AllocHGlobal(ProcName.Length + 5);
                Marshal.StructureToPtr(ProcedureName, Addr, false);
                ProcNamePtr = Addr;
            }
            else            
                Ordinal = (ushort)ProcNamePtr.ToUlong();

           
            IntPtr hMapped = BasepMapModuleHandle(hModule, false);
           
                  
            var Status = LdrGetProcedureAddress(hMapped, ProcNamePtr, Ordinal, out IntPtr fnExp);

            Marshal.FreeHGlobal(ProcNamePtr);

            if (!NT_SUCCESS(Status))
            {
                SetLastError(Status.ToInt32());
                return IntPtr.Zero;
            }

            if (fnExp == hMapped)
            {
                if (ProcNamePtr.ToUlong() > ushort.MaxValue)
                    SetLastError(0xC0000139);//STATUS_ENTRYPOINT_NOT_FOUND
                else
                    SetLastError(0xC0000138);//STATUS_ORDINAL_NOT_FOUND

                return IntPtr.Zero;
            }
           
            return fnExp;
        }

        static IntPtr BasepMapModuleHandle(IntPtr hModule, bool AsDataFile) {
            if (hModule == IntPtr.Zero)
                return GetCurrentImageBaseAddress();

            if ((hModule.ToUlong() & 1) != 0 && AsDataFile)
                return IntPtr.Zero;

            return hModule;
        }

        static IntPtr GetCurrentImageBaseAddress() {
            var PEB = NtCurrentPeb();
            return Marshal.ReadIntPtr(PEB, IntPtr.Size == 8 ? 0x10 : 0x08);
        }

        static bool NT_SUCCESS(IntPtr STATUS) => STATUS.ToInt32() >= 0; 


        static Ret0 NtCurrentPeb;
        delegate IntPtr Ret0();

        /*
        #include <intrin.h>
        void* NtCurrentPeb()
        {
        #ifdef _WIN64
            return (void*)__readgsqword(0x60);
        #else
            __asm {
                mov eax, fs:[0x30];
            }
        #endif
        }
        */

        static byte[] NTCurPebData
        {
            get {
                if (IntPtr.Size == 8)
                    return NTCurPebx64;
                return NTCurPebx86;
            }
        }
        static readonly byte[] NTCurPebx64 = new byte[] {
            0x65, 0x48, 0x8B, 0x04, 0x25, 0x60,
            0x00, 0x00, 0x00, 0xC2, 0x00, 0x00
        };

        static readonly byte[] NTCurPebx86 = new byte[] { 
            0x64, 0xA1, 0x30, 0x00, 0x00, 0x00, 0xC2, 0x00, 0x00 
        };

        const uint SYMOPT_DEFERRED_LOADS = 0x00000004;
        const uint SYMOPT_PUBLICS_ONLY = 0x00004000;

        internal const uint RTLD_NOW = 0x002;

        static GetProcAddressDel dGetProcAddr;
        static LoadLibraryExWDel dLoadLibExW;

        static LoadLibraryExWDel dLoadLibExWReal;

        static dlsymDel ddlsym;
        static dlopenDel ddlopen;

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr GetProcAddressDel(IntPtr hModule, IntPtr Proc);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, CharSet = CharSet.Unicode)]
        delegate IntPtr LoadLibraryExWDel(string lpFileName, IntPtr hReservedNull, LoadLibraryFlags dwFlags);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr dlsymDel(IntPtr hModule, IntPtr Symbol, IntPtr StrError, uint ErrorSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        delegate IntPtr dlopenDel(IntPtr lpFileName, uint Flags, IntPtr StrError, uint ErrorSize);

        internal static IntPtr dlsym(IntPtr hModule, string Symbol) => dlsym(hModule, Marshal.StringToHGlobalAnsi(Symbol));
        internal static IntPtr dlopen(string lpFilename, uint Flags) => dlopen(Marshal.StringToHGlobalAnsi(lpFilename), Flags);

        internal static IntPtr dlsym(IntPtr hModule, IntPtr Symbol) {
            if (IntPtr.Size == 8)
                return UnixFastCall(dlsymAddr, hModule, Symbol, IntPtr.Zero, 0);
            return ddlsym(hModule, Symbol, IntPtr.Zero, 0);
        }

        internal static IntPtr dlopen(IntPtr lpFileName, uint Flags) {
            if (IntPtr.Size == 8)
                return UnixFastCall(dlopenAddr, lpFileName, Flags, IntPtr.Zero, 0);
            return ddlopen(lpFileName, Flags, IntPtr.Zero, 0);
        }

        public static IntPtr UnixFastCall(IntPtr Function, params object[] Paramters) {
            IntPtr[] Args = new IntPtr[Paramters.Length];
            for (int i = 0; i < Paramters.Length; i++) {
                object Paramter = Paramters[i];
                IntPtr NParamter = IntPtr.Zero;
                switch (Type.GetTypeCode(Paramter.GetType()))
                {
                    case TypeCode.Boolean:
                        NParamter = new IntPtr(((bool)Paramter) ? 1 : 0);
                        break;
                    case TypeCode.Byte:
                        NParamter = new IntPtr((byte)Paramter);
                        break;
                    case TypeCode.SByte:
                        NParamter = new IntPtr((sbyte)Paramter);
                        break;
                    case TypeCode.Int16:
                        NParamter = new IntPtr((short)Paramter);
                        break;
                    case TypeCode.UInt16:
                        NParamter = new IntPtr((ushort)Paramter);
                        break;
                    case TypeCode.Int32:
                        NParamter = new IntPtr((int)Paramter);
                        break;
                    case TypeCode.UInt32:
                        NParamter = new IntPtr((uint)Paramter);
                        break;
                    case TypeCode.Int64:
                        NParamter = new IntPtr((long)Paramter);
                        break;
                    case TypeCode.UInt64:
                        NParamter = new IntPtr(unchecked((long)(ulong)Paramter));
                        break;
                    case TypeCode.Char:
                        NParamter = new IntPtr((char)Paramter);
                        break;
                    case TypeCode.String:
                        NParamter = Marshal.StringToHGlobalAnsi(((string)Paramter));
                        break;
                    default:
                        var TP = Paramter.GetType();
                        if (Paramter is IntPtr)
                            NParamter = (IntPtr)Paramter;
                        else if (Paramter is UIntPtr)
                            NParamter = new IntPtr(unchecked((long)((UIntPtr)Paramter).ToUInt64()));
                        else if (TP.IsValueType && !TP.IsEnum) { //Is Struct
                            NParamter = Marshal.AllocHGlobal(Marshal.SizeOf(Paramter));
                            Marshal.StructureToPtr(Paramter, NParamter, false);
                        } else
                            throw new ArgumentException(Paramter.ToString() + " Can't be automatically parsed.");
                        break;
                }
                Args[i] = NParamter;
            }
            return UnixFastCall(Function, Args);
        }
        public static IntPtr UnixFastCall(IntPtr Function, params IntPtr[] Paramters)
        {
            List<byte> Buffer = new List<byte>();

            for (int i = 0; i < Paramters.Length; i++)
            {
                switch (i)
                {
                    case -1:
                        Buffer.AddRange(BitConverter.GetBytes(Paramters[i].ToUlong()));
                        break;
                    case 0:
                        Buffer.AddRange(new byte[] { 0x48, 0xbf });//RDI
                        goto case -1;
                    case 1:
                        Buffer.AddRange(new byte[] { 0x48, 0xbe });//RSI
                        goto case -1;
                    case 2:
                        Buffer.AddRange(new byte[] { 0x48, 0xba });//RDX
                        goto case -1;
                    case 3:
                        Buffer.AddRange(new byte[] { 0x48, 0xb9 });//RCX
                        goto case -1;
                    case 4:
                        Buffer.AddRange(new byte[] { 0x48, 0xb8 });//R8
                        goto case -1;
                    case 5:
                        Buffer.AddRange(new byte[] { 0x48, 0xb9 });//R9
                        goto case -1;
                    default:
                        Buffer.AddRange(new byte[] { 0x48, 0xb8 });//RAX
                        Buffer.AddRange(BitConverter.GetBytes(Paramters[i].ToUlong()));
                        Buffer.Add(0x50);//push rax
                        break;
                }
            }

            Buffer.AddRange(new byte[] { 0x48, 0xb8 });//RAX
            Buffer.AddRange(BitConverter.GetBytes(Function.ToUlong()));

            Buffer.AddRange(new byte[] { 0xFF, 0xE0 });//jmp RAX

            IntPtr TmpFunc = Marshal.AllocHGlobal(Buffer.Count);
            Write(TmpFunc, Buffer.ToArray(), Protection.PAGE_EXECUTE_READWRITE);

            Ret0 TmpDel = (Ret0)Marshal.GetDelegateForFunctionPointer(TmpFunc, typeof(Ret0));

            IntPtr Result = TmpDel();

            Marshal.FreeHGlobal(TmpFunc);

            return Result;
        }

        static IntPtr GetSymbol(string name) => GetSymbol(System.Diagnostics.Process.GetCurrentProcess().Handle, name);
        static IntPtr GetSymbol(IntPtr hProcess, string name)
        {
            unchecked
            {
                IntPtr ret = IntPtr.Zero;
                SymSetOptions(SYMOPT_DEFERRED_LOADS | SYMOPT_PUBLICS_ONLY | 0x40000000);
                if (SymInitialize(hProcess, null, true))
                {
                    var si = new SYMBOL_INFO();
                    si.SizeOfStruct = 592;
                    si.MaxNameLen = 512;
                    IntPtr Addr = Marshal.AllocHGlobal(1024);
                    Marshal.StructureToPtr(si, Addr, false);
#if false
                    if (SymFromName(hProcess, name, Addr))
                    {
                        ret = Marshal.ReadIntPtr(Addr, 56);
                        var str = Marshal.PtrToStringAnsi(new IntPtr((long)((ulong)Addr.ToInt64() + 84)));
                        Console.WriteLine("SymFromName Sucess 0x{0:X8}, {1}", IntPtr.Size == 8 ? ret.ToInt64() : ret.ToInt32(), str);
                    }
                    else
                        Console.WriteLine("SymFromName Failed");
#else
                    if (SymFromName(hProcess, name, Addr))
                        ret = Marshal.ReadIntPtr(Addr, 56);
#endif
                    SymCleanup(hProcess);
                }
                return ret;
            }
        }

        static readonly int JmpSize = IntPtr.Size == 8 ? 12 : 5;

        static byte[] AssembleJump(IntPtr From, IntPtr Destination)
        {
            byte[] jmp = new byte[JmpSize];
            if (IntPtr.Size == 8)
            {
                //x64
                new byte[] { 0x48, 0xb8 }.CopyTo(jmp, 0);
                BitConverter.GetBytes(unchecked((ulong)Destination.ToInt64())).CopyTo(jmp, 2);
                new byte[] { 0xFF, 0xE0 }.CopyTo(jmp, 10);
            }
            else
            {
                //x86
                jmp[0] = 0xE9;
                int Result = (int)(Destination.ToInt64() - From.ToInt64() - JmpSize);
                BitConverter.GetBytes(Result).CopyTo(jmp, 1);
            }
            return jmp;
        }

        static byte[] Read(IntPtr Address, uint Length)
        {
            byte[] Buffer = new byte[Length];
            if (!ChangeProtection(Address, Buffer.Length, Protection.PAGE_EXECUTE_READWRITE, out Protection Original))
                throw new Exception($"Falied to change the R/W memory permissions at {Address.ToInt64():X8}");
            Marshal.Copy(Address, Buffer, 0, Buffer.Length);           
            if (!ChangeProtection(Address, Buffer.Length, Original))
                throw new Exception($"Falied to restore the memory permissions at {Address.ToInt64():X8}");
            return Buffer;
        }

        static bool Write(IntPtr Address, byte[] Content, Protection? NewProtection = null)
        {
            ChangeProtection(Address, Content.Length, Protection.PAGE_EXECUTE_READWRITE, out Protection Original);

            uint Saved = (uint)Content.LongLength;
            Marshal.Copy(Content, 0, Address, Content.Length);

            if (NewProtection.HasValue)
                ChangeProtection(Address, Content.Length, NewProtection.Value);
            else
                ChangeProtection(Address, Content.Length, Original);

            if (Saved != Content.Length)
                return false;

            return true;
        }

        static bool ChangeProtection(IntPtr Address, int Range, Protection Protection, out Protection OriginalProtection)
        {
            return VirtualProtect(Address, Range, Protection, out OriginalProtection);
        }

        static bool ChangeProtection(IntPtr Address, int Range, Protection Protection)
        {
            return VirtualProtect(Address, Range, Protection, out _);
        }

        [DllImport("kernel32", SetLastError = true)]
        static extern bool VirtualProtect(IntPtr lpAddress, int dwSize, Protection flNewProtect, out Protection lpflOldProtect);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibraryW(string lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        delegate uint GetModuleFileNameDel([In] IntPtr hModule, [Out] StringBuilder lpFilename,[ In] [MarshalAs(UnmanagedType.U4)] int nSize);
        static GetModuleFileNameDel dGetModuleFileName;

        static string GetModuleFileName(IntPtr hModule)
        {
            StringBuilder fileName = new StringBuilder(1024);
            if (dGetModuleFileName(hModule, fileName, fileName.Capacity) == 0)
                return null;
            return fileName.ToString();
        }

        internal static ulong ToUlong(this IntPtr Pointer) => unchecked((ulong)Pointer.ToInt64());

        [DllImport("dbghelp.dll")]
        static extern IntPtr SymSetOptions(uint SymOptions);
        [DllImport("dbghelp.dll")]
        static extern bool SymInitialize(IntPtr hProcess, string UserSearchPath, bool fInvadeProcess);
        [DllImport("dbghelp.dll")]
        static extern bool SymCleanup(IntPtr hProcess);
        [DllImport("dbghelp.dll")]
        static extern bool SymFromName(IntPtr hProcess, string Name, IntPtr Symbol);

        [DllImport("ntdll.dll")]
        static extern IntPtr LdrGetProcedureAddress(IntPtr hModule, IntPtr FunctionName, ushort Oridinal, out IntPtr FunctionAddress);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetLastError(int ErrorCode);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern void SetLastError(uint ErrorCode);


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct SYMBOL_INFO
        {
            public uint SizeOfStruct;//+0
            public uint TypeIndex;//+4
            public ulong ReservedA;//+8
            public ulong ReservedB;//+16
            public uint Index;//+24
            public uint Size;//+28
            public ulong ModBase;//+32
            public uint Flags;//+40
            public ulong Value;//+44
            public ulong Address;//+52
            public uint Register;//+60
            public uint Scope;//+64
            public uint Tag;//+68
            public uint NameLen;//+72
            public uint MaxNameLen;//+76
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
            public string Name;//+80
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct ANSI_STRING
        {
            public short Length;
            public short MaximumLength;
            public string Buffer;
        }


        [Flags]
        enum LoadLibraryFlags : uint
        {
            None = 0,
            DONT_RESOLVE_DLL_REFERENCES = 0x00000001,
            LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
            LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
            LOAD_LIBRARY_AS_DATAFILE_EXCLUSIVE = 0x00000040,
            LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
            LOAD_LIBRARY_SEARCH_APPLICATION_DIR = 0x00000200,
            LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000,
            LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR = 0x00000100,
            LOAD_LIBRARY_SEARCH_SYSTEM32 = 0x00000800,
            LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400,
            LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
        }


        enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }
    }
}