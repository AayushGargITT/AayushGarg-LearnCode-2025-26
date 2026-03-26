namespace GeoApp.Utils
{
    public static class InputValidator
    {
        private const int MinAddressLength = 3;
        public static void Validate(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Location cannot be empty");
            }

            if (input.Length < MinAddressLength)
            {
                throw new ArgumentException("Location is too short");
            }

        }
    }
}