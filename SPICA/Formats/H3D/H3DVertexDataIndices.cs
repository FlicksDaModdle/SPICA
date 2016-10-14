﻿using SPICA.Serialization;
using SPICA.Serialization.Serializer;

using System;
using System.IO;

namespace SPICA.Formats.H3D
{
    class H3DVertexDataIndices : ICustomSerialization
    {
        public byte Type;
        public byte DrawMode;
        public ushort Count;

        public int MaxIndex
        {
            get
            {
                int Max = 0;

                foreach (ushort Index in Indices)
                {
                    if (Index > Max) Max = Index;
                }

                return Max;
            }
        }

        [NonSerialized]
        public ushort[] Indices;

        public void Deserialize(BinaryDeserializer Deserializer)
        {
            bool Format = (Type & 1) != 0;
            uint Address = Deserializer.Reader.ReadUInt32();
            long Position = Deserializer.BaseStream.Position;

            Indices = new ushort[Count];

            Deserializer.BaseStream.Seek(Address, SeekOrigin.Begin);

            for (int Index = 0; Index < Count; Index++)
            {
                if (Format)
                {
                    Indices[Index] = Deserializer.Reader.ReadUInt16();
                }
                else
                {
                    Indices[Index] = Deserializer.Reader.ReadByte();
                }
            }

            Deserializer.BaseStream.Seek(Position, SeekOrigin.Begin);
        }

        public bool Serialize(BinarySerializer Serializer)
        {
            Serializer.Writer.Write(Type);
            Serializer.Writer.Write(DrawMode);
            Serializer.Writer.Write((ushort)Indices.Length);

            H3DRelocationType RelocType = H3DRelocationType.RawDataIndex16;

            object Data;

            if (MaxIndex <= byte.MaxValue)
            {
                RelocType = H3DRelocationType.RawDataIndex8;

                byte[] Buffer = new byte[Indices.Length];

                for (int Index = 0; Index < Indices.Length; Index++)
                {
                    Buffer[Index] = (byte)Indices[Index];
                }

                Data = Buffer;
            }
            else
            {
                Data = Indices;
            }

            long Position = Serializer.BaseStream.Position;

            Serializer.RawDataVtx.Values.Add(new RefValue
            {
                Position = Position,
                Value = Data
            });

            Serializer.Skip(4);

            Serializer.Relocator.RelocTypes.Add(Position, RelocType);

            return true;
        }
    }
}