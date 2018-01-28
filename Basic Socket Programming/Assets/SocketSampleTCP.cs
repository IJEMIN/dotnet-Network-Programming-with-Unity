using UnityEngine;
using System.Net;
using System.Net.Sockets;

public class SocketSampleTCP : MonoBehaviour
{
	// 상대방 기계 주소
	private string m_address = "";

	// 그 주소안에 상세 번지수
	private int m_port = 50765;

	// 소켓은 소프트웨어 적으로 존재하는 네트워크 입구
	// 인사를 위한 늘 열려 있는 입구
	private Socket m_listener;

	// 손님을 받은 다음 실제로 데이터 교환을 위해 쓸 소켓
	private Socket m_dataSocket;

	// 현재 네트워크 상태
	private enum State
	{
		Idle, // 네트워크 자체가 아직 실행 안됨
		Listen, // 서버가 손님을 기다리는 중
		AcceptClient, // 손님이 서버에 왔음
		ServerCommunication, // 서버가 클라이언트랑 통신중
		StopListen, // 서버가 손님을 더이상 안받음
		ClientCommunication, // 클라이언트가 서버랑 통신중
		EndCommunication, // 통신을 마감하고 종료
	}

	private State m_state = State.Idle;

	private void Start()
	{
		// 나 자신
		m_address = "127.0.0.1";
	}

	private void Update()
	{
		switch (m_state)
		{
			case State.ClientCommunication:
				ClientProcess();
				break;

			case State.Listen:
				ServerStartListen();
				break;

			case State.AcceptClient:
				AcceptClient();
				break;

			case State.ServerCommunication:
				ServerCommunication();
				break;

			case State.StopListen:
				StopListen();
				break;

			case State.EndCommunication:
				break;

			default:
				break;
		}
	}

	public void ChangeAddress(string newAddress)
	{
		m_address = newAddress;
	}

	// 손님이 서버로 찾아가서 메세지를 던져넣기
	private void ClientProcess()
	{
		Debug.Log("TCP - 클라이언트로서 서버에 연결을 시작함");

		m_dataSocket =
			new Socket(AddressFamily.InterNetwork, SocketType.Stream,
			ProtocolType.Tcp);

		m_dataSocket.NoDelay = true;
		m_dataSocket.SendBufferSize = 0;
		// 대화할 상대방을 알고 있으므로 리스너 소켓을 통하지 않아도 됨
		m_dataSocket.Connect(m_address, m_port);

		// 문장 데이터 타입 (UTF8인코딩) => 컴퓨터가 다루는 (날것) 데이터 타입
		byte[] buffer
			= System.Text.Encoding.UTF8.GetBytes("안녕? 나는 클라이언트야");

		m_dataSocket.Send(buffer, buffer.Length, SocketFlags.None);

		m_dataSocket.Shutdown(SocketShutdown.Both);
		m_dataSocket.Close();

		Debug.Log("TCP - 클라이언트 접속 종료");

		m_state = State.EndCommunication;
	}

	// 서버가 손님을 기다리기 시작
	private void ServerStartListen()
	{
		Debug.Log("TCP - 서버 리슨 시작");
		// 손님용 입구 소켓을 하나 찍어냄
		// 인터넷주소 (000.000.000.000)
		// TPC 프로토콜을 사용
		m_listener = new Socket(AddressFamily.InterNetwork,
			SocketType.Stream, ProtocolType.Tcp);

		// 소켓이 통신하려는 상대방의 끝지점이 무엇인가?
		// 대화하려는 주소(아이피) + 번지수(포트)
		m_listener.Bind(new IPEndPoint(IPAddress.Any, m_port));

		m_listener.Listen(1);

		m_state = State.AcceptClient;
	}

	// 손님을 인식하고, 인식이 됬다면 진짜 대화용 소켓을 만드는곳
	private void AcceptClient()
	{
		if (m_listener != null && m_listener.Poll(0, SelectMode.SelectRead))
		{
			m_dataSocket = m_listener.Accept();
			Debug.Log("TCP - 클라이언트가 연결되어 옴");
			m_state = State.ServerCommunication;
		}
	}

	// 실제 데이터 통신용 소켓을 사용해서 클라이언트가 주는 메세지를 받기
	private void ServerCommunication()
	{
		// 실제 데이터를 받아올 버퍼(중간 창고)
		byte[] buffer = new byte[1400];

		// 버퍼에 받아온 데이터를 옮겨 넣기
		// 버퍼를 얼마까지 채워서 받아왔는지 recvSize 에 저장
		int recvSize = m_dataSocket.Receive(buffer,
			buffer.Length, SocketFlags.None);

		if (recvSize > 0)
		{
			// 바이트 데이터 (날것 데이터)를 변환해서 원래 문장으로
			string message = System.Text.Encoding.UTF8.GetString(buffer);
			Debug.Log(message);

			m_state = State.StopListen;
		}
	}

	// 더이상의 손님 거절한다
	private void StopListen()
	{
		if (m_listener != null)
		{
			// 장사끝
			m_listener.Close();
			m_listener = null;
		}
		m_state = State.EndCommunication;
	}

	public void OnServerButtonClick()
	{
		m_state = State.Listen;
	}

	public void OnClientButtonClick()
	{
		m_state = State.ClientCommunication;
	}
}