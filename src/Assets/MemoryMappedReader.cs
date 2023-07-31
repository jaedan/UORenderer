using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace UORenderer;

public unsafe class MemoryMappedReader : IDisposable
{
    private MemoryMappedViewAccessor _accessor;
    private MemoryMappedFile _file;

    private byte* _data;
    private ulong _size;
    private long _position;
    private FileStream _input = null;

    public MemoryMappedReader(FileStream input) : this(input, false)
    {
    }

    public MemoryMappedReader(FileStream input, bool leaveOpen)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input");
        }

        if (!input.CanRead)
        {
            throw new ArgumentException("File is not readable");
        }

        _input = input;

        _file = MemoryMappedFile.CreateFromFile
        (
            input,
            null,
            0,
            MemoryMappedFileAccess.Read,
            HandleInheritability.None,
            leaveOpen
        );

        _accessor = _file.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        try
        {
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref _data);
            _size = _accessor.SafeMemoryMappedViewHandle.ByteLength;
            _position = 0;
        }
        catch
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            throw new Exception("Unable to memory map file");
        }
    }

    public void Close()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_accessor != null)
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
            _accessor?.Dispose();
            _accessor = null;
        }
        if (_file != null)
        {
            _file.SafeMemoryMappedFileHandle.Close();
            _file?.Dispose();
            _file = null;
        }
        if (_input != null)
        {
            _input.Close();
            _input = null;
        }
    }

    public long Position { get { return _position; } }

    public void Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                if (offset < 0)
                {
                    throw new ArgumentException("offset");
                }

                if ((ulong)offset > _size)
                {
                    throw new ArgumentException("offset");
                }
                _position = offset;
                break;
            case SeekOrigin.Current:
                if (offset < 0)
                {
                    long o = -1 * offset;
                    if (o > _position)
                    {
                        throw new ArgumentException("offset");
                    }

                    _position -= o;
                }
                else
                {
                    if ((ulong)offset > _size)
                    {
                        throw new ArgumentException("offset");
                    }

                    _position += offset;
                }
                break;
            case SeekOrigin.End:
                if (offset > 0)
                {
                    throw new ArgumentException("offset");
                }

                {
                    long o = -1 * offset;

                    if ((ulong)o > _size)
                    {
                        throw new ArgumentException("offset");
                    }

                    _position = (long)(_size - (ulong)o);
                }
                break;
        }
    }

    #region Simple Types - Copy to stack, advance position

    public T Read<T>() where T : unmanaged
    {
        if ((ulong)(_position + sizeof(T)) > _size)
        {
            throw new EndOfStreamException();
        }

        T v = *(T*)(_data + _position);
        _position += sizeof(T);

        return v;
    }

    public byte ReadByte()
    {
        return Read<byte>();
    }

    public short ReadInt16()
    {
        return Read<short>();
    }

    public int ReadInt32()
    {
        return Read<int>();
    }

    public long ReadInt64()
    {
        return Read<long>();
    }

    public sbyte ReadSByte()
    {
        return Read<sbyte>();
    }

    public ushort ReadUInt16()
    {
        return Read<ushort>();
    }

    public uint ReadUInt32()
    {
        return Read<uint>();
    }

    public ulong ReadUInt64()
    {
        return Read<ulong>();
    }

    #endregion

    #region Direct Views - No copy, advance position

    /// <summary>
    /// This returns a pointer to any unmanaged type without copying it. This is especially
    /// useful for reading structs out of the file.
    /// </summary>
    public T* ReadType<T>() where T : unmanaged
    {
        if ((ulong)(_position + sizeof(T)) > _size)
        {
            throw new EndOfStreamException();
        }

        T* v = (T*)(_data + _position);
        _position += sizeof(T);

        return v;
    }

    /// <summary>
    /// This returns a read only span directly to the memory mapped region.
    /// </summary>
    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        var view = new ReadOnlySpan<byte>(_data + _position, length);
        _position += length;
        return view;
    }

    /// <summary>
    /// Return a stream of the given length starting from the current position.
    /// This does not copy the data, but it does allocate a stream object.
    /// </summary>
    public UnmanagedMemoryStream ReadStream(int length)
    {
        var stream = new UnmanagedMemoryStream(_data + _position, length);
        _position += length;
        return stream;
    }

    #endregion

}
