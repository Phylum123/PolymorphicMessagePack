﻿using MessagePack;

namespace PolymorphicMessagePack
{


    public class PolymorphicMessagePackSerializerOptions : MessagePackSerializerOptions
    {

        internal readonly PolymorphicMessagePackSettings PolymorphicSettings;
        internal readonly PolymorphicResolver PolymorphicResolver;

        public PolymorphicMessagePackSerializerOptions(PolymorphicMessagePackSettings polymorphicSettings)
            : base(new PolymorphicResolver(polymorphicSettings))
        {
            PolymorphicSettings = polymorphicSettings;
            PolymorphicResolver = Resolver as PolymorphicResolver;
        }

        protected PolymorphicMessagePackSerializerOptions(PolymorphicMessagePackSerializerOptions copyFrom)
            : base(copyFrom)
        {
            PolymorphicSettings = copyFrom.PolymorphicSettings;
        }

        protected override MessagePackSerializerOptions Clone()
        {
            return new PolymorphicMessagePackSerializerOptions(this);
        }
    }
}
