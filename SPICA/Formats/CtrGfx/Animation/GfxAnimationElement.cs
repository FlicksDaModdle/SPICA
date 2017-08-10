﻿using SPICA.Formats.Common;
using SPICA.Serialization.Attributes;

using System;

namespace SPICA.Formats.CtrGfx.Animation
{
    public class GfxAnimationElement : INamed
    {
        private uint Flags;

        private string _Name;

        public string Name
        {
            get => _Name;
            set => _Name = value ?? throw Exceptions.GetNullException("Name");
        }

        public GfxPrimitiveType PrimitiveType;
        
        [Inline]
        [TypeChoiceName("PrimitiveType")]
        [TypeChoice((uint)GfxPrimitiveType.Float,         typeof(GfxAnimFloat))]
        [TypeChoice((uint)GfxPrimitiveType.Vector2D,      typeof(GfxAnimVector2D))]
        [TypeChoice((uint)GfxPrimitiveType.Transform,     typeof(GfxAnimTransform))]
        [TypeChoice((uint)GfxPrimitiveType.RGBA,          typeof(GfxAnimRGBA))]
        [TypeChoice((uint)GfxPrimitiveType.QuatTransform, typeof(GfxAnimQuatTransform))]
        [TypeChoice((uint)GfxPrimitiveType.Boolean,       typeof(GfxAnimBoolean))]
        [TypeChoice((uint)GfxPrimitiveType.MtxTransform,  typeof(GfxAnimMtxTransform))]
        private object _Content;

        public object Content
        {
            get => _Content;
            set
            {
                Type ValueType = value.GetType();

                if (ValueType != typeof(GfxAnimFloat)         &&
                    ValueType != typeof(GfxAnimVector2D)      &&
                    ValueType != typeof(GfxAnimTransform)     &&
                    ValueType != typeof(GfxAnimRGBA)          &&
                    ValueType != typeof(GfxAnimQuatTransform) &&
                    ValueType != typeof(GfxAnimBoolean)       &&
                    ValueType != typeof(GfxAnimMtxTransform))
                {
                    throw Exceptions.GetTypeException("Content", ValueType.ToString());
                }

                _Content = value ?? throw Exceptions.GetNullException("Content");
            }
        }
    }
}