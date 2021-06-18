using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

#if WINDOWS_UWP
using Windows.System.Threading;
using System.Threading;
#else
using System.Threading;
#endif

namespace Sereno.Network
{
    /// <summary>
    /// The Connection Status enumeration
    /// </summary>
    public enum ConnectionStatus
	{
		CONNECTED,
		DISCONNECTED,
        SOCKET_RECREATED,
		FAILED_CONNECTION
	}

    /// <summary>
    /// Delegate function called when the status of the connection has changed
    /// </summary>
    /// <param name="s">The socket being used. It can be closed after this call</param>
    /// <param name="status">status the status value of the connection</param>
    public delegate void ClientStatusCallback(Socket s, ConnectionStatus status);

    /// <summary>
    /// Client object. It will connect to a remote Server and launch a Read Thread.
	/// You can Inherit from this class and overwrite the function "HandleMessage"
    /// </summary>
	public class Client
	{
		/******************************/
		/********PRIVATE FIELDS********/
		/******************************/

        /// <summary>
        /// The Client Socket
        /// </summary>
		private Socket m_sock = null;

        /// <summary>
        /// The Remote Server address
        /// </summary>
		private readonly IPAddress m_addr;

        /// <summary>
        /// The Remote Server port
        /// </summary>
		private readonly uint m_port;

        /// <summary>
        /// The Client thread used to handle data received by the Server
        /// </summary>
		private Thread           m_readThread     = null;

		/// <summary>
		/// The Client thread used to handle data sent to the Server
		/// </summary>
		protected Thread           m_writeThread    = null;

        /// <summary>
        /// Is this Client closed ?
        /// </summary>
		private bool             m_closed         = true;

        /// <summary>
        /// The status callback
        /// </summary>
		private List<ClientStatusCallback> m_statusCallbacks = new List<ClientStatusCallback>();

        /// <summary>
        /// The write buffer queue
        /// </summary>
        private Queue<byte[]> m_writeBuffer = new Queue<byte[]>();

        /// <summary>
        /// The current connection status
        /// </summary>
        private ConnectionStatus m_currentStatus = ConnectionStatus.DISCONNECTED;

        /// <summary>
        /// The number of time in micro seconds that the reading and writing threads sleep if no data is to be sent/read or in case of disconnection
        /// </summary>
		private const int THREAD_SLEEP = 50;

        /******************************/
        /*******PUBLIC FUNCTIONS*******/
        /******************************/

        /// <summary>
        /// Client constructor. Initialize the Address but does not yet connect to the address specified
        /// </summary>
        /// <param name="addr">addr the Address to connect</param>
        /// <param name="port">port the port to connect</param>
        /// <exception cref="FormatException">when the address is not in a correct format</exception>
        public Client(string addr, uint port)
		{
			m_addr           = IPAddress.Parse(addr);
			m_port           = port;
		}

        /// <summary>
        /// Client constructor. Initialize the Address but does not yet connect to the address specified
        /// </summary>
        /// <param name="addr">addr the Address to connect</param>
        /// <param name="port">port the port to connect</param>
        public Client(IPAddress addr, uint port)
		{
			m_addr           = addr;
			m_port           = port;
        }

        ~Client()
        {
            Close();
        }

        /// <summary>
        /// Launch the ReadThread and try to connect to the remote server
		/// The thread will reconnect to the server at each disconnection
        /// </summary>

        public void Connect()
		{
			//Launch the communication thread (ReadThread)
			m_readThread = new Thread(new ThreadStart(ReadThread));
			m_writeThread = new Thread(new ThreadStart(WriteThread));
			m_closed = false;
			m_readThread.Start();
			m_writeThread.Start();
		}

        /// <summary>
        /// Close the communication
        /// </summary>
		public void Close()
		{
			if(!m_closed)
			{
				m_closed = true;
                m_readThread.Interrupt();
				m_readThread.Join();
                m_writeThread.Interrupt();
				m_writeThread.Join();
                if(m_sock != null && m_sock.Connected)
                {
                    m_sock.Close();
                    m_sock = null;
                }
			}
        }

        /// <summary>
        /// Is this client connected to the distant server?
        /// </summary>
        /// <returns></returns>
        public bool IsConnected()
        {
            return m_sock != null && m_sock.Connected;
        }

        /// <summary>
        /// Push into a Buffer the message to send to the Server
        /// </summary>
        /// <param name="msg">msg the message to send</param>
        public void Send(byte[] msg)
		{
			lock(m_writeBuffer)
			{
            	m_writeBuffer.Enqueue(msg);
			}
        }

        /// <summary>
        /// Add a new Callback listener
        /// </summary>
        /// <param name="clbk">The new callback to call when status connection changes</param>
        public void AddListener(ClientStatusCallback clbk)
        {
            m_statusCallbacks.Add(clbk);
        }

