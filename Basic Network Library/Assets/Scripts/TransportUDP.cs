using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class TransportUDP : MonoBehaviour
{

    // 소켓 액세스 포인트들

    // 클라이언트의 접속을 받을 소켓
    private Socket m_socket = null;

    private PacketQueue m_sendQueue; // 송신 버퍼

    private PacketQueue m_recvQueue; // 수신 버퍼

    public bool isServer { get; private set;} // 서버 플래그

    public bool isConnected {get; private set;} // 접속 플래그


    // 이벤트 관련

    // 이벤트 통지 델리게이트 타입 정의
    public delegate void EventHandler(NetEventState state);
    // 이벤트 핸들러
    public event EventHandler onStateChanged;


    // 스레드 관련 멤버 변수

    // 스레드 실행 프래그
    protected bool m_threadLoop = false;
    protected Thread m_thread = null;
    private static int s_mtu = 1400; // 한번에 읽을 데이터


    // 초기화 페이즈: 큐 생성
    void Awake()
    {
        m_sendQueue = new PacketQueue();
        m_recvQueue = new PacketQueue();
    }

    // 서버로서 가동 (대기) 시작
    public bool StartServer(int port, int connectionNum)
    {

        Debug.Log("Initiate Server!");

        try
        {
            // 소켓 생성후
            m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Udp);

            // 대응 대역폭 지정
            m_socket.Bind(new IPEndPoint(IPAddress.Any, port));
        }
        catch
        {
            Debug.Log("Server Failed!");

            return false;
        }

        isServer = true;

        bool success = LaunchThread();

        return success;
    }

    // 서버로서 대기 종료
    public void StopServer()
    {
		// 쓰레드 종료
        m_threadLoop = false;

        if (m_thread != null)
        {
            m_thread.Join();
            m_thread = null;
        }

		// 접속 종료
		Disconnect();

		isServer = false;

		Debug.Log("Server Stopped");
    }

    public bool Connect(string address, int port)
    {
        Debug.Log("TransportUDP Connect Called");

		// 이미 통신 소켓이 선점되어 있다면
		if (m_socket != null) {
			return false;
		}

		bool ret = false;

		try {
			m_socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Udp);

			m_socket.NoDelay = true; // 소켓 지연시간 없음
			m_socket.Connect(address,port); // 소켓 연결 시작

			// 커넥션 스레드 시작
			ret = LaunchThread();
		} catch {
			// 실패했다면 소켓 파괴
			m_socket = null;
		}

		if(ret == true)
		{
			isConnected = true;
			Debug.Log("Connection Success");
		}
		else
		{
			isConnected = false;
			Debug.Log("Connection Fail");
		}

		// 리스너가 존재한다면 통지
		if(onStateChanged != null) {
			NetEventState state = new NetEventState();
			state.type = NetEventType.Connect;
			state.result = (isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
			
			Debug.Log("Event Handler Called");
		}

		return isConnected;
    }

	// 접속 종료
    public void Disconnect()
    {
        isConnected = false;

        if (m_socket != null)
        {
            m_socket.Shutdown(SocketShutdown.Both); // 쌍방향 소켓 연결 내리기
            m_socket.Close(); // 소켓 종료
            m_socket = null; // 소켓 파괴
        }

        // 리스터가 존재한다면 접속 종료를 공지
        if (onStateChanged != null)
        {
			// 새로운 네트워크 상태 정보 생성후
			NetEventState state = new NetEventState();
			state.type = NetEventType.Disconnect;
			state.result = NetEventResult.Success;

			// 이벤트로 공지
			onStateChanged(state);
        }
    }

	// 송신 처리
	// 큐에 데이터를 넣어놓으면 알아서 쓰레드가 빼가서 보내놓을거라능 ㅇㅅㅇ
	public int Send(byte[] data, int size)
	{
		// 세이프티 체크
		if(m_sendQueue == null)
		{
			return 0;
		}

		return m_sendQueue.Enqueue(data,size);
	}


	// 수신 처리
	// 큐에 데이터를 넣어놓으면 알아서 쓰레드가 빼가서 보내놓을거라능 ㅇㅅㅇ
	public int Receive(ref byte[] data, int size)
	{
		// 세이프티 체크
		if(m_recvQueue == null)
		{
			return 0;
		}

		return m_recvQueue.Dequeue(ref data,size);
	}

	// 스레드 실행 함수.
	// 목적: 돌려 놓으면 알아서 Send Queue 에 쌓아놓은 데이터는 보내주고
	// 온 데이터는 Recv Queue 에 쌓아놓아줌
	bool LaunchThread()
	{
		try {
			// Dispatch용 스레드 시작.
			m_threadLoop = true;
			m_thread = new Thread(new ThreadStart(Dispatch));
			m_thread.Start();
		}
		catch {
			Debug.Log("Cannot launch thread.");
			return false;
		}
		
		return true;
	}


	// 스레드를 통해 송수신을 처리해주는 실제 패킷큐 처리부
	public void Dispatch()
	{
		Debug.Log("Distpach thread started.");

		// 스레드 루프가 계속 돌아가는 동안
		while(m_threadLoop)
		{
			if(m_socket != null && isConnected == true)
			{
				// 송신
				DispatchSend();

				// 수신
				DispatchReceive();
			}

			// 실행 간격 5밀리 세컨드
			Thread.Sleep(5);
		}

		Debug.Log("Dispatch thread ended");
	}

	void DispatchSend()
	{
		try{
			// 데이터를 보낼 준비가 되있다면
			if(m_socket.Poll(0,SelectMode.SelectWrite))
			{
				// 바이트 배열 생성
				byte[] buffer = new byte[s_mtu];

				// 전송 대기큐로부터 데이터를 빼내어 가져옴
				int sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);

				// 보낼 데이터가 있는 동안 계속 송신-데이터 빼오기 반복
				while(sendSize > 0) {
					m_socket.Send(buffer, sendSize, SocketFlags.None);
					sendSize = m_sendQueue.Dequeue(ref buffer, buffer.Length);
				}
			}
		} catch {
			return;
		}
	}

	void DispatchReceive()
	{
		try{
			// 읽을 데이터가 있으면
			while(m_socket.Poll(0,SelectMode.SelectRead)) {
				byte[] buffer = new byte[s_mtu];

				int recvSize = m_socket.Receive(buffer,buffer.Length,SocketFlags.None);

				if(recvSize == 0)
				{
					// 더이상 가져올 데이터가 없다면

					Debug.Log("Diconnect recv from client.");
					Disconnect();
				}
				else if (recvSize > 0) { // 데이터를 가져왔다면 리시브 큐에 적재해둠
					m_recvQueue.Enqueue(buffer,recvSize);

				}
			}
		}
		catch {
			return;
		}
	}
}
