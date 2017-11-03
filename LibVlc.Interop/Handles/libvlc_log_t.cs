using System;
using System.Runtime.InteropServices;
#if !NETSTANDARD1_3
using System.Security.Permissions;
#endif

namespace LibVlc.Interop
{
    /// <summary>
    /// Contains a handle that is safely released when not used anymore.
    /// </summary>
    /// <remarks>Inspired from code at https://msdn.microsoft.com/en-us/library/microsoft.win32.safehandles.safehandlezeroorminusoneisinvalid(v=vs.80).aspx#code-snippet-4 </remarks>
#if !NETSTANDARD1_3
    [SecurityPermission(SecurityAction.Demand, UnmanagedCode = true)]
#endif
    public sealed class libvlc_log_t : SafeHandle
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="libvlc_log_t"/> class by providing the handle to be stored.
        /// </summary>
        /// <param name="preexistingHandle">The handle that is stored by this instance at initialization.</param>
        internal libvlc_log_t(IntPtr handle)
            : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        /// <summary>
        /// Releases the memory associated to this handle
        /// </summary>
        /// <returns>A value indicating whether the release operation was successful</returns>
        protected override bool ReleaseHandle()
        {
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            // Free the handle.
            //Marshal.FreeHGlobal(handle);

            // Set the handle to zero.
            handle = IntPtr.Zero;

            // Return success.
            return true;
        }

        /// <summary>En cas de substitution dans une classe dérivée, obtient une valeur indiquant si la valeur du handle n'est pas valide.</summary>
        /// <returns>true si la valeur du handle n'est pas valide, sinon false.</returns>
        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