        /// <summary>
        /// Remove an already registered Callback listener
        /// </summary>
        /// <param name="clbk">The old callback called when status connection changes</param>
        public void RemoveListener(ClientStatusCallback clbk)
        {
            m_statusCallbacks.Remove(clbk);
        }

        /// <summary>
        /// Get the IP Address of this client
        /// </summary>
        /// <returns>The IP Adress if connected, null otherwise</returns>
        public IPAddress GetIPAddress()
        {
            lock(this)
                if(IsConnected())
                    return ((IPEndPoint)m_sock.LocalEndPoint).Address;
            return null;
        }

        /******************************/
        /*****PROTECTED FUNCTIONS******/
        /******************************/

#region
        /// <summary>
        /// Handle the messages received by the Remote Server. Aimed at being overrided
        /// </summary>
        /// <param name="data">the data received</param>
        protected virtual void HandleMessage(byte[] data)
		{
		}
#endregion

        /******************************/
        /******PRIVATE FUNCTIONS*******/
        /******************************/

#region

        /// <summary>
        /// Fired the status via the ConnectStatus callback
        /// </summary>
        /// <param name="status">status the status to fire</param>
        private void FiredStatus(ConnectionStatus status)
		{
			lock(this)
                if(status != m_currentStatus)
                {
                    m_currentStatus = status;
				    foreach(var clbk in m_statusCallbacks)
					    clbk(m_sock, status);
                }
        }

        /// <summary>
        /// The "Read" Thread. This thread will loop until the Close function has been called.
		/// It will read every information sent by the Server and pass the data to HandleMessage.
        /// 
        /// 
        /// If a disconnection has been detected, the ClientStatusClbk will fire.
		/// This thread will try to reconnect to the server if the client is not yet connected.
        /// </summary>
        private void ReadThread()
		{
			//The closed command will be sent by "Close()" function
			while(!m_closed) 
			{
                //If the socket is not created, recreate it and try to reconnect
                bool askClose = false;
                if(m_sock == null)
                {
                    lock (this)
                    {
                        try
                        {
                            lock(m_writeBuffer)
                                m_writeBuffer.Clear();
                            m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            m_sock.NoDelay = true;
                            m_sock.ReceiveTimeout = THREAD_SLEEP;
                            FiredStatus(ConnectionStatus.SOCKET_RECREATED);
                            m_sock.Connect(new IPEndPoint(m_addr, (int)m_port));
                            FiredStatus(ConnectionStatus.CONNECTED);
                        }
                        catch(SocketException)
                        {
                            FiredStatus(ConnectionStatus.FAILED_CONNECTION);
                            askClose = true;
                        }
                    }
                }

                //Data available
                //The message will be sent to HandleMessage
                if(m_sock.Poll(THREAD_SLEEP, SelectMode.SelectRead))
                {
                    if(m_sock.Available > 0)
                    { 
                        //Receive data and handle it
                        byte[] dataBuf = new byte[m_sock.Available];
                        SocketError err;

                        try
                        {
                            int readSize = m_sock.Receive(dataBuf, 0, dataBuf.Length, SocketFlags.None, out err);
                            if (err == SocketError.Success)
                            {
                                if (readSize != dataBuf.Length)
                                    Array.Resize<byte>(ref dataBuf, readSize);

                                HandleMessage(dataBuf);
                            }
                            else
                                askClose = true;
                        }
                        catch(SocketException)
                        {
                            FiredStatus(ConnectionStatus.DISCONNECTED);
                            askClose = true;
                        }
                    }

                    //Server disconnected
                    else
                    {
                        FiredStatus(ConnectionStatus.DISCONNECTED);
                        askClose = true;
                    }

                }

                if (askClose)
                {
                    if(m_sock != null)
                        m_sock.Close();
                    m_sock = null;
                }
            }
		}

        /// <summary>
        /// Thread checking for data to write
        /// </summary>
        private void WriteThread()
        {
            bool sleep = false; 
            while (!m_closed)
            {
                if(sleep)
                {
                    try
                    {
                        Thread.Sleep(THREAD_SLEEP);
                    }
                    catch(Exception e)
                    { 
                        return;
                    }
                }
                sleep = false;

                lock(this)
                {
                    if(m_sock == null || m_sock.Connected == false)
                    {
                        sleep = true;
                        continue;
                    }


                    byte[] msg = null;
                    lock(m_writeBuffer)
                    {
                        if(m_writeBuffer.Count > 0)
                            msg = m_writeBuffer.Dequeue();
                    }

                    if (msg == null)
                    {
                        sleep = true;
                        continue;
                    }

                    //Try to send. If failed -> Disconnection
                    try
                    {
                        m_sock.Send(msg);
                    }
                    catch (SocketException)
                    {}
                }
            }
        }

        #endregion
    }
}