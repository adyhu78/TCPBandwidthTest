using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Net;
using System.Net.Sockets;

namespace BandwidthTest
{
    class CommHeader
    {
        private int SIGNATURE = 0x01020304;

        public const int TYPE_CLIENT_TO_SERVER = 0;
        public const int TYPE_SERVER_TO_CLIENT = 1;
        public const int TYPE_DUPLEX = 3;

        public int mSignature;
        public int mVersion;
        public int mType;
        public int mSize;

        public CommHeader()
        {
        }

        public CommHeader(int pType, int pSize) 
        {
            mSignature = SIGNATURE;
            mVersion = 1;

            mType = pType;
            mSize = pSize;
        }

        public void WriteToStream(Stream pStream) 
        {
            WriteIntToStream(pStream, mSignature);
            WriteIntToStream(pStream, mVersion);
            WriteIntToStream(pStream, mType);
            WriteIntToStream(pStream, mSize);
        }

        public void ReadFromStream(Stream pStream)
        {
            mSignature = ReadIntFromStream(pStream);

            if (mSignature != SIGNATURE)
                throw new IOException("Invalid header signature");

            mVersion = ReadIntFromStream(pStream);
            if (mVersion != 1)
                throw new IOException("Invalid version");

            mType = ReadIntFromStream(pStream);
            if ((mType != TYPE_CLIENT_TO_SERVER) && (mType != TYPE_SERVER_TO_CLIENT) && (mType != TYPE_DUPLEX))
                throw new IOException("Invalid type");

            mSize = ReadIntFromStream(pStream);
        }

        private int ReadIntFromStream(Stream pStream) 
        {
            byte[] buffer = new byte[sizeof(Int32)];
            pStream.Read(buffer, 0, sizeof(Int32));

            return BitConverter.ToInt32(buffer, 0);
        }

        private void WriteIntToStream(Stream pStream, int pValue)
        {
            byte[] buffer = BitConverter.GetBytes(pValue);
            pStream.Write(buffer, 0, buffer.Length);
        }

    }
}
