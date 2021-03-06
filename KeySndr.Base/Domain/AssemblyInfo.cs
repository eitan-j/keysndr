﻿using System;

namespace KeySndr.Base.Domain
{
    public class AssemblyInfo
    {
        public static Version GetAssemblyVersion(Type a)
        {
            var reference = a.Assembly;
            return reference.GetName().Version;
        }
    }
}
