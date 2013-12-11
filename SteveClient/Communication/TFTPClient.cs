
using System;
using System.Net.Sockets;
using System.IO;
using System.Net;
using System.Text;

namespace Communication
{
	/// </summary>
	class TFTPClient
	{
		public TFTPClient()
		{
			m_client = new UdpClient();
		}

		public TFTPClient(int port, string hostname)
		{
			m_client = new UdpClient();
			m_commandPort = port;
			m_hostname = hostname;
		}

		private enum Opcodes
		{
			RRQ = 1,
			WRQ = 2,
			DATA = 3,
			ACK = 4,
			ERROR = 5
		}

		/// <summary>
		/// Gets the file.
		/// </summary>
		/// <param name='hostname'>Hostname.</param>
		/// <param name='tMode'>Transfer mode (octet or netascii).</param>
		/// <param name='fName'>name of file to get</param>
		/// <param name='error'>true to get data with errors</param>
		public void GetFile(string fName)
		{
			if (File.Exists(fName))
			{
				File.Delete(fName);
			}
			FileStream stream = new FileStream (fName, FileMode.Create);

			//Send the request for the file
			SendRequest("octet", fName, true);

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
			while (true)
			{
				//Recieve the response packet
				byte[] echo = m_client.Receive(ref endpoint);

				if (echo[1] == (byte)Opcodes.ERROR)
				{
					HandleError(echo);
				}
				else if (echo[1] == (byte)Opcodes.DATA)
				{
					int port = endpoint.Port;
					int blockNum = echo [3];

					SendAck (port, blockNum, m_hostname);
					stream.Write (echo, 4, echo.Length - 4);

					if (echo.Length < 516) {
						//Console.WriteLine ("Length: " + echo.Length);
						if (echo.Length == 0 && blockNum == 1) {
							File.Delete (fName);
						}
						//Last Packet
						break;
					}
				}
			}
			stream.Close();
		}


		public void PutFile(string fName){
			int packetNr = 0;
			BinaryReader fileStream = new BinaryReader(new FileStream(fName,FileMode.Open,FileAccess.Read,FileShare.ReadWrite));

			//Send the request for the file
			SendRequest("octet", fName, false);

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
			while (true)
			{
				//Recieve the response packet
				byte[] echo = m_client.Receive(ref endpoint);

				int port = endpoint.Port;

				if (echo[1] == (byte)Opcodes.ERROR)
				{
					HandleError(echo);
				}
				else if (echo[1] == (byte)Opcodes.ACK)
				{
					byte[] data = fileStream.ReadBytes(512);

					if ((((Opcodes)echo[1]) == Opcodes.ACK) && (((echo[2] << 8) & 0xff00) | echo[3]) == packetNr) {
						SendDataPacket(++packetNr, data, port);
					}

					// we are done
					if (data.Length < 512) {
						break;
					}
				}
			}
			fileStream.Close();
		}


		/// <summary>
		/// Constructs the Datagram for the request
		/// </summary>
		/// <param name='hostname'>Hostname.</param>
		/// <param name='tMode'>Transfer Mode (octet or netascii)</param>
		/// <param name='fName'> Filename.</param>
		private void SendRequest(string tMode, string fName, bool read)
		{
			//Build Datagram
			byte[] transferMode = Encoding.ASCII.GetBytes(tMode);
			byte[] fileName = Encoding.ASCII.GetBytes(fName);

			//Compute the size to allocate
			int sizeOfRequest = 2 + transferMode.Length + fileName.Length + 2;

			byte[] request = new byte[sizeOfRequest];

			// Set opcode
			request[0] = 0;
			if (read) {
				request [1] = (byte)Opcodes.RRQ;
			} else {
				request[1] = (byte)Opcodes.WRQ;
			}


			//Copy the filename into the request array
			System.Buffer.BlockCopy(fileName, 0, request, 2, fileName.Length);
			//Add separating 0
			request[2 + fileName.Length] = 0;

			//Copy the transfer mode into the request array
			System.Buffer.BlockCopy(transferMode, 0, request, 3 + fileName.Length, transferMode.Length);
			//Add trailing zero
			request[3 + transferMode.Length + fileName.Length] = 0;

			//Send request
			try{
				m_client.Send(request, request.Length, m_hostname, m_commandPort);
			}catch (Exception){
				Console.Error.WriteLine("Unable to send request to server {0} on port {1}", m_hostname, m_commandPort);
				Environment.Exit(0);
			}
		}

		private void SendDataPacket(int blockNr, byte[] data, int port) {
			// Create Byte array to hold ack packet
			byte[] ret = new byte[4 + data.Length];

			// Set first Opcode of packet to TFTP_ACK
			ret[0] = 0;
			ret[1] = (byte)Opcodes.DATA;
			ret[2] = (byte)((blockNr >> 8) & 0xff);
			ret[3] = (byte)(blockNr & 0xff);
			Array.Copy(data, 0, ret, 4, data.Length);
			try{
				m_client.Send(ret, ret.Length, m_hostname, port);
			}catch (Exception){
				Console.Error.WriteLine("Unable to send request to server {0} on port {1}", m_hostname, port);
				Environment.Exit(0);
			}
		}

		/// <summary>
		/// Sends the ack.
		/// </summary>
		/// <param name='port'>Port.</param>
		/// <param name='blockNum'> Block number.</param>
		/// <param name='hostname'>Hostname.</param>
		private void SendAck(int port, int blockNum, string hostname)
		{
			//Construct a 4 byte array
			byte[] ack = new byte[4];

			// Set opcode and block number
			ack[0] = 0;
			ack[1] = (byte)Opcodes.ACK;
			ack[2] = 0;
			ack[3] = (byte)blockNum;

			//Send ack
			try
			{
				m_client.Send(ack, ack.Length, hostname, port);
			}
			catch (Exception)
			{
				Console.Error.WriteLine("Unable to send request to server {0} on port {1}", hostname, port);
				Environment.Exit(0);
			}
		}

		/// <summary>
		/// Handles the error.
		/// </summary>
		/// <param name='errorResponse'>Error response.</param>
		private void HandleError(byte[] errorResponse)
		{
			int errorCode = (int)errorResponse[3];
			string str = Encoding.ASCII.GetString(errorResponse, 4, errorResponse.Length - 5);
			Console.WriteLine(String.Format("TFTPserver: Error Code {0}: {1}", errorCode, str));
			Environment.Exit(0);
		}

		/// <summary>
		/// UdpClient for the TFTPreader
		/// </summary>
		private UdpClient m_client;

		private string m_hostname;
		private int m_commandPort;
	}
}
