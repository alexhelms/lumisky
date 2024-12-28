using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LumiSky.Core.Memory;

public sealed unsafe class NativeMemoryAllocator<T> : MemoryManager<T>, IDisposable
{
    private bool _disposed;
    private void* _ptr;
    private readonly int _length;
    private readonly bool _aligned;

    private unsafe NativeMemoryAllocator(void* ptr, int length, bool aligned = true)
    {
        _ptr = ptr;
        _length = length;
        _aligned = aligned;
    }

    /// <summary>
    /// Allocate 32-byte aligned native memory equal to <paramref name="length"/> * sizeof <typeparamref name="T"/> bytes.
    /// </summary>
    /// <param name="length"></param>
    /// <returns>A ref counted memory owner.</returns>
    public static IMemoryOwner<T> Allocate(int length)
    {
#if DEBUG
        if ((ulong)length * (ulong)Unsafe.SizeOf<T>() > nuint.MaxValue)
            throw new InvalidOperationException();
#endif
        nuint byteCount = (nuint)(length * Unsafe.SizeOf<T>());
        void* ptr = NativeMemory.AlignedAlloc(byteCount, Simd.AlignmentSize);
        NativeMemory.Clear(ptr, byteCount);
        return new NativeMemoryAllocator<T>(ptr, length);
    }

    internal static IMemoryOwner<T> AllocateUnaligned(int length)
    {
#if DEBUG
        if ((ulong)length * (ulong)Unsafe.SizeOf<T>() > nuint.MaxValue)
            throw new InvalidOperationException();
#endif
        nuint byteCount = (nuint)(length * Unsafe.SizeOf<T>());
        void* ptr = NativeMemory.Alloc(byteCount);
        NativeMemory.Clear(ptr, byteCount);
        return new NativeMemoryAllocator<T>(ptr, length, aligned: false);
    }

    public static IMemoryOwner<TTo> Cast<TFrom, TTo>(IMemoryOwner<TFrom> owner)
        where TFrom : unmanaged
        where TTo : unmanaged
    {
        if (owner is not NativeMemoryAllocator<TFrom> nativeMemory)
            throw new NotSupportedException($"{nameof(owner)} must be of type {nameof(NativeMemoryAllocator<TFrom>)}");

        Span<TTo> spanTTo = MemoryMarshal.Cast<TFrom, TTo>(owner.Memory.Span);
        void* ptr = Unsafe.AsPointer(ref spanTTo.GetReference());
        return new NativeMemoryAllocator<TTo>(ptr, spanTTo.Length, nativeMemory.IsAligned);
    }

    public bool IsDisposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _disposed;
    }

    /// <summary>
    /// Get the number of allocated elements.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length;
    }

    /// <summary>
    /// True if the buffer is SIMD aligned.
    /// </summary>
    public bool IsAligned
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _aligned;
    }

    internal void* Pointer
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _ptr;
    }

    public override Span<T> GetSpan()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(NativeMemoryAllocator<T>));
        return new Span<T>(_ptr, _length);
    }

    public override MemoryHandle Pin(int elementIndex = 0)
    {
        if ((uint)elementIndex > _length)
            throw new ArgumentOutOfRangeException(nameof(elementIndex));

        void* pointer = Unsafe.Add<T>(_ptr, elementIndex);
        return new MemoryHandle(pointer);
    }

    public override void Unpin()
    {
        // nothing to do
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        // typically this would call into a native method appropriate for the platform
        if (_aligned)
            NativeMemory.AlignedFree(_ptr);
        else
            NativeMemory.Free(_ptr);
        _ptr = null;

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
