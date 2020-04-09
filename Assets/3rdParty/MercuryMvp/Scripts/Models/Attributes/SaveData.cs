using System;

namespace LunraGames.SubLight.Models
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SaveData : Attribute
    {
        public readonly string Path;
        public readonly int MinimumSupportedVersion;
        public readonly bool CanSave;
        
        public SaveData(
            string path,
            int minimumSupportedVersion = -1,
            bool canSave = true
        )
        {
            Path = path;
            MinimumSupportedVersion = minimumSupportedVersion;
            CanSave = canSave;
        }
    }
}