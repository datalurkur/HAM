using System;
using System.Text;
using System.Collections.Generic;

public interface Packable
{
    void Pack(DataPacker packer);
    void Unpack(DataUnpacker unpacker);
}

public abstract class DataPacker
{
    public void Pack(string s)
    {
        Pack(System.Text.Encoding.Unicode.GetBytes(s));
    }

    public abstract byte[] GetBytes();

    public abstract void Pack(byte b);
    public abstract void Pack(bool b);
    public abstract void Pack(int i);
    public abstract void Pack(float f);
    public abstract void Pack(byte[] b);
}

public abstract class DataUnpacker
{
    public void Unpack(out string s)
    {
        byte[] cData;
        Unpack(out cData);
        char[] cArray = System.Text.Encoding.Unicode.GetChars(cData, 0, cData.Length);
        s = new string(cArray);
    }

    public abstract void Unpack(out byte b);
    public abstract void Unpack(out bool b);
    public abstract void Unpack(out int i);
    public abstract void Unpack(out float f);
    public abstract void Unpack(out byte[] b);
}

public class BinaryDataPacker : DataPacker
{
    private List<byte> buffer;

    public BinaryDataPacker()
    {
        this.buffer = new List<byte>();
    }

    public override byte[] GetBytes()
    {
        return buffer.ToArray();
    }

    public override void Pack(byte b)
    {
        this.buffer.Add(b);
    }
    public override void Pack(bool b)
    {
        this.buffer.AddRange(BitConverter.GetBytes(b));
    }
    public override void Pack(int i)
    {
        this.buffer.AddRange(BitConverter.GetBytes(i));
    }
    public override void Pack(float f)
    {
        this.buffer.AddRange(BitConverter.GetBytes(f));
    }
    public override void Pack(byte[] b)
    {
        this.buffer.AddRange(BitConverter.GetBytes(b.Length));
        if (b.Length > 0)
        {
            this.buffer.AddRange(b);
        }
    }
}

public class BinaryDataUnpacker : DataUnpacker
{
    private byte[] buffer;
    private int offset;

    public BinaryDataUnpacker(byte[] buffer)
    {
        this.buffer = buffer;
        this.offset = 0;
    }

    public override void Unpack(out byte b)
    {
        b = this.buffer[this.offset];
        this.offset += 1;
    }
    public override void Unpack(out bool b)
    {
        b = BitConverter.ToBoolean(this.buffer, this.offset);
        this.offset += 1;
    }
    public override void Unpack(out int i)
    {
        i = BitConverter.ToInt32(this.buffer, this.offset);
        this.offset += 4;
    }
    public override void Unpack(out float f)
    {
        f = BitConverter.ToSingle(this.buffer, this.offset);
        this.offset += 4;
    }
    public override void Unpack(out byte[] b)
    {
        int size;
        Unpack(out size);

        if (size == 0)
        {
            b = new byte[0];
        }
        else
        {
            b = new byte[size];
            Array.Copy(this.buffer, this.offset, b, 0, size);
            this.offset += size;
        }
    }
}