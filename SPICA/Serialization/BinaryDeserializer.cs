﻿using SPICA.Serialization.Attributes;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SPICA.Serialization
{
    class BinaryDeserializer
    {
        public Stream BaseStream;
        public BinaryReader Reader;

        Dictionary<long, object> ObjPointers;

        private long BufferedPos = 0;
        private uint BufferedUInt = 0;
        private uint BufferedShift = 0;

        public BinaryDeserializer(Stream Stream)
        {
            BaseStream = Stream;
            Reader = new BinaryReader(Stream);

            ObjPointers = new Dictionary<long, object>();
        }

        public T Deserialize<T>()
        {
            return (T)ReadValue(typeof(T));
        }

        private object ReadValue(Type Type, FieldInfo Info = null, int Length = 0)
        {
            if (Type.IsPrimitive || Type.IsEnum)
            {
                switch (Type.GetTypeCode(Type))
                {
                    case TypeCode.UInt64: return Reader.ReadUInt64();
                    case TypeCode.UInt32: return Reader.ReadUInt32();
                    case TypeCode.UInt16: return Reader.ReadUInt16();
                    case TypeCode.Byte: return Reader.ReadByte();
                    case TypeCode.Int64: return Reader.ReadInt64();
                    case TypeCode.Int32: return Reader.ReadInt32();
                    case TypeCode.Int16: return Reader.ReadInt16();
                    case TypeCode.SByte: return Reader.ReadSByte();
                    case TypeCode.Single: return Reader.ReadSingle();
                    case TypeCode.Double: return Reader.ReadDouble();
                    case TypeCode.Boolean:
                        if (BufferedPos != BaseStream.Position || BufferedShift == 0)
                        {
                            BufferedUInt = Reader.ReadUInt32();
                            BufferedPos = BaseStream.Position;
                            BufferedShift = 32;
                        }

                        bool Value = (BufferedUInt & 1) != 0;

                        BufferedUInt >>= 1;
                        BufferedShift--;

                        return Value;

                    default: return null;
                }
            }
            else if (typeof(IList).IsAssignableFrom(Type))
            {
                return ReadList(Type, Info, Length);
            }
            else if (Type == typeof(string))
            {
                return ReadString();
            }
            else
            {
                return ReadObject(Type);
            }
        }

        private IList ReadList(Type Type, FieldInfo Info, int Length)
        {
            IList List;

            bool Range = Info != null && Info.IsDefined(typeof(RangeAttribute));
            bool Pointers = Info != null && Info.IsDefined(typeof(PointersAttribute));

            long Position = BaseStream.Position;

            if (Type.IsArray)
            {
                Type = Type.GetElementType();
                List = Array.CreateInstance(Type, Length);
            }
            else
            {
                List = (IList)Activator.CreateInstance(Type);
                Type = Type.GetGenericArguments()[0];
            }

            int Index;
            for (Index = 0; (Range ? BaseStream.Position : Index) < Length; Index++)
            {
                if (Pointers)
                {
                    BaseStream.Seek(Position + Index * 4, SeekOrigin.Begin);
                    BaseStream.Seek(Reader.ReadUInt32(), SeekOrigin.Begin);
                }

                long Address = BaseStream.Position;
                object Value = GetObj(Type, Address, ReadValue(Type));

                if (List.IsFixedSize)
                {
                    List[Index] = Value;
                }
                else
                {
                    List.Add(Value);
                }
            }

            if (Pointers) BaseStream.Seek(Position + Index * 4, SeekOrigin.Begin);

            return List;
        }

        private string ReadString()
        {
            StringBuilder SB = new StringBuilder();

            char Chr;
            while ((Chr = Reader.ReadChar()) != '\0')
            {
                SB.Append(Chr);
            }

            return SB.ToString();
        }

        private object ReadObject(Type ObjectType)
        {
            object Value = Activator.CreateInstance(ObjectType);

            foreach (FieldInfo Info in Value.GetType().GetFields())
            {
                if (!Info.IsDefined(typeof(NonSerializedAttribute)))
                {
                    Type Type = Info.FieldType;

                    bool Inline;

                    Inline = Info.IsDefined(typeof(InlineAttribute));
                    Inline |= Type.IsDefined(typeof(InlineAttribute));

                    if (Type.IsValueType || Type.IsEnum || Inline)
                    {
                        int Length = 0;

                        if (Info.IsDefined(typeof(FixedLengthAttribute)))
                        {
                            Length = Info.GetCustomAttribute<FixedLengthAttribute>().Length;
                        }

                        Info.SetValue(Value, ReadValue(Type, Info, Length));
                    }
                    else
                    {
                        Info.SetValue(Value, ReadReference(Info));
                    }
                }
            }

            if (Value is ICustomSerialization)
            {
                ((ICustomSerialization)Value).Deserialize(this);
            }

            return Value;
        }

        private object ReadReference(FieldInfo Info)
        {
            uint Address = Reader.ReadUInt32();
            int Length = 0;

            if (Info.IsDefined(typeof(FixedLengthAttribute)))
            {
                Length = Info.GetCustomAttribute<FixedLengthAttribute>().Length;
            }
            else if (typeof(IList).IsAssignableFrom(Info.FieldType))
            {
                Length = Reader.ReadInt32();
            }
            
            object Value = null;

            if (Address != 0)
            {
                long Position = BaseStream.Position;

                BaseStream.Seek(Address, SeekOrigin.Begin);

                Value = ReadValue(Info.FieldType, Info, Length);

                if (Length == 0) Value = GetObj(Info.FieldType, Address, Value);

                BaseStream.Seek(Position, SeekOrigin.Begin);
            }

            return Value;
        }

        private object GetObj(Type Type, long Address, object Value)
        {
            //Note: Several Bool values may share the same Address (since they use only 1 bit)
            //So we can't use the Address as Key reliably for Bools
            if (ObjPointers.ContainsKey(Address))
            {
                Value = ObjPointers[Address];
            }
            else if (Type != typeof(bool))
            {
                ObjPointers.Add(Address, Value);
            }

            return Value;
        }
    }
}
