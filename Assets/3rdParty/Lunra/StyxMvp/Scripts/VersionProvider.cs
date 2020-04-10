using System;
using UnityEngine;

namespace Lunra.StyxMvp
{
    public static class VersionProvider
    {
        public static int Current => Convert.ToInt32(Application.version);
    }
}