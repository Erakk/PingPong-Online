using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ServerData;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;

namespace Server
{
    public partial class ServerForm : Form
    {

		// halutut muuttuja käyttöliittymään. 
        public bool serverOn = false;
        ServerNetworkings networking = new ServerNetworkings();
		
		// ajaa nämä tiedot serverin käynnistyessä
        public ServerForm()
        {
			// luo halutut komponentit käyttöliittymään ja sytää oikean ip ja portin valmiiksi
            InitializeComponent();
            IP_tb.Text = Packet.GetIP4Address();
            port_tb.Text = "224";

        }
		
		// serverin luonti painike
        private void createServer_btn_Click(object sender, EventArgs e)
        {
			// haluaa tietyt tiedot ennen serverin käynnistymistä
            if (IP_tb.Text != "" && port_tb.Text != "" && serverOn == false)
            {
				// kirjoittaa ilmoituksen, että serveri käynnistyy, estää uudestaan käynnistämisen ja lähettää tiedon yhteyteyksiä hoitavalle classille
                messages_tb.Invoke(new MethodInvoker(delegate () { messages_tb.AppendText("Starting server on: " + IP_tb.Text + "\n"); }));
                serverOn = true;
                networking.Main(IP_tb.Text, port_tb.Text, this);
            }
        }


    }
		
	// hoitaa yhteyksiä käyttäjien kanssa
    class ServerNetworkings
    {
		// halutut muuttujat käyttäjiin
		// käytettävä socket, kättäjä lista, käyttöliittymä class
        static Socket listenerSocket;
        static List<ClientData> _clients;
        public ServerForm sForm;

		
		// halutaan käynnistää tämän classin käynnistyessä
        public void Main(string ip, string port, ServerForm form)
        {
			// tekee perus säädöt classille
            sForm = form;
            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
              _clients = new List<ClientData>();

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));
            listenerSocket.Bind(ipe);

			// aloittaa uuden threadin halutuilla säädöillä
            Thread listenThread = new Thread(ListenThread);
            listenThread.Start();
        }
 
		// luodaan loop kuuntelemaan mikäli uusia käyttäjiä tulee ja luo käyttäjälle uuden classin
        static void ListenThread()
        {
            for (;;)
            {
                listenerSocket.Listen(0);
                _clients.Add(new ClientData(listenerSocket.Accept()));
            }

        }
		
		// ottaa serverille tulevaa dataa vastaan
        public static void Data_IN(object cSocket)
        {
            Socket clientSocket = (Socket)cSocket;
            //           ServerForm serverForm = new ServerForm();
            byte[] Buffer;
            int readBytes;

            for (;;)
            {
				// yrittää ottaa dataa vastaan jatkuvasti ja datan tultua lähettää sen tulkittavaksi
                try
                {
                    Buffer = new byte[clientSocket.SendBufferSize];
                    readBytes = clientSocket.Receive(Buffer);

                    if (readBytes > 0)
                    {
                        // handle data
                        Packet packet = new Packet(Buffer);
                        DataManager(packet);
                    }
                }
				// ilmoittaa, mikäli virhe tapahtuu
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Source);
                }

            }
        }

		// tulkitsee saapuvan datan
        public static void DataManager(Packet p)
        {
            ServerNetworkings sNetworking = new ServerNetworkings();

			// katsoo minkä tyylistä dataa on tulossa ja tekee toimenpiteitä, mikäli tarvitsee
            switch (p.packetType)
            {
                // 
                case PacketType.Registeration:
 //                   sNetworking.sForm.WriteMessage("Client connected!");
                    break;
				// lähettää kaikille käyttäjille peliin liittyvän aika teidon (kesken)
                case PacketType.Timer:
                    foreach (ClientData c in _clients)
                    {
                        c.clientSocket.Send(p.ToBytes());
                    }
                    break;
				// lähettää kaikille käyttäjilel viestit
                case PacketType.Chat:
                    foreach (ClientData c in _clients)
                    {
                        c.clientSocket.Send(p.ToBytes());
                    }
                    break;

                default:
                    break;
            }
        }

    }
	// tekee säädöt uudelle käyttäjälle
    class ClientData
    {
        public Socket clientSocket;
        public Thread clientThread;
        public string id;
        public string firstClient = "yes";


		
        public ClientData()
        {
			// luo uuden "unique" tunnuksen käyyäjälle
            id = Guid.NewGuid().ToString();
			// aloittaa uuden threadin ottamaan dataa vastaan
            clientThread = new Thread(ServerNetworkings.Data_IN);
            clientThread.Start(clientSocket);
            SendRegisterationPacket();

        }
        public ClientData(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            id = Guid.NewGuid().ToString();
            clientThread = new Thread(ServerNetworkings.Data_IN);
            clientThread.Start(clientSocket);
            SendRegisterationPacket();
        }
		// lähettää tiedon, että client pääsi serverille
        public void SendRegisterationPacket()
        {

            Packet p = new Packet(PacketType.Registeration, "server");
            p.Gdata.Add(id);
            p.Gdata.Add(firstClient);
            clientSocket.Send(p.ToBytes());
            firstClient = "no";
        }

    }
}
