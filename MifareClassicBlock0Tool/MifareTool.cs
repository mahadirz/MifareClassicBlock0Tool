using PCSC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MifareClassicBlock0Tool
{
    class MifareTool
    {
        string[] szReaders;
        SCardContext hContext;
        SCardReader reader;
        IntPtr pioSendPci;

        public String[] GetReaders()
        {
            return szReaders;
        }

        public void OnbuzzerLED()
        {
            //0xFF,0x00,0x40,0x82,0x04,0x01,0x01,0x01,0x03
            sendCommand(reader, pioSendPci, new byte[] { 0xFF, 0x00, 0x40, 0x82, 0x04, 0x01, 0x01, 0x01, 0x03 });
        }

        public void Connect(int position)
        {
            if (hContext.CheckValidity() != PCSC.SCardError.Success)
                init();
            // Create a reader object using the existing context
            reader = new SCardReader(hContext);
            // Connect to the card
            SCardError err = reader.Connect(szReaders[position],
                SCardShareMode.Shared,
                SCardProtocol.T0 | SCardProtocol.T1);
            CheckErr(err);

            switch (reader.ActiveProtocol)
            {
                case SCardProtocol.T0:
                    pioSendPci = SCardPCI.T0;
                    break;
                case SCardProtocol.T1:
                    pioSendPci = SCardPCI.T1;
                    break;
                default:
                    throw new PCSCException(SCardError.ProtocolMismatch,
                        "Protocol not supported: "
                        + reader.ActiveProtocol.ToString());
            }

        }

        public void Disconnect()
        {
            hContext.Release();
        }

        protected void init()
        {
            // Establish SCard context
            hContext = new SCardContext();
            hContext.Establish(SCardScope.System);

            // Retrieve the list of Smartcard readers
            szReaders = hContext.GetReaders();
            if (szReaders.Length <= 0)
                throw new PCSCException(SCardError.NoReadersAvailable,
                    "Could not find any Smartcard reader.");
        }

        public MifareTool()
        {
            init();
        }

        static void CheckErr(SCardError err)
        {
            if (err != SCardError.Success)
                throw new PCSCException(err,
                    SCardHelper.StringifyError(err));
        }

        public static string ByteArrayToString(byte[] ba, bool addSpace = true)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
            {
                if (addSpace)
                    hex.AppendFormat("{0:x2} ", b);
                else
                    hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        public byte[] GetUid()
        {
            // Get UID
            byte[] apdu = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
            return trim(sendCommand(reader, pioSendPci, apdu)).ToArray();
        }

        public bool WriteBlock(String data)
        {
            if (data.Length < 32)
            {
                throw new Exception("Invalid length");
            }
            byte[] apdu, response;
            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x08, 0xD4, 0x08, 0x63, 0x02, 0x00, 0x63, 0x03, 0x00 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x06, 0xD4, 0x42, 0x50, 0x00, 0x57, 0xCD };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x05, 0xD4, 0x08, 0x63, 0x3D, 0x07 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x03, 0xD4, 0x42, 0x40 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x05, 0xD4, 0x08, 0x63, 0x3D, 0x00 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x03, 0xD4, 0x42, 0x43 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x08, 0xD4, 0x08, 0x63, 0x02, 0x80, 0x63, 0x03, 0x80 };
            response = sendCommand(reader, pioSendPci, apdu);

            List<byte> cmdList = new List<byte>() { 0xFF, 0x00, 0x00, 0x00, 0x15, 0xD4, 0x40, 0x01, 0xA0, 0x00 };
            List<byte> block0 = StringToByteList(data);
            //final check of the size
            if (block0.Count < 16)
                throw new Exception("Conversion to 32 bytes of block data failed");
            cmdList.AddRange(block0);
            response = sendCommand(reader, pioSendPci, cmdList.ToArray());
            if(ByteArrayCompare(new byte[] { 0xD5, 0x41, 0x00, 0x90, 0x00 }, response))
            {
                return true;
            }
            return false;
        }

        public static List<byte> StringToByteList(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToList();
        }

        public byte[] ReadBlock0()
        {
            byte[] apdu,response;
            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x08, 0xD4, 0x08, 0x63, 0x02, 0x00, 0x63, 0x03, 0x00 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x06, 0xD4, 0x42, 0x50, 0x00, 0x57, 0xCD };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x05, 0xD4, 0x08, 0x63, 0x3D, 0x07 };
            response = sendCommand(reader, pioSendPci, apdu);

            //from the response of this APDU we can already know whether possible or
            //to retrieve block 0 without key
            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x03, 0xD4, 0x42, 0x40 };
            response = sendCommand(reader, pioSendPci, apdu);
            if (ByteArrayCompare(new byte[] { 0xD5, 0x43, 0x01, 0x90, 0x00 }, response))
            {
                //throw error
                throw new PCSCException(SCardError.CardUnsupported,
                    "This card might be not a block 0 writable.");
            }

                apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x05, 0xD4, 0x08, 0x63, 0x3D, 0x00 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x03, 0xD4, 0x42, 0x43 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x08, 0xD4, 0x08, 0x63, 0x02, 0x80, 0x63, 0x03, 0x80 };
            response = sendCommand(reader, pioSendPci, apdu);

            apdu = new byte[] { 0xFF, 0x00, 0x00, 0x00, 0x05, 0xD4, 0x40, 0x01, 0x30, 0x00 };
            response = sendCommand(reader, pioSendPci, apdu);
            if (response.Length < 21)
            {
                throw new PCSCException(SCardError.BadSeek,
                    "Error retrieving block 0 data.");
            }
            List<byte> resp = trim(response);
            resp.RemoveRange(0,3);
            return resp.ToArray();
        }

        static List<byte> trim(byte[] data)
        {
            List<byte> resp = new List<byte>(data);
            byte sl = 0x90;
            byte l = 0x00;
            if(resp[resp.Count-2]==sl && resp[resp.Count - 1]==l)
                resp.RemoveRange(resp.Count - 2, 2);
            return resp;
        }

        public static bool ByteArrayCompare(byte[] a1, byte[] a2)
        {
            return StructuralComparisons.StructuralEqualityComparer.Equals(a1, a2);
        }

        public static byte[] sendCommand(SCardReader reader, IntPtr pioSendPci, byte[] apdu)
        {
            SCardError err;
            byte[] pbRecvBuffer = new byte[256];
            err = reader.Transmit(pioSendPci, apdu, ref pbRecvBuffer);
            CheckErr(err);
            return pbRecvBuffer;
        }


        


    }
}
