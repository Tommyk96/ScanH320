namespace FarmaSerialize.Threading
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;

    public class NamedMutex : WaitHandle
    {
        private bool createdNew;
        private const int ERROR_ALREADY_EXISTS = 0xb7;
        public const int WAIT_TIMEOUT = 0x102;

        public NamedMutex() : this(false, null)
        {
        }

        public NamedMutex(bool initiallyOwned) : this(initiallyOwned, null)
        {
        }

        public NamedMutex(bool initiallyOwned, string name)
        {
            IntPtr ptr = CreateMutex(IntPtr.Zero, initiallyOwned, name);
            if (ptr == IntPtr.Zero)
            {
                throw new ApplicationException("Failure creating mutex: " + Marshal.GetLastWin32Error().ToString("X"));
            }
            if (Marshal.GetLastWin32Error() == 0xb7)
            {
                this.createdNew = false;
            }
            else
            {
                this.createdNew = true;
            }
#pragma warning disable CS0618  
            this.Handle = ptr;
#pragma warning restore CS0618  
        }

        public NamedMutex(bool initiallyOwned, string name, out bool createdNew) : this(initiallyOwned, name)
        {
            createdNew = this.createdNew;
        }

        public override void Close()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("coredll.dll", SetLastError=true)]
        public static extern bool CloseHandle(IntPtr hObject);
        [DllImport("coredll.dll", SetLastError=true)]
        private static extern IntPtr CreateMutex(IntPtr lpMutexAttributes, bool InitialOwner, string MutexName);
        protected override void Dispose(bool explicitDisposing)
        {
#pragma warning disable CS0618
            
            if (this.Handle != WaitHandle.InvalidHandle)
            {
                CloseHandle(this.Handle);
            }
            this.Handle = WaitHandle.InvalidHandle;
#pragma warning restore CS0618  
            base.Dispose(explicitDisposing);
        }

        public static NamedMutex OpenExisting(string name)
        {
            bool flag;
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (name.Length < 1)
            {
                throw new ArgumentException("name is a zero-length string.");
            }
            if (name.Length > 260)
            {
                throw new ArgumentException("name is longer than 260 characters.");
            }
            NamedMutex mutex = new NamedMutex(false, name, out flag);
            if (flag)
            {
                mutex.Dispose(true);
                throw new WaitHandleCannotBeOpenedException();
            }
            return mutex;
        }

        public void ReleaseMutex()
        {
#pragma warning disable CS0618
            if (!ReleaseMutex(this.Handle))
            {
                throw new ApplicationException("The calling thread does not own the mutex.");
            }
#pragma warning restore CS0618
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("coredll.dll", SetLastError=true)]
        private static extern bool ReleaseMutex(IntPtr hMutex);
        [DllImport("coredll.dll", SetLastError=true)]
        public static extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds);
        public override bool WaitOne()
        {
            return this.WaitOne(-1, false);
        }

        public override bool WaitOne(int millisecondsTimeout, bool notApplicableOnCE)
        {
#pragma warning disable CS0618
            return (WaitForSingleObject(this.Handle, millisecondsTimeout) != 0x102);
#pragma warning restore CS0618
        }

        public override bool  WaitOne(TimeSpan aTs, bool notApplicableOnCE)
        { 
 #pragma warning disable CS0618
            return (WaitForSingleObject(this.Handle, (int) aTs.TotalMilliseconds) != 0x102);
#pragma warning restore CS0618
        }
    }
}

