namespace PsionicCorePowerParser
{
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => "",
                _ => input[0].ToString().ToUpper() + input[1..]
            };
    }
}