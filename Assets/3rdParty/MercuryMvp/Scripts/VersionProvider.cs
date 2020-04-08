using System;
using UnityEngine;

namespace LunraGames.SubLight
{
    public static class VersionProvider
    {
        public static int Current => Convert.ToInt32(Application.version);
    }
}