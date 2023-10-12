namespace WindowsImageComparator
{
    public static class Log
    {
        public static void Info(string message)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"[INFO] {message}");
            Console.ResetColor();
        }

        public static void Error(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {message}");
            Console.ResetColor();
        }

        public static void Success(string message)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[SUCCESS] {message}");
            Console.ResetColor();
        }

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] {message}");
            Console.ResetColor();
        }
    }
}
