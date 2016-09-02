using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;

namespace ServerData
{
	// packet kirjasto pitämässä yhteisen "kielen" serverin ja clientin datan välitykseen
	
    [Serializable]
    public class Packet
    {
		// tiedot joita halutaan mahdollisesti lähettää
        public List<String> Gdata;
        public int packetInt;
        public bool packetBool;
        public bool isFirst;
        public string senderID;
        public PacketType packetType;

		
        public Packet(PacketType type, string senderID)
        {
            Gdata = new List<String>();
            this.senderID = senderID;
            this.packetType = type;
        }

		// avaa paketin "suojauksen"
        public Packet(byte[] packetBytes)
        {
			
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(packetBytes);

            Packet p = (Packet)bf.Deserialize(ms);
            ms.Close();
            this.Gdata = p.Gdata;
            this.packetInt = p.packetInt;
            this.packetBool = p.packetBool;
            this.senderID = p.senderID;
            this.packetType = p.packetType;
            this.isFirst = p.isFirst;


        }
		
		// muuttaa datan suojattuun/lähetettävään muotoon
        public byte[] ToBytes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);
            byte[] bytes = ms.ToArray();
            ms.Close();
            return bytes;

        }
		
		// hakee serverille IP osoitteen automaattisesti
        public static string GetIP4Address()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress i in ips)
            {
                if (i.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return i.ToString();
                }
            }

            return "127.0.0.1";
        }
    }

	
	// paketti tyypit
    public enum PacketType
    {
        Registeration,
        Chat,
        Movement,
        Timer,
    }
}
