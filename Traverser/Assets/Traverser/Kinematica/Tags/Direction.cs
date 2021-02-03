using Unity.Kinematica;


    [Trait]
    public struct Direction
    {
        public enum Type
        {
            Up,
            Down,
            Left,
            Right,
            UpRight,
            DownRight,
            UpLeft,
            DownLeft,
            CornerRight,
            CornerLeft
        }

        public Type type;

        public bool IsType(Type type)
        {
            return this.type == type;
        }

        public static Direction Create(Type type)
        {
            return new Direction
            {
                type = type
            };
        }
    }

