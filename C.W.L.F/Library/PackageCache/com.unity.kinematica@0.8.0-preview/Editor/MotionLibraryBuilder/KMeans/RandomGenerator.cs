namespace Unity.Kinematica.Editor
{
    internal struct RandomGenerator
    {
        //
        // generate random integer between 0 and max-1
        //

        public int Integer(int max)
        {
            return random.Next(max);
        }

        //
        // between 0 and 1
        //

        public float Float()
        {
            return (float)random.NextDouble();
        }

        public RandomGenerator(int seed = 1234)
        {
            random = new System.Random(seed);
        }

        private System.Random random;
    }
}
