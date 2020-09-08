using System;

namespace Unity.Kinematica.Editor
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class FieldDrawerAttribute : Attribute
    {
        public Type fieldType;

        public FieldDrawerAttribute(Type fieldType)
        {
            this.fieldType = fieldType;
        }
    }
}
