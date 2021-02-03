using System;
using Unity.Kinematica.Editor;


    [Serializable]
    [Tag("Direction", "#5048d2")]
    public struct DirectionTag : Payload<Direction>
    {
        public Direction.Type type;

        public Direction Build(PayloadBuilder builder)
        {
            return Direction.Create(type);
        }
    }

