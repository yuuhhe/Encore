﻿using System.Diagnostics.Contracts;

namespace Trinity.Encore.Game.IO.Formats.Databases.DBC
{
    [ContractVerification(false)]
    public sealed class AnimReplacementRecord : IClientDbRecord
    {
        public int Id { get; set; }

        public int SrcAnimId { get; set; }

        public int DstAnimId { get; set; }

        public int ParentAnimReplacementSetId { get; set; }
    }
}
