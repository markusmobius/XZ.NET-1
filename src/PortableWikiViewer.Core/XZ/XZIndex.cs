﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PortableWikiViewer.Core.XZ
{
    public class XZIndex
    {
        private readonly BinaryReader _reader;
        public long StreamStartPosition { get; private set; }
        public ulong NumberOfRecords { get; private set; }
        public List<XZIndexRecord> Records { get; } = new List<XZIndexRecord>();

        private bool _indexMarkerAlreadyVerified;

        public XZIndex(BinaryReader reader, bool indexMarkerAlreadyVerified)
        {
            _reader = reader;
            _indexMarkerAlreadyVerified = indexMarkerAlreadyVerified;
            StreamStartPosition = reader.BaseStream.Position;
        }

        public static XZIndex FromStream(Stream stream, bool indexMarkerAlreadyVerified)
        {
            var index = new XZIndex(new BinaryReader(stream, Encoding.UTF8, true), indexMarkerAlreadyVerified);
            index.Process();
            return index;
        }

        public void Process()
        {
            if (!_indexMarkerAlreadyVerified)
                VerifyIndexMarker();
            NumberOfRecords = _reader.ReadXZInteger();
            for (ulong i = 0; i < NumberOfRecords; i++)
            {
                Records.Add(XZIndexRecord.FromBinaryReader(_reader));
            }
            SkipPadding();
            VerifyCrc32();
        }

        private void VerifyIndexMarker()
        {
            byte marker = _reader.ReadByte();
            if (marker != 0)
                throw new InvalidDataException("Not an index block");
        }

        private void SkipPadding()
        {
            int padding = (int)(_reader.BaseStream.Position - StreamStartPosition) % 4;
            if (padding > 0)
            {
                byte[] paddingBytes = _reader.ReadBytes(padding);
                if (paddingBytes.Any(b => b != 0))
                    throw new InvalidDataException("Padding bytes were non-null");
            }
        }

        private void VerifyCrc32()
        {
            uint crc = _reader.ReadLittleEndianUInt32();
            // TODO verify this matches
        }
    }
}
