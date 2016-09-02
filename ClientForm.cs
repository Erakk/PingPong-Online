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
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace Client
{
    public partial class ClientForm : Form
    {
		// muuttujat koskien itse peliä. Lähinnä pallon nopeus ja merkinnät pelaajien paikoista
        public int speedLeft = 0;
        public int speedRight = 0;
        public bool isFirst;
        public int totalPlayers;

		// muuttujat merkitsemään käyttäjän chattiin. Name on chatin nimi. ID on serverin käyttämä merkintä käyttäjän tunnistamiseen
//        public static string name;
//        public static string id;
		
		// uusi classi hoitamaan liikenteen serverille
        ClientNetworking cNetworking = new ClientNetworking();

		// käynnistyy clientin käynnistyessä. 
        public ClientForm()
        {
            InitializeComponent();
            timer1.Enabled = true;
                
        }

		// clientin käyttöliittymän "connect" painike. vaatii joitakin arvoja ip-addressini ja porttiin ennen liittymistä
        private void connect_btn_Click(object sender, EventArgs e)
        {
            if (port_tb.Text != "" && IP_tb.Text != "")
            {
				// käynnistää serveriliikenteen classin halutuilla arvoilla
                cNetworking.Main(nickName_tb.Text, IP_tb.Text, port_tb.Text, this);
            }
        }

		// käyttöliittymän viestinlähetys nappi
        private void sendMessage_btn_Click(object sender, EventArgs e)
        {
			// lähettää tiedon networkingille
            cNetworking.SendMessage(message_tb.Text);
			// nollaa viestin textboxin
            message_tb.Text = "";
        }

		// ottaa vastaan tietoa networking calssilta ja kirjoittaa ne chattiin
        public void WriteMessage(string input)
        {
            Console.WriteLine(input);
//            chat_tb.Text = input;
            chat_tb.Invoke(new MethodInvoker(delegate () { chat_tb.AppendText(input + "\n"); }));

        }
		
		// itse peliin ajastin
		// TODO kesken
        public void TimerTick(ClientForm cform)
        {
            Console.WriteLine("test");

            if (isFirst)
            {

 //               cform.racket_one.Top = Cursor.Position.Y - (cform.racket_one.Height / 2);
            }
            else
            {
                this.racket_two.Top = Cursor.Position.Y - (this.racket_two.Height / 2);
            }
        }
		// ajastin lähettää serverille ajastin tiedon, joka välitetään kaikille käyttäjille
		// TODO kesken
        private void GameTimer(object sender, EventArgs e)
        {

            if (isFirst)
            {
                cNetworking.Timer();
            }           
        }
        public void IsFirstCheck()
        {
            isFirst = true;
        }
    }
	
	// classi joka pitää yhteyttä serveriin
    class ClientNetworking
    {
		// classin muuttujat
		
		// tekee uuden socketin ja clientin tunnistamiseen tarvittavat muutujat
        public static Socket master;
        public static string name;
        public static string id;
        public static bool isFirst; 
        public ClientForm cForm;

		// halutaan käynnistää alkuun tätä classia käynnistäessä
        public void Main(string nickName, string ip, string port, ClientForm form)
        {
			// merkkaa käyttöliittymän polun muuttujaan
            cForm = form;
			
			// socketin säädöt
            master = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), Int32.Parse(port));
            name = nickName;
			
			// yritetään liittyä halutulle serverille
            try
            {
                master.Connect(ipe);
                cForm.WriteMessage("Connected to the server!");

				// käynnistetään uusi threadi ottamaan dataa vastaan
                Thread t = new Thread(() => { Data_IN(cForm); });
                t.Start();
				
				// lähetetään paketti serverille rekisteröimistä varten
                Packet p = new Packet(PacketType.Registeration, name);
                master.Send(p.ToBytes());
            }
			
			// tehdään, mikäli yhteys serveriin ei jostain syystä onnistunut
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                cForm.WriteMessage("Could not connect to host!");


            }
        }
		
		// ottaa dataa vastaan serveriltä
        static void Data_IN(ClientForm form)
        {
            ClientForm cForm = form;

            byte[] Buffer;
            int readBytes;

			// threadi pitää looppia päällä kokoajan, jotta kaikki data saadaan vastaan. 
            for (;;)
            {
				// yrittää ottaa dataa vastaa
                try
                {
                    Buffer = new byte[master.SendBufferSize];
                    readBytes = master.Receive(Buffer);

					// mikäli dataa on tullut, lähetetään se käsiteltäväksi
                    if (readBytes > 0)
                    {
                        DataManager(new Packet(Buffer), cForm);
                    }
                }
				
				// mikäli mitään dataa ei tule, serveri on todennäköisesti kaatunut, joten ohjelma suljetaan
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                    cForm.WriteMessage("The server had disconnected!");


                    Environment.Exit(0);
                }
            }
        }
		
		// käsittelee datan
        static void DataManager(Packet p, ClientForm form)
        {
            ClientForm cForm = form;

            ClientNetworking cNetworking = new ClientNetworking();

			// katsoo millaista dataa käsitellään
            switch (p.packetType)
            {
                case PacketType.Registeration:
					
					// tarkista onko kyseessä ensimmäinen käyttäjän
					// (vaikutta lähinnä itse pelin ajastimen hoitamiseen)
                    id = p.Gdata[0];
                    if (p.Gdata[1] == "yes")
                    {
                        isFirst = true;
                        cForm.IsFirstCheck();
                    }
                    else
                    {
                        isFirst = false;
                    }
                    break;
                case PacketType.Chat:
                    
                    string input = p.Gdata[0] + ": " + p.Gdata[1];
                    cForm.WriteMessage(input);
                    break;
                case PacketType.Movement:
                    break;
                case PacketType.Timer:
                    cForm.TimerTick(cForm);
                    break;
                default:
                    break;
            }
        }
		
		// lähettää viestin serverille
        public void SendMessage(string input)
        {
            Packet p = new Packet(PacketType.Chat, id);
            p.Gdata.Add(name);
            p.Gdata.Add(input);
            master.Send(p.ToBytes());
        }
		
		// lähettää aika infon serverille
		// TODO kesken
        public void Timer()
        {
            Packet p = new Packet(PacketType.Timer, id);
            master.Send(p.ToBytes());
        }

    }
}
