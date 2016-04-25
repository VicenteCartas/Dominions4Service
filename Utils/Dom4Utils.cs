using System;
using System.Linq;

namespace Utils
{
    using System.IO;

    public static class Dom4Utils
    {
        public static string GetGameName(string fullFilePath)
        {
            return Path.GetFileName(Path.GetDirectoryName(fullFilePath));
        }

        public static string GetPlayerNation(string fullFilePath)
        {
            return Path.GetFileNameWithoutExtension(fullFilePath);
        }

        public static bool IsPlayerTurn(string localSavePath, string fullTurnFilePath)
        {
            string gameName = GetGameName(fullTurnFilePath);
            string turnNation = GetPlayerNation(fullTurnFilePath);

            string gamePath = Path.Combine(localSavePath, gameName);
            if (Directory.Exists(gamePath)) // This is a game we are playing
            {
                if (Directory.GetFiles(gamePath, "*.2h")
                    .Select(f => Path.GetFileNameWithoutExtension(f))
                    .Any(f => f.Equals(turnNation, StringComparison.InvariantCultureIgnoreCase)))
                {
                    // There is a 2h file with the same name as the trn file, it's our turn
                    return true;
                }
            }

            return false;
        }
    }
}