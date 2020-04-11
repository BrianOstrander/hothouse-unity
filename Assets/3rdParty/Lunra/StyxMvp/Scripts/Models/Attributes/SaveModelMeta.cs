using System;

namespace Lunra.StyxMvp.Models
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SaveModelMeta : Attribute
    {
        public readonly string Path;
        public readonly int MinimumSupportedVersion;
        public readonly bool CanSave;
        
        public SaveModelMeta(
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