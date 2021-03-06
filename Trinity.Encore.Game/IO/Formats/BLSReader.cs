using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;
using Trinity.Core.IO;

namespace Trinity.Encore.Game.IO.Formats
{
    public sealed class BLSReader : BinaryFileReader
    {
        public BLSReader(string fileName)
            : base(fileName, Encoding.ASCII)
        {
            Contract.Requires(!string.IsNullOrEmpty(fileName));

            Chunks = new List<BLSChunk>();
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(Chunks != null);
        }

        protected override void Read(BinaryReader reader)
        {
            Magic = reader.ReadFourCC(); // TODO: Magic check.
            Version = reader.ReadInt32();

            if (Version < 0)
                throw new InvalidDataException("Negative version encountered.");

            PermutationCount = reader.ReadInt32();

            if (PermutationCount < 0)
                throw new InvalidDataException("Negative permutation count encountered.");

            for (var i = 0; i < PermutationCount; i++)
                Chunks.Add(new BLSChunk(reader));
        }

        public string Magic { get; private set; }

        public int Version { get; private set; }

        public int PermutationCount { get; private set; }

        public List<BLSChunk> Chunks { get; private set; }

        public sealed class BLSChunk
        {
            [ContractInvariantMethod]
            private void Invariant()
            {
                Contract.Invariant(UnknownFlags1 >= 0);
                Contract.Invariant(UnknownFlags2 >= 0);
                Contract.Invariant(Unknown >= 0);
                Contract.Invariant(Size >= 0);
                Contract.Invariant(Data != null);
            }

            public BLSChunk(BinaryReader reader)
            {
                Contract.Requires(reader != null);

                UnknownFlags1 = reader.ReadInt32();
                UnknownFlags2 = reader.ReadInt32();
                Unknown = reader.ReadInt32(); // Always zero?
                Size = reader.ReadInt32();

                if (Size < 0)
                    throw new InvalidDataException("Negative chunk size encountered.");

                Data = reader.ReadBytes(Size);
            }

            public int UnknownFlags1 { get; private set; }

            public int UnknownFlags2 { get; private set; }

            public int Unknown { get; private set; }

            public int Size { get; private set; }

            public byte[] Data { get; private set; }
        }
    }
}
