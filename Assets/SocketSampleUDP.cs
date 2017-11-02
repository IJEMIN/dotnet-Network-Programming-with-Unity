using UnityEngine;
using System.Net;
using System.Net.Sockets;

// UDP는 TCP와 달리 서버와 클라이언트의 구별도 없고
// 서버가 대기할 필요도 없고
// 받는 주소와 포트를 지정해 보내기만 하면됨
public class SocketSampleUDP : MonoBehaviour
{
	// 접속 주소 IPv4
	private string m_address = "127.0.0.1";

	private int m_port = 54329;

	private Socket m_socket = null;

	private enum State
	{
		Idle, // 통신을 시작 안함
		CreateListener, // 데이터를 받기 위한 소켓 생성
		Receving, // 소켓에서 메세지를 꺼내는 중
		CloseListener, // 데이터를 받는 소켓을 닫기
		Sending, // 소켓으로 메세지를 보내는 중
		End // 네트워크 마감
	}

	private State m_state = State.Idle;

	// 데이터를 받는 소켓 생성
	private void CreateListener()
	{
		Debug.Log("UDP - 소켓 연결 시작");

		m_socket =
			new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
			ProtocolType.Udp);

		m_socket.Bind(new IPEndPoint(IPAddress.Any, m_port));
		m_state = State.Receving;
	}

	private void Receving()
	{
		byte[] buffer = new byte[1400];
		IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
		EndPoint remoteSender = (EndPoint)sender;

		if (m_socket.Poll(0, SelectMode.SelectRead))
		{
			int recvSize
				= m_socket.
				ReceiveFrom(buffer, SocketFlags.None, ref remoteSender);

			if (recvSize > 0)
			{
				string message
					= System.Text.Encoding.UTF8.GetString(buffer);
				Debug.Log(message);
				m_state = State.CloseListener;
			}
		}
	}

	// 대기 종료
	private void CloseListener()
	{
		if (m_socket != null)
		{
			m_socket.Close();
			m_socket = null;
		}
		m_state = State.End;
		Debug.Log("UDP - 통신 끝");
	}

	private void Sending()
	{
		Debug.Log("UDP - 통신 시작");
		m_socket =
			new Socket(AddressFamily.InterNetwork, SocketType.Dgram,
			ProtocolType.Udp);

		byte[] buffer =
			System.Text.Encoding.UTF8.GetBytes("This is Client! from UDP");

		IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(m_address),
			m_port);

		m_socket.SendTo(buffer, buffer.Length, SocketFlags.None, endPoint);

		m_socket.Shutdown(SocketShutdown.Both);
		m_socket.Close();

		m_state = State.End;

		Debug.Log("UDP - 통신 끝");
	}

	public void OnServerButtonClicked()
	{
		m_state = State.CreateListener;
	}

	public void OnClientButtonClicked()
	{
		m_state = State.Sending;
	}

	public void ChangeIPAddress(string ipAddress)
	{
		m_address = ipAddress;
	}

	private void Update()
	{
		switch (m_state)
		{
			case State.Sending:
				Sending();
				break;

			case State.CreateListener:
				CreateListener();
				break;

			case State.Receving:
				Receving();
				break;

			case State.CloseListener:
				CloseListener();
				break;

			case State.End:
				break;

			case State.Idle:
				break;

			default:
				break;
		}
	}
}